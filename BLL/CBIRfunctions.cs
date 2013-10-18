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

        #region processdata

        //This function finds the feature vectors for each picture as necessary
        //and then calculats the distance that each picture is from the query image
        public static void calculatePictures(string imageFoldPath, string qFilename, int distanceFunc,
                                             bool[] features, ArrayList weight, ref ArrayList oldlist) {
            //displacement by rows, columns used by texture features
            int dr = 1, dc = 1; //hardcoded for now but maybe a parameter later
            ArrayList list;
            //if there is no oldlist start making a new one, but otherwise alias the old list as the new one
            if (oldlist == null) { list = new ArrayList(); oldlist = list; } else { list = oldlist; }

            DirectoryInfo dir = new DirectoryInfo(imageFoldPath);
            string dbFile = imageFoldPath + "\\" + HISTOGRAM_FILE;

            HistogramDB db = null;

            //read the existing histogram file if there is one
            if (File.Exists(dbFile)) { db = (HistogramDB)HF.DeSerialize(dbFile); } else { db = new HistogramDB(); }

            //if we found a new picture in the folder rebuild the database so that 
            //gaussian normalization can be reapplied over all the features
            if (UpdateDB(db, dir, list)) {
                RebuildDB(ref db, dir, dr, dc);
                if (db.CheckData()) { throw new Exception("There are bad numbers in the database. (NaN, Infinity, etc)"); }
                db.NormalizeByFeatures(); //normalize each feature over a gaussian distribution
                //db.NormalizeByGlobal(); //normalize by the database's global mean and standard deviation over a gaussian distribution
                if (db.CheckData()) { throw new Exception("There are bad numbers in the database. (NaN, Infinity, etc)"); }
                HF.Serialize(dbFile, db); //save the new db since we had to rebuild it
            }

            //update picture distances
            ArrayList qFeatures = SelectFeatures(db, qFilename, features);
            foreach (var file in dir.GetFiles("*.jpg")) {
                ArrayList picFeatures = SelectFeatures(db, file.Name, features);

                //find the picture object in the old list that matches the current file being processed
                PictureClass pic = list.OfType<PictureClass>().Single(p => p.name == file.Name);
                pic.distance = CalculateDist(qFeatures, picFeatures, weight, distanceFunc);
            }
        }

        //re-aquire raw histograms for every image in the directory
        static public void RebuildDB(ref HistogramDB db, DirectoryInfo dir, int dr, int dc) {
            db = new HistogramDB();
            foreach (var file in dir.GetFiles("*.jpg")) {
                DBAddPicture(file.FullName, file.Name, file.Length, db, dr, dc);
            }
        }

        //possible scenarios
        //-db doesn't needs to be updated
        //  any file in the db but not the list is added to the list if it's found in the folder
        //-different file, but named the same as an old file is in the folder
        //  the old file is removed from the db and list, new file added to list, db gets fully rebuilt and saved over
        //-a file was removed from the folder
        //  absent item removed from list, can probably just keep the db as is
        //-a file was added to the folder
        //  new file gets added to list, db gets fully rebuilt and saved over
        static public bool UpdateDB(HistogramDB db, DirectoryInfo dir, ArrayList list) {
            bool updated = false;
            List<PictureClass> removed = new List<PictureClass>();

            //check for pictures in the list that are missing in the db
            foreach (PictureClass pic in list) {
                //make a list of pictures that need to be removed from the list
                if (!db.sizeDB.ContainsKey(pic.name)) {
                    removed.Add(pic);
                }
            }

            //anything in the removed list will be taken out of list
            foreach (PictureClass pic in removed) {
                list.Remove(pic);
            }

            //anything in the folder that's missing from or changed in the db should be added to the list
            foreach (var file in dir.GetFiles("*.jpg")) {
                //look the file up in the current histogram data
                if (db.sizeDB.ContainsKey(file.Name) && !(db.sizeDB[file.Name] == file.Length)) { //we've seen this image before
                    db.Remove(file.Name); //the file changed size so delete it from our DB 
                }

                if (!db.sizeDB.ContainsKey(file.Name)) {
                    PictureClass pic = new PictureClass(file.Name, file.FullName, file.Length, false, 0);
                    list.Remove(pic);
                    list.Add(pic);
                    updated = true;
                }
            }

            //anything in the db but missing from list needs to be added to list
            //this would track something in the db that's not actually present in the folder right 
            //  now, but it checks for the file before adding it to the list.
            //this is also critical if the db is loaded for the first time because the list will be empty
            foreach (string pic in db.sizeDB.Keys) {
                //is this key in the list?
                // -yes, then do nothing
                // -no, ok we need to make a picture object and add it
                if (!ListContainsPic(list, pic)) {
                    string fullpath = dir.ToString() + "\\" + pic;
                    if (File.Exists(fullpath)) {
                        FileInfo f = new FileInfo(fullpath);
                        PictureClass p = new PictureClass(f.Name, f.FullName, f.Length, false, 0);
                        list.Add(p);
                    }
                }
            }
            return updated;
        }

        //features[] => intensity, color-code, energy, entropy, contrast
        static public ArrayList SelectFeatures(HistogramDB db, string filename, bool[] features) {
            ArrayList combined = new ArrayList();
            if (features[0]) { combined.AddRange(db.intensityDB[filename]); }
            if (features[1]) { combined.AddRange(db.colorCodeDB[filename]); }
            if (features[2]) { combined.Add(db.textureDB[filename][0]); } //energy
            if (features[3]) { combined.Add(db.textureDB[filename][1]); } //entropy
            if (features[4]) { combined.Add(db.textureDB[filename][2]); } //contrast
            //NormalizeGaussian(combined);
            return combined;
        }

        //generate a matrix with rows as selected features, and columns being images marked as relevant
        //if this matrix has less than 2 columns leave the weights are they are
        //otherwise find the standard deviation of each row (features) and adjust the weights
        //CRITICAL this function assumes that the database file exists and that it is in SYNC with the list of PictureClass objects
        static public void AdjustWeights(ArrayList weight, bool[] features, ArrayList list, string imageFoldPath) {
            if (list == null) { return; }

            //read the existing histogram file if there is one
            string dbFile = imageFoldPath + "\\" + HISTOGRAM_FILE;
            HistogramDB db = null;
            if (File.Exists(dbFile)) { db = (HistogramDB)HF.DeSerialize(dbFile); } else { throw new Exception("Image meta-data database not found."); }

            //initialize the matrix: rows (features), columns (relevant images)
            List<double>[] matrix = new List<double>[weight.Count]; //rows are [], columns are List<double>
            for (int i = 0; i < weight.Count; i++) { matrix[i] = new List<double>(); } //init the columns

            //build the matrix
            foreach (string pic in db.sizeDB.Keys) {
                bool relevant = list.OfType<PictureClass>().Single(p => p.name == pic).relevant;
                if (relevant) {
                    ArrayList selected = SelectFeatures(db, pic, features);
                    for (int i = 0; i < selected.Count; i++) {
                        matrix[i].Add((double)weight[i] * (double)selected[i]);
                    }
                }
            }

            //if we have at least 2 relevant pictures go ahead and adjust the weights; otherwise, don't do anything just leave the weights as they are
            if (matrix[0].Count > 1) {
                List<double> mean = new List<double>();  //mean == average value, mean != median
                List<double> sigma = new List<double>(); //sigma == standard deviation, sigma^2 == variance
                int S = matrix.Length; //there are S selected features
                int N = matrix[0].Count; //there are N relevant images

                //find the means and sigmas, one for each feature (row)
                for (int i = 0; i < S; i++) {
                    mean.Add(matrix[i].Sum() / N);
                    sigma.Add(Math.Sqrt(matrix[i].Sum(fi => (fi - mean[i]) * (fi - mean[i])) / (N - 1)));
                }

                //update the weights
                for (int i = 0; i < S; i++) {
                    if (mean[i] != 0 && sigma[i] == 0) {
                        sigma[i] = 0.5 * sigma.Where(stddev => stddev != 0).Min();
                        weight[i] = 1.0d / sigma[i];
                    } else if (mean[i] == 0) {
                        weight[i] = 0.0d;
                    } else {
                        weight[i] = 1.0d / sigma[i];
                    }
                }
                NoramlizeWeights(weight);
            }
        }

        #endregion

        #region generatefeatures

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
                    energy += Math.Pow(col.Value, 2); //N^2
                }
            }

            return energy;
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the entropy
        static public double CalcEntropy(Dictionary<int, Dictionary<int, double>> ngtcom) {
            double entropy = 0;
            foreach (KeyValuePair<int, Dictionary<int, double>> row in ngtcom) {
                foreach (KeyValuePair<int, double> col in row.Value) {
                    if (col.Value != 0) { entropy += col.Value * Math.Log(col.Value, 2); } //N Log N with log base 2
                }
            }

            return entropy;
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the entropy
        static public double CalcContrast(Dictionary<int, Dictionary<int, double>> ngtcom) {
            double contrast = 0;
            foreach (KeyValuePair<int, Dictionary<int, double>> row in ngtcom) {
                foreach (KeyValuePair<int, double> col in row.Value) {
                    contrast += Math.Pow(row.Key - col.Key, 2) * col.Value; //(i - j)^2 N
                }
            }

            return contrast;
        }

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

            NormalizeBySize(hist, picture.Width * picture.Height);
            return hist;
        }

        //given an image and a displacement vector (dr,dc) find the co-occurrence matrix
        //the upper left corner of the image is (0,0)
        //dr is the displacement rows downward
        //dc is the displacement columns to the right
        static public Dictionary<int, Dictionary<int, double>> CalcCoOccurrence(Bitmap Img, int dr, int dc) {
            int[][] image = new int[Img.Height][]; //initalize matrix of intensity values
            for (int r = 0; r < Img.Height; r++) { image[r] = new int[Img.Width]; }

            //build intensity matrix
            for (int r = 0; r < Img.Height; r++) {
                for (int c = 0; c < Img.Width; c++) {
                    image[r][c] = (int)CalcIntensity(Img.GetPixel(c, r));
                }
            }

            //for checking against the assignment
            //int[][] image = new int[][] {new int[]{2,2,0,0},
            //                             new int[]{2,2,0,0},
            //                             new int[]{0,0,3,3},
            //                             new int[]{0,0,3,3}};

            //grab a sorted list of unique intensity values from the image
            int[] values = image.SelectMany(value => value).Distinct().OrderBy(value => value).ToArray();

            //initalize a hash of hashes to hold the texture counts
            Dictionary<int, Dictionary<int, double>> counts = new Dictionary<int, Dictionary<int, double>>();
            foreach (int v in values) {
                counts.Add(v, new Dictionary<int, double>());
                foreach (int va in values) {
                    counts[v].Add(va, 0.0);
                }
            }

            //update counts using the displacement vector
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
                gray.Add(row.Key, new Dictionary<int, double>());
                foreach (KeyValuePair<int, double> col in row.Value) {
                    gray[row.Key][col.Key] = col.Value / sum;
                }
            }

            return gray;
        }

        #endregion

        #region normalization

        //will normalize so that each feature in the list is a percent between [0,1)
        //the entire vector will sum to 1 afterwards
        static private void NormalizeBySize(ArrayList features, int N) {
            //normalize bins by divding by the number of pixels
            //to account for images of different sizes
            for (int f = 0; f < features.Count; f++) {
                features[f] = (double)features[f] / N;
            }
        }

        //normlize the vector so that each feature is between [0,1]
        static private void NormalizeUniform(ArrayList features) {
            double min = features.OfType<double>().Min();
            double max = features.OfType<double>().Max();

            for (int f = 0; f < features.Count; f++) {
                features[f] = ((double)features[f] - min) / (max - min);
            }
        }

        //Intra-Normalization step and Inter-Normalization step combined not really following the article on this part
        //normlize the vector so that each feature is between [-3,3]
        static private void NormalizeGaussian(ArrayList features) {
            // standard deviation is sqrt(sum((value - mean)^2) / (N-1)) where N is number of items in the vector
            // see http://en.wikipedia.org/wiki/Standard_deviation#Discrete_random_variable for any questions

            if (features.Count > 1) {
                double mean = features.OfType<double>().Average(); //find the mean
                double sigma = Math.Sqrt(features.OfType<double>().Sum(f => (f - mean) * (f - mean)) / (features.Count - 1));

                if (sigma != 0) { //apply normalization       
                    for (int f = 0; f < features.Count; f++) {
                        features[f] = ((double)features[f] - mean) / (sigma);
                    }
                }
            }
        }

        //normalize the adjusted weights so that all weights sum to 1
        static private void NoramlizeWeights(ArrayList weight) {
            double sum = weight.OfType<double>().Sum();
            for (int i = 0; i < weight.Count; i++) {
                weight[i] = (double)weight[i] / sum;
            }
        }

        #endregion

        #region helperfunctions

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

        //p = 1 is manhattan distance function
        //p = 2 is euclidean distance function
        //p = higher increases side-effects between dimensions
        static private double CalculateDist(ArrayList Qhistogram, ArrayList histogram, ArrayList weight, int p) {
            double distance = 0.0;
            if (p <= 0) { throw new Exception("Invalid distance function P must be > 0"); }
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int i = 0; i < histogram.Count; i++) {
                distance += (double)weight[i] * Math.Pow(Math.Abs((double)Qhistogram[i] - (double)histogram[i]), p);
            }
            return Math.Pow(distance, (double)(1.0) / p);
        }

        //find an intensity given RGB values
        static public double CalcIntensity(Color p) {
            return (.299 * p.R) + (.587 * p.G) + (.114 * p.B);
        }

        //add a picture to the database
        static private void DBAddPicture(string fullname, string name, long filesize, HistogramDB db, int dr, int dc) {
            Bitmap picture = (Bitmap)Bitmap.FromFile(fullname);
            ArrayList intensityHist = CBIRfunctions.CalcIntensityHist(picture);
            ArrayList colorCodeHist = CBIRfunctions.CalcColorCodeHist(picture);
            ArrayList textureHist = CBIRfunctions.CalcTextureFeatures(picture, dr, dc);
            db.Add(name, filesize, intensityHist, colorCodeHist, textureHist);
            picture.Dispose();
        }

        static private bool ListContainsPic(ArrayList list, string pic) {
            bool exists = false;
            foreach (PictureClass p in list) {
                if (p.name.Equals(pic)) { exists = true; }
            }
            return exists;
        }

        #endregion
    }
}
