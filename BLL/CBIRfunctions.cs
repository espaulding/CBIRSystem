using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CBIR {
    static public class CBIRfunctions {
        private const int COLOR_CODE_BIN_COUNT = 64;
        private const int INTENSITY_BIN_COUNT = 25;
        public const string HISTOGRAM_FILE = "imageFeatures.dat";
        public const int DR = 1, DC = 1; //displacement by rows, columns used by texture features

        //memoize the DB so that it can be updated between relevance feedback rounds
        static private FeaturesDB db = null;

        //This function finds the feature vectors for each picture as necessary
        //and then calculats the distance that each picture is from the query image
        //this is the big cheese right here
        //the whole point of CBIR is to call this function get the query results by updating the distances in the list objects
        static public void RankPictures(DirectoryInfo folder, string qFilename, int distanceFunc, bool[] features, ArrayList list, bool feedback) {
            SynchDB(folder, DR, DC, list);
            ArrayList weight = GetInitialWeights(features); //get unbiased weights
            GetPictureByName(list, qFilename).relevant = true; //the query image is always relevant
            if (feedback) { AdjustWeights(weight, features, list); } //bias weights with selected features of relevant images

            //update picture distances
            ArrayList qFeatures = SelectFeatures(qFilename, features);
            foreach (var file in folder.GetFiles("*.jpg")) {
                ArrayList picFeatures = SelectFeatures(file.Name, features);

                //find the picture object in the old list that matches the current file being processed
                ImageMetaData pic = list.OfType<ImageMetaData>().Single(p => p.name == file.Name);
                pic.distance = GetMinkowskiDist(qFeatures, picFeatures, weight, distanceFunc);
            }
        }

        #region generatefeatures

        //get the intensity histogram
        static public ArrayList CalcIntensityHist(Bitmap picture) {
            double I;
            int intensity;
            ArrayList hist = new ArrayList(INTENSITY_BIN_COUNT);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0); }

            for (int x = 0; x < picture.Width; x++) {
                for (int y = 0; y < picture.Height; y++) {
                    I = CalcIntensity(picture.GetPixel(x, y));
                    intensity = (int)Math.Floor(I / 10);
                    if (intensity > (INTENSITY_BIN_COUNT - 1)) { intensity = INTENSITY_BIN_COUNT - 1; } //don't let 250 and up move out of the histogram
                    hist[intensity] = (double)hist[intensity] + 1.0;
                }
            }

            //allow true comparison between images of different sizes
            NormalizeBySize(hist, picture.Width * picture.Height);
            return hist;
        }

        //get the color-code histogram
        static public ArrayList CalcColorCodeHist(Bitmap picture) {
            Color p;
            ArrayList hist = new ArrayList(COLOR_CODE_BIN_COUNT);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0); }
            string RED, GREEN, BLUE;
            int bin;
            for (int x = 0; x < picture.Width; x++) {
                for (int y = 0; y < picture.Height; y++) {
                    p = picture.GetPixel(x, y);

                    //convert to base 2 and add leading zeros up until we have 8 digits
                    RED = Convert.ToString(p.R, 2).PadLeft(8, '0');
                    GREEN = Convert.ToString(p.G, 2).PadLeft(8, '0');
                    BLUE = Convert.ToString(p.B, 2).PadLeft(8, '0');

                    //convert from string of base 2 to an integer in base 10
                    bin = Convert.ToInt32(RED.Substring(0, 2) + GREEN.Substring(0, 2) + BLUE.Substring(0, 2), 2);
                    hist[bin] = (double)hist[bin] + 1;
                }
            }

            //allow true comparison between images of different sizes
            NormalizeBySize(hist, picture.Width * picture.Height);
            return hist;
        }

        //will normalize so that each feature in the list is a percent between [0,1)
        //the entire vector will sum to 1 afterwards
        static private void NormalizeBySize(ArrayList features, int N) {
            //normalize bins by divding by the number of pixels
            //to account for images of different sizes
            for (int f = 0; f < features.Count; f++) {
                features[f] = (double)features[f] / N;
            }
        }

        //get the texture features
        //they are always in the order: Energy, Entropy, Contrast
        static public ArrayList CalcTextureFeatures(Bitmap picture, int dr, int dc) {
            Dictionary<int, Dictionary<int, double>> ngtcom = CalcGrayTone(CalcCoOccurrence(picture, dr, dc));
            ArrayList hist = new ArrayList(3);
            hist.Add(CalcEnergy(ngtcom));
            hist.Add(CalcEntropy(ngtcom));
            hist.Add(CalcContrast(ngtcom));

            return hist;
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the energy
        static public double CalcEnergy(Dictionary<int, Dictionary<int, double>> ngtcom) {
            double energy = 0;
            foreach (KeyValuePair<int, Dictionary<int, double>> row in ngtcom) {
                foreach (KeyValuePair<int, double> col in row.Value) {
                    energy += col.Value * col.Value; //N^2
                }
            }

            return HF.FixFloatingPoint(energy, "1.0E-15");
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the entropy
        static public double CalcEntropy(Dictionary<int, Dictionary<int, double>> ngtcom) {
            double entropy = 0;
            foreach (KeyValuePair<int, Dictionary<int, double>> row in ngtcom) {
                foreach (KeyValuePair<int, double> col in row.Value) {
                    if (col.Value != 0) { entropy += col.Value * Math.Log(col.Value, 2); } //N Log N with log base 2
                }
            }

            return HF.FixFloatingPoint(entropy, "1.0E-15");
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the entropy
        static public double CalcContrast(Dictionary<int, Dictionary<int, double>> ngtcom) {
            double contrast = 0;
            foreach (KeyValuePair<int, Dictionary<int, double>> row in ngtcom) {
                foreach (KeyValuePair<int, double> col in row.Value) {
                    contrast += Math.Pow(row.Key - col.Key, 2) * col.Value; //(i - j)^2 N
                }
            }

            return HF.FixFloatingPoint(contrast, "1.0E-15"); ;
        }

        //given an image and a displacement vector (dr,dc) find the co-occurrence matrix
        //the upper left corner of the image is (0,0)
        //dr is the displacement rows downward
        //dc is the displacement columns to the right
        static public Dictionary<int, Dictionary<int, double>> CalcCoOccurrence(Bitmap Img, int dr, int dc) {
            int[][] image = new int[Img.Height][]; //initalize matrix of intensity values
            for (int r = 0; r < Img.Height; r++) { image[r] = new int[Img.Width]; }

            //build matrix with an intensity value for each pixel
            for (int r = 0; r < Img.Height; r++) {
                for (int c = 0; c < Img.Width; c++) {
                    image[r][c] = (int)CalcIntensity(Img.GetPixel(c, r));
                }
            }

            //grab a sorted list of unique intensity values from the image
            int[] values = image.SelectMany(value => value).Distinct().OrderBy(value => value).ToArray();

            //initalize a hash of hashes to hold the texture counts
            Dictionary<int, Dictionary<int, double>> counts = new Dictionary<int, Dictionary<int, double>>();
            foreach (int v in values) {
                counts.Add(v, new Dictionary<int, double>());
                foreach (int va in values) { counts[v].Add(va, 0.0); }
            }

            //update counts using the displacement vector that defines this texture
            for (int r = 0; r < image.Length - dr; r++) {
                for (int c = 0; c < image[0].Length - dc; c++) {
                    counts[image[r][c]][image[r + dr][c + dc]]++;
                }
            }

            return counts;
        }

        static public Dictionary<int, Dictionary<int, double>> CalcGrayTone(Dictionary<int, Dictionary<int, double>> com) {
            //sum up all the values in the co-occurrence matrix
            double sum = com.Sum(kvpRow => kvpRow.Value.Sum(kvpCol => kvpCol.Value));

            //initalize a hash of hashes for the normalized gray tone matrix
            Dictionary<int, Dictionary<int, double>> gray = new Dictionary<int, Dictionary<int, double>>();

            //normalize each value by the sum. i.e. uniform distribution
            foreach (KeyValuePair<int, Dictionary<int, double>> row in com) {
                //instantiate the column and add it to the current row
                gray.Add(row.Key, new Dictionary<int, double>());

                //normalize and add the com value to the ngtcom
                foreach (KeyValuePair<int, double> col in row.Value) {
                    gray[row.Key][col.Key] = col.Value / sum;
                }
            }

            return gray;
        }

        #endregion

        #region database

        //undo any DB modifications caused by relevance feedback by either reloading the DB from a file
        //or if necessary rebuild it from scratch, computing all the feature metadata again
        static public void LoadDB(DirectoryInfo folder, ref ArrayList oldlist, bool forceRebuild) {
            string dbFile = folder.FullName + "\\" + HISTOGRAM_FILE;

            //if there is no oldlist start making a new one, but otherwise alias the old list as the new list
            ArrayList list = null;
            if (oldlist == null) { list = new ArrayList(); oldlist = list; } else { list = oldlist; }

            db = null; //wipe out any db information that may have been in memory
            if (!forceRebuild) {
                if (File.Exists(dbFile)) { db = (FeaturesDB)HF.DeSerialize(dbFile); } else { db = new FeaturesDB(); }
                SynchDB(folder, DR, DC, list);
            } else {
                RebuildDB(folder, DR, DC);
            }
        }

        //re-aquire raw histograms for every image in the directory
        //this is the computational bottleneck for the entire software program
        //writing the data to file alleviates most of the issue, but it takes too long to build a new DB.
        static private void RebuildDB(DirectoryInfo folder, int dr, int dc) {
            db = new FeaturesDB();
            foreach (var file in folder.GetFiles("*.jpg")) {
                DBAddPicture(file.FullName, file.Name, file.Length, dr, dc);
            }
            //make sure the data in the db doesn't contain bad data and then normalize each feature
            if (db.CheckData()) { throw new Exception("Database is corrupted. Entries set to (NaN, Infinity, etc)"); }
            HF.Serialize(folder.FullName + "\\" + HISTOGRAM_FILE, db); //save the new db since it just got rebuilt
            db.NormalizeByFeatures(); //normalize each feature over a gaussian distribution
        }

        //add new items to the database as needed, and synch the db up with list of picture metadata
        static private void SynchDB(DirectoryInfo folder, int dr, int dc, ArrayList list) {
            //possible scenarios
            //-different file, but named the same as an old file is in the folder; ACTION: the old file is removed from the db and list, new file gets added
            //-a file was added to the folder; ACTION: new file gets added to the db and the list
            //-a file was removed from the folder; ACTION: absent item removed from list and db
            bool updated = false;
            List<string> removed = new List<string>();
            List<ImageMetaData> added = new List<ImageMetaData>();
            string dbFile = folder.FullName + "\\" + HISTOGRAM_FILE;

            //anything new in the folder should be added to the list and the db
            foreach (var file in folder.GetFiles("*.jpg")) {
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
                if (GetPictureByName(list, pic) == null) {
                    string fullpath = folder.ToString() + "\\" + pic;
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
                list.Remove(GetPictureByName(list, pic));
            }

            //add new stuff in the folder to the db
            if (added.Count > 0) {
                //reload the db file before adding new stuff because we need the un-normalized db 
                //so that everything in the db can be normalized together
                if (File.Exists(dbFile)) { db = (FeaturesDB)HF.DeSerialize(dbFile); } else { db = new FeaturesDB(); }
                foreach (ImageMetaData pic in added) {
                    list.Remove(GetPictureByName(list, pic.name));
                    list.Add(pic);
                    DBAddPicture(pic.path, pic.name, pic.size, dr, dc);
                }
            }

            //save changes to the dbFile
            if (updated) {
                //make sure the data in the db doesn't contain bad data and then normalize each feature
                if (db.CheckData()) { throw new Exception("Database is corrupted. Entries set to (NaN, Infinity, etc)"); }
                HF.Serialize(dbFile, db); //always save before normalization              
            }
            db.NormalizeByFeatures(); //normalize each feature over a gaussian distribution
        }

        //add a picture to the database
        static private void DBAddPicture(string fullname, string name, long filesize, int dr, int dc) {
            Bitmap picture = (Bitmap)Bitmap.FromFile(fullname);
            ArrayList intensityHist = CBIRfunctions.CalcIntensityHist(picture);
            ArrayList colorCodeHist = CBIRfunctions.CalcColorCodeHist(picture);
            ArrayList textureHist = CBIRfunctions.CalcTextureFeatures(picture, dr, dc);
            db.Add(name, filesize, intensityHist, colorCodeHist, textureHist);
            picture.Dispose();
        }

        //allow a specific feature of a specific picture to be set directly as the given value
        //db[picture][feature] = value; where feature is a single number. Consider that color-code generates 64 features
        //if this function is being used to fix a corrupted database, strongly consider saving the fixed database file
        static private void UpdateDBValues(double value, int index, string pic, bool[] features) {
            //deal with cases where intensity is selected
            if (features[0]) {
                if (index < INTENSITY_BIN_COUNT) {
                    db.intensityDB[pic][index] = value; return;
                }
                index -= INTENSITY_BIN_COUNT; //adjust index if intensity is selected
            }

            //deal with cases where color-code is selected
            if (features[1]) {
                if (index < COLOR_CODE_BIN_COUNT) {
                    db.colorCodeDB[pic][index] = value; return;
                }
                index -= COLOR_CODE_BIN_COUNT; //adjust index if color-code is selected
            }

            //deal with cases where textures are selected
            if (!features[2]) { index++; } //pad the index if energy is not selected
            if (!features[3]) { index++; } //pad the index if entropy is not selected

            //update textureDB index must be 0,1,2 i.e. energy, entropy, or contrast
            db.textureDB[pic][index] = value;
        }

        //features[] => intensity, color-code, energy, entropy, contrast
        static private ArrayList SelectFeatures(string filename, bool[] features) {
            ArrayList combined = new ArrayList();
            if (features[0]) { combined.AddRange(db.intensityDB[filename]); }
            if (features[1]) { combined.AddRange(db.colorCodeDB[filename]); }
            if (features[2]) { combined.Add(db.textureDB[filename][0]); } //energy
            if (features[3]) { combined.Add(db.textureDB[filename][1]); } //entropy
            if (features[4]) { combined.Add(db.textureDB[filename][2]); } //contrast
            return combined;
        }

        #endregion

        #region relevancefeedback

        //get initial weights such that all weights sum to 1 and are equal
        static public ArrayList GetInitialWeights(bool[] features) {
            int vectorLength = 0;
            if (features[0]) { vectorLength += INTENSITY_BIN_COUNT; }
            if (features[1]) { vectorLength += COLOR_CODE_BIN_COUNT; }
            if (features[2]) { vectorLength += 1; } //energy
            if (features[3]) { vectorLength += 1; } //entropy
            if (features[4]) { vectorLength += 1; } //contrast

            double w = 1.0d / vectorLength;
            ArrayList weight = new ArrayList(vectorLength);
            for (int x = 0; x < weight.Capacity; x++) { weight.Add(w); }
            return weight;
        }

        //generate a matrix with rows as selected features, and columns being images marked as relevant
        //if this matrix has less than 2 columns set weight to the balanced initial weights
        //otherwise find the standard deviation of each row (features) and adjust the weights
        //CRITICAL this function assumes that the database file is in SYNC with the list of PictureClass objects
        static private void AdjustWeights(ArrayList weight, bool[] features, ArrayList list) {
            if (list == null || db == null) { return; } //stop immediately the function has been prematurely called
            //possibly consider throwing an exception here rather than quietly ignoring the call

            //initialize the matrix: rows (features), columns (relevant images)
            List<double>[] matrix = new List<double>[weight.Count]; //rows are [], columns are List<double>
            for (int i = 0; i < weight.Count; i++) { matrix[i] = new List<double>(); } //init the columns

            //build the matrix of relevant images with selected features
            foreach (string pic in db.sizeDB.Keys) {
                if (list.OfType<ImageMetaData>().Single(p => p.name == pic).relevant) {
                    ArrayList selected = SelectFeatures(pic, features);
                    for (int i = 0; i < selected.Count; i++) { matrix[i].Add((double)selected[i]); }
                }
            }

            //if we have at least 2 relevant pictures go ahead and adjust the weights; otherwise, don't do anything
            if (matrix[0].Count > 1) {
                List<double> mean = new List<double>();  //mean == average value, mean != median
                List<double> sigma = new List<double>(); //sigma == standard deviation, sigma^2 == variance
                int S = matrix.Length;   //there are S selected features
                int N = matrix[0].Count; //there are N relevant images

                //find the means and sigmas, one for each feature (row)
                for (int i = 0; i < S; i++) {
                    mean.Add(HF.FixFloatingPoint(matrix[i].Sum() / N, "1.0E-10"));
                    sigma.Add(HF.FixFloatingPoint(Math.Sqrt(matrix[i].Sum(fi => (fi - mean[i]) * (fi - mean[i])) / (N - 1)), "1.0E-11"));
                }

                //update the weights         
                for (int i = 0; i < S; i++) {
                    if (mean[i] != 0 && sigma[i] == 0) { //nonzero mean but the column has no variance: should get high weight
                        if (sigma.Where(stddev => stddev != 0).Count() > 0) {
                            sigma[i] = 0.5 * sigma.Where(stddev => stddev != 0).Min();
                            weight[i] = 1.0d / sigma[i];
                        } else {
                            weight[i] = -1.0d; //placeholder so we can come back in a moment and set this to the max weight
                        }
                    } else if (mean[i] == 0) { //mean is zero so this feature has no information, set weight to zero
                        weight[i] = 0.0d;
                    } else {                   //adjust weight so that features with less variance get more emphasis
                        weight[i] = 1.0d / sigma[i];
                    }
                    weight[i] = HF.FixFloatingPoint((double)weight[i], "1.0E-15");
                }
                //set any placeholders weights to the max weight
                for (int i = 0; i < S; i++) { if ((double)weight[i] == -1) { weight[i] = weight.OfType<double>().Max(); } }
                NoramlizeWeights(weight);
            }
        }

        //normalize the adjusted weights so that all weights sum to 1
        static private void NoramlizeWeights(ArrayList weight) {
            double sum = weight.OfType<double>().Sum();
            if (sum != 0) {
                for (int i = 0; i < weight.Count; i++) {
                    weight[i] = HF.FixFloatingPoint((double)weight[i] / sum, "5.0E-8");
                }
            }
        }

        #endregion

        #region helperfunctions

        //linear search through list to find picture metadata object by name
        //returns null if no matching data is found
        static public ImageMetaData GetPictureByName(ArrayList list, string name) {
            ImageMetaData result = null;
            foreach (ImageMetaData pic in list) {
                if (pic.name.Equals(name)) { result = pic; }
            }
            return result;
        }

        //p = 1 is manhattan distance function
        //p = 2 is euclidean distance function
        //p = higher increases side-effects between dimensions
        static private double GetMinkowskiDist(ArrayList Qhistogram, ArrayList histogram, ArrayList weight, int p) {
            decimal distance = 0; //use decimal for intermediate computations to help avoid floating point errors
            if (p <= 0) { throw new Exception("Invalid distance function P must be > 0"); }
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int i = 0; i < histogram.Count; i++) {
                distance += Convert.ToDecimal(weight[i]) * Convert.ToDecimal(Math.Pow((double)Math.Abs(Convert.ToDecimal(Qhistogram[i]) - Convert.ToDecimal(histogram[i])), p));
            }
            return Math.Pow((double)distance, (double)(1.0) / p);
        }

        //find an intensity given RGB values
        static public double CalcIntensity(Color p) {
            return (.299 * p.R) + (.587 * p.G) + (.114 * p.B);
        }

        #endregion
    }
}
