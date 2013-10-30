using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.Drawing;

namespace CBIR {
    [Serializable()]
    public class FeaturesDB : ISerializable {
        //class level constants
        public const int DR = 1, DC = 1; //displacement by rows, columns used by texture features
        public const int COLOR_CODE_BIN_COUNT = 64;
        public const int INTENSITY_BIN_COUNT = 25;
        public const int TEXTURE_BIN_COUNT = 3;

        //Data to serialize and store
        public Dictionary<string, ArrayList> intensityDB;
        public Dictionary<string, ArrayList> colorCodeDB;
        public Dictionary<string, ArrayList> textureDB;
        public Dictionary<string, long> sizeDB;
        
        //globals for each instantiation
        public bool normalizedGuassian = false;
        public bool normalizedZeroToOne = false;
        public List<decimal> meanByFeature = new List<decimal>();
        public List<decimal> sigmaByFeature = new List<decimal>();
        public List<decimal> maxByFeature = new List<decimal>();
        public List<decimal> minByFeature = new List<decimal>();
        public DirectoryInfo folder;
        public string dbname;
        public string dbfullname;

        private FeaturesDB(DirectoryInfo folder, string dbfilename) {
            this.folder = folder;
            this.dbname = dbfilename;
            this.dbfullname = folder.FullName + "\\" + dbfilename;
            if (File.Exists(this.dbfullname)) {
                FeaturesDB db = (FeaturesDB)HF.DeSerialize(this.dbfullname);
                intensityDB = db.intensityDB;
                colorCodeDB = db.colorCodeDB;
                textureDB = db.textureDB;
                sizeDB = db.sizeDB;
            } else {
                intensityDB = new Dictionary<string, ArrayList>();
                colorCodeDB = new Dictionary<string, ArrayList>();
                textureDB = new Dictionary<string, ArrayList>();
                sizeDB = new Dictionary<string, long>();
            }
        }

        #region serialization definition

        //constructor used by the deserialization process
        public FeaturesDB(SerializationInfo info, StreamingContext ctxt) {
            this.sizeDB = (Dictionary<string, long>)info.GetValue("fileSize", typeof(Dictionary<string, long>));
            this.intensityDB = (Dictionary<string, ArrayList>)info.GetValue("intensityHistograms", typeof(Dictionary<string, ArrayList>));
            this.colorCodeDB = (Dictionary<string, ArrayList>)info.GetValue("colorCodeHistograms", typeof(Dictionary<string, ArrayList>));
            this.textureDB = (Dictionary<string, ArrayList>)info.GetValue("textureHistograms", typeof(Dictionary<string, ArrayList>));
        }

        //used by the serialization process so that serialize knows what to save
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("fileSize", this.sizeDB);
            info.AddValue("intensityHistograms", this.intensityDB);
            info.AddValue("colorCodeHistograms", this.colorCodeDB);
            info.AddValue("textureHistograms", this.textureDB);
        }

        #endregion

        #region factory

        //undo any DB modifications caused by relevance feedback by either reloading the DB from a file
        //or if necessary rebuild it from scratch, computing all the feature metadata again
        static public FeaturesDB LoadDB(DirectoryInfo folder, string filename, ref List<ImageMetaData> oldlist, bool gaussian, bool uniform, bool forceRebuild) {
            string dbfile = folder.FullName + "\\" + filename;

            //if there is no oldlist start making a new one, but otherwise alias the old list as the new list
            List<ImageMetaData> list = null;
            if (oldlist == null) { list = new List<ImageMetaData>(); oldlist = list; } else { list = oldlist; }

            FeaturesDB db = new FeaturesDB(folder,filename);
            if (forceRebuild) { db.RebuildDB(); }
            db = SynchDB(db, list, gaussian, uniform);
            return db;
        }

        //add new items to the database as needed, and synch the db up with list of picture metadata
        static private FeaturesDB SynchDB(FeaturesDB db, List<ImageMetaData> list, bool gaussian, bool uniform) {
            //possible scenarios
            //-different file, but named the same as an old file is in the folder; ACTION: the old file is removed from the db and list, new file gets added
            //-a file was added to the folder; ACTION: new file gets added to the db and the list
            //-a file was removed from the folder; ACTION: absent item removed from list and db
            bool updated = false;
            List<string> removed = new List<string>();
            List<ImageMetaData> added = new List<ImageMetaData>();

            //anything new in the folder should be added to the list and the db
            foreach (var file in db.folder.GetFiles("*.jpg")) {
                if (db.sizeDB.ContainsKey(file.Name) && !(db.sizeDB[file.Name] == file.Length)) {
                    db.Remove(file.Name); //the file changed size so delete it from our DB 
                }

                if (!db.sizeDB.ContainsKey(file.Name)) {
                    updated = true;
                    added.Add(new ImageMetaData(file.Name, file.FullName, file.Length, false, 0));
                }
            }

            //anything in the db but missing from list needs to be added to list
            //critical if the db is loaded for the first time(application start) because the list will be empty
            foreach (string pic in db.sizeDB.Keys) {
                if (CBIRfunctions.GetPictureByName(list, pic) == null) {
                    string fullpath = db.folder.ToString() + "\\" + pic;
                    if (File.Exists(fullpath)) {
                        FileInfo f = new FileInfo(fullpath);
                        ImageMetaData p = new ImageMetaData(f.Name, f.FullName, f.Length, false, 0);
                        list.Add(p);
                    } else {
                        updated = true;
                        removed.Add(pic); //image is not in the folder anymore so delete from db and the list
                    }
                }
            }

            //anything in the removed list will be taken out of list and the db
            foreach (string pic in removed) {
                db.Remove(pic);
                list.Remove(CBIRfunctions.GetPictureByName(list, pic));
            }

            //add new stuff in the folder to the db
            if (added.Count > 0) {
                //reload the db file before adding new stuff because we need the un-normalized db 
                //so that everything in the db can be normalized together          
                if (db.normalizedGuassian || db.normalizedZeroToOne) { db = new FeaturesDB(db.folder, db.dbname); }
                foreach (ImageMetaData pic in added) {
                    list.Remove(CBIRfunctions.GetPictureByName(list, pic.name));
                    list.Add(pic);
                    db.AddImage(pic.path, pic.name, pic.size, DR, DC);
                }
            }

            //save changes to the dbFile
            if (updated) { HF.Serialize(db.dbfullname, db); }   //always save changes before normalization   
            if (gaussian) { db.NormalizeByFeaturesGaussian(); }         //normalize each feature over a gaussian distribution
            if (uniform) { db.NormalizeByFeaturesUniformly(); } //normalize each feature to the range [0..1] so such that the feature sums to 1    
            return db;
        }

        //re-aquire raw histograms and/or features for every image in the directory
        //this is the computational bottleneck for the entire software program
        //writing the data to file alleviates most of the issue, but it takes too long to build a new DB.
        private void RebuildDB() {
            this.Clear();
            foreach (var file in folder.GetFiles("*.jpg")) {
                this.AddImage(file.FullName, file.Name, file.Length, DR, DC);
            }
            HF.Serialize(this.dbfullname, this); //save the new db since it just got rebuilt
        }

        #endregion

        #region normalization

        //use gaussian normalization
        //this method gets good results, but still isn't matching up with Min's sample output
        public void NormalizeByFeaturesGaussian() {
            if (normalizedGuassian) { return; } //don't let the db get normalized more than once
            bool verbose = false; //cause console output to report significant outliers among the features
            ComputeStdDevByFeature();
            List<string> record = sizeDB.Keys.ToList<string>();
            for (int c = 0; c < record.Count; c++) {
                int intensity = INTENSITY_BIN_COUNT;
                int colorcode = COLOR_CODE_BIN_COUNT + intensity;
                int texture = TEXTURE_BIN_COUNT + colorcode;

                //normalize intensityDB
                for (int i = 0; i < intensity; i++) {
                    if (intensityDB[record[c]].Count > 1 && (decimal)HF.FixFloatingPoint(sigmaByFeature[i], "1.0E-10") != 0) {
                        intensityDB[record[c]][i] = ((decimal)intensityDB[record[c]][i] - meanByFeature[i]) / sigmaByFeature[i];

                        if (Math.Abs((decimal)intensityDB[record[c]][i]) > 4M && verbose) {
                            Console.WriteLine(record[c] + " is " + intensityDB[record[c]][i] + " stdev from the mean in intensity bin " + i);
                        }
                    } else {
                        //this feature currently contains no information that can be used, so suppress it
                        intensityDB[record[c]][i] = 0M; 
                    }
                }

                //normalize colorCodeDB
                for (int i = intensity; i < colorcode; i++) {
                    if (colorCodeDB[record[c]].Count > 1 && (decimal)HF.FixFloatingPoint(sigmaByFeature[i], "1.0E-10") != 0) {
                        colorCodeDB[record[c]][i - intensity] = ((decimal)colorCodeDB[record[c]][i - intensity] - meanByFeature[i]) / sigmaByFeature[i];

                        if (Math.Abs((decimal)colorCodeDB[record[c]][i - intensity]) > 4M && verbose) {
                            Console.WriteLine(record[c] + " is " + colorCodeDB[record[c]][i - intensity] + " stdev from the mean in color code bin " + i);
                        }
                    } else {
                        //this feature currently contains no information that can be used, so suppress it
                        colorCodeDB[record[c]][i - intensity] = 0M;
                    }
                }

                //normalize textureDB
                for (int i = colorcode; i < texture; i++) {
                    if (textureDB[record[c]].Count > 1 && (decimal)HF.FixFloatingPoint(sigmaByFeature[i], "1.0E-10") != 0) {
                        textureDB[record[c]][i - colorcode] = ((decimal)textureDB[record[c]][i - colorcode] - meanByFeature[i]) / sigmaByFeature[i];

                        if (Math.Abs((decimal)textureDB[record[c]][i - colorcode]) > 4M && verbose) {
                            Console.WriteLine(record[c] + " is " + textureDB[record[c]][i - colorcode] + " stdev from the mean in texture bin " + i);
                        }
                    } else {
                        //this feature currently contains no information that can be used, so suppress it
                        textureDB[record[c]][i - colorcode] = 0M;
                    }
                }
            }
            normalizedGuassian = true;
        }

        //use [0..1] normalization
        public void NormalizeByFeaturesUniformly() {
            if (normalizedZeroToOne) { return; } //don't let the db get uniform normalized more than once
            ComputeMaxMinByFeature();
            decimal tolerance = .00000001M; //the max and the min of a feature must be at least this different for the feature to have relevant information
            List<string> record = intensityDB.Keys.ToList<string>();
            for (int c = 0; c < record.Count; c++) {
                int intensity = intensityDB[record[c]].Count;
                int colorcode = colorCodeDB[record[c]].Count + intensity;
                int texture = textureDB[record[c]].Count + colorcode;

                //normalize intensityDB
                for (int i = 0; i < intensity; i++) {
                    if (Math.Abs(maxByFeature[i] - minByFeature[i]) > tolerance) {
                        intensityDB[record[c]][i] = ((decimal)intensityDB[record[c]][i] - minByFeature[i]) / (maxByFeature[i] - minByFeature[i]);
                    } else {
                        //this feature currently contains no information that can be used, so suppress it
                        intensityDB[record[c]][i] = 0M;
                    }
                }

                //normalize colorCodeDB
                for (int i = intensity; i < colorcode; i++) {
                    if (Math.Abs(maxByFeature[i] - minByFeature[i]) > tolerance) {
                        colorCodeDB[record[c]][i - intensity] = ((decimal)colorCodeDB[record[c]][i - intensity] - minByFeature[i]) / (maxByFeature[i] - minByFeature[i]);
                    } else {
                        //this feature currently contains no information that can be used, so suppress it
                        colorCodeDB[record[c]][i - intensity] = 0M;
                    }
                }

                //normalize textureDB
                for (int i = colorcode; i < texture; i++) {
                    if (Math.Abs(maxByFeature[i] - minByFeature[i]) > tolerance) {
                        textureDB[record[c]][i - colorcode] = ((decimal)textureDB[record[c]][i - colorcode] - minByFeature[i]) / (maxByFeature[i] - minByFeature[i]);
                    } else {
                        //this feature currently contains no information that can be used, so suppress it
                        textureDB[record[c]][i - colorcode] = 0M;
                    }
                }
            }
            normalizedZeroToOne = true;
        }

        public ArrayList[] GenerateFeatureMatrix() {
            //there are 25 intensity features, 64 color-code features, and 3 texture features
            ArrayList[] feature = new ArrayList[INTENSITY_BIN_COUNT + COLOR_CODE_BIN_COUNT + TEXTURE_BIN_COUNT];
            foreach (KeyValuePair<string, ArrayList> record in intensityDB) {
                int c = 0;
                foreach (decimal f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            foreach (KeyValuePair<string, ArrayList> record in colorCodeDB) {
                int c = INTENSITY_BIN_COUNT;
                foreach (decimal f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            foreach (KeyValuePair<string, ArrayList> record in textureDB) {
                int c = INTENSITY_BIN_COUNT + COLOR_CODE_BIN_COUNT;
                foreach (decimal f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            return feature;
        }

        public void ComputeMaxMinByFeature() {
            ArrayList[] feature = GenerateFeatureMatrix();

            //find the mean and standard deviation by looking at each feature or bin individually over all pictures in the DB
            for (int c = 0; c < feature.Length; c++) {
                decimal max = ((ArrayList)feature[c]).OfType<decimal>().Max(); //find the max
                decimal min = ((ArrayList)feature[c]).OfType<decimal>().Min(); //find the max
                this.maxByFeature.Add(max);
                this.minByFeature.Add(min);
            }
        }

        public void ComputeStdDevByFeature() {
            ArrayList[] feature = GenerateFeatureMatrix();

            //find the mean and standard deviation by looking at each feature or bin individually over all pictures in the DB
            for (int c = 0; c < feature.Length; c++) {
                decimal avg = ((ArrayList)feature[c]).OfType<decimal>().Average(); //find the mean
                decimal sum = ((ArrayList)feature[c]).OfType<decimal>().Sum(f => (f - avg) * (f - avg)); //get the numerator for std dev
                decimal sigma = Convert.ToDecimal(Math.Sqrt((double)sum / (((ArrayList)feature[c]).Count - 1)));
                this.meanByFeature.Add(avg);
                this.sigmaByFeature.Add(sigma);
            }
        }

        #endregion

        //features[] => intensity, color-code, energy, entropy, contrast
        public ArrayList SelectFeatures(string imagefilename, bool[] features) {
            ArrayList combined = new ArrayList();
            if (features[0]) { combined.AddRange(intensityDB[imagefilename]); }
            if (features[1]) { combined.AddRange(colorCodeDB[imagefilename]); }
            if (features[2]) { combined.Add(textureDB[imagefilename][0]); } //energy
            if (features[3]) { combined.Add(textureDB[imagefilename][1]); } //entropy
            if (features[4]) { combined.Add(textureDB[imagefilename][2]); } //contrast
            return combined;
        }

        public void Add(string filename, long size, ArrayList intensityHist, ArrayList colorCodeHist, ArrayList textureHist) {
            if (normalizedGuassian || normalizedZeroToOne) { throw new Exception("Can't add items to a normalized DB. Reload or remake the DB before adding this item"); }
            sizeDB.Add(filename, size);
            intensityDB.Add(filename, intensityHist);
            colorCodeDB.Add(filename, colorCodeHist);
            textureDB.Add(filename, textureHist);
        }

        //add a picture to the database
        public void AddImage(string fullname, string name, long filesize, int dr, int dc) {
            Bitmap picture = (Bitmap)Bitmap.FromFile(fullname);
            ArrayList intensityHist = CBIRfunctions.CalcIntensityHist(picture);
            ArrayList colorCodeHist = CBIRfunctions.CalcColorCodeHist(picture);
            ArrayList textureHist = CBIRfunctions.CalcTextureFeatures(picture, dr, dc);
            Add(name, filesize, intensityHist, colorCodeHist, textureHist);
            picture.Dispose();
        }

        //allow a specific feature of a specific picture to be set directly as the given value
        //db[picture][feature] = value; where feature is a single number. Consider that color-code generates 64 features
        //if this function is being used to fix a corrupted database, strongly consider saving the fixed database file
        public void UpdateDBValue(decimal value, int featureindex, string imagefilename, bool[] features) {
            //deal with cases where intensity is selected
            if (features[0]) {
                if (featureindex < INTENSITY_BIN_COUNT) {
                    intensityDB[imagefilename][featureindex] = value; return;
                }
                featureindex -= INTENSITY_BIN_COUNT; //adjust index if intensity is selected
            }

            //deal with cases where color-code is selected
            if (features[1]) {
                if (featureindex < COLOR_CODE_BIN_COUNT) {
                    colorCodeDB[imagefilename][featureindex] = value; return;
                }
                featureindex -= COLOR_CODE_BIN_COUNT; //adjust index if color-code is selected
            }

            //deal with cases where textures are selected
            if (!features[2]) { featureindex++; } //pad the index if energy is not selected
            if (!features[3]) { featureindex++; } //pad the index if entropy is not selected

            //update textureDB index must be 0,1,2 i.e. energy, entropy, or contrast
            textureDB[imagefilename][featureindex] = value;
        }

        public void Remove(string filename) {
            sizeDB.Remove(filename);
            intensityDB.Remove(filename);
            colorCodeDB.Remove(filename);
            textureDB.Remove(filename);
        }

        public void Clear() {
            string[] all = sizeDB.Keys.ToArray();
            foreach (string image in all) {
                Remove(image);
            }
        }
    }
}
