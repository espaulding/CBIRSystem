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

        //memoize the DB so that it can be updated between relevance feedback rounds
        static private DirectoryInfo dir = null;
        static private HistogramDB db = null;

        //This function finds the feature vectors for each picture as necessary
        //and then calculats the distance that each picture is from the query image
        //this is the big cheese right here
        //the whole point of CBIR is to call this function get the query results by updating the distances in the list objects
        static public void RankPictures(string qFilename, int distanceFunc, ArrayList weight, bool[] features, ArrayList list, bool feedback, bool memoizedDB) {
            if (feedback) { AdjustWeights(weight, features, list, memoizedDB); } 

            //update picture distances
            ArrayList qFeatures = SelectFeatures(qFilename, features);
            foreach (var file in dir.GetFiles("*.jpg")) {
                ArrayList picFeatures = SelectFeatures(file.Name, features);

                //find the picture object in the old list that matches the current file being processed
                PictureClass pic = list.OfType<PictureClass>().Single(p => p.name == file.Name);
                pic.distance = CalculateDist(qFeatures, picFeatures, weight, distanceFunc);
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
        static public void RefreshDB(string imageFoldPath, ref ArrayList oldlist, bool forceRebuild) {
            //displacement by rows, columns used by texture features
            int dr = 1, dc = 1; //hardcoded for now but maybe a parameter later
            dir = new DirectoryInfo(imageFoldPath);
            string dbFile = imageFoldPath + "\\" + HISTOGRAM_FILE;

            //if there is no oldlist start making a new one, but otherwise alias the old list as the new one
            ArrayList list = null;
            if (oldlist == null) { list = new ArrayList(); oldlist = list; } else { list = oldlist; }

            db = null;
            //read the existing histogram file if there is one
            if (File.Exists(dbFile)) { db = (HistogramDB)HF.DeSerialize(dbFile); } else { db = new HistogramDB(); }

            //if we found a new picture in the folder rebuild the database so that 
            //gaussian normalization can be reapplied over all the features
            if (UpdateDB(list) || forceRebuild) {
                RebuildDB(dr, dc); //new images found, so rebuild the db from scratch
                if (db.CheckData()) { throw new Exception("There are bad numbers in the database. (NaN, Infinity, etc)"); }
                db.NormalizeByFeatures(); //normalize each feature over a gaussian distribution
                //db.NormalizeByGlobal(); //normalize by the database's global mean and standard deviation over a gaussian distribution
                if (db.CheckData()) { throw new Exception("There are bad numbers in the database. (NaN, Infinity, etc)"); }
                HF.Serialize(dbFile, db); //save the new db since we had to rebuild it
            }
        }

        //re-aquire raw histograms for every image in the directory
        //this is the computational bottleneck for the entire software program
        //writing the data to file alleviates most of the issue, but it takes too long to build a new DB.
        static private void RebuildDB(int dr, int dc) {
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
        static private bool UpdateDB(ArrayList list) {
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

        //add a picture to the database
        static private void DBAddPicture(string fullname, string name, long filesize, HistogramDB db, int dr, int dc) {
            Bitmap picture = (Bitmap)Bitmap.FromFile(fullname);
            ArrayList intensityHist = CBIRfunctions.CalcIntensityHist(picture);
            ArrayList colorCodeHist = CBIRfunctions.CalcColorCodeHist(picture);
            ArrayList textureHist = CBIRfunctions.CalcTextureFeatures(picture, dr, dc);
            db.Add(name, filesize, intensityHist, colorCodeHist, textureHist);
            picture.Dispose();
        }

        //map selected features to the database and update it as the matrix is built
        static private void UpdateMemoizedDBValues(double value, int index, string pic, bool[] features) {
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
        //if this matrix has less than 2 columns leave the weights are they are
        //otherwise find the standard deviation of each row (features) and adjust the weights
        //CRITICAL this function assumes that the database file exists and that it is in SYNC with the list of PictureClass objects
        static private void AdjustWeights(ArrayList weight, bool[] features, ArrayList list, bool memoizedDB) {
            if (list == null) { return; } //stop immediately the function has been prematurely called

            //initialize the matrix: rows (features), columns (relevant images)
            List<double>[] matrix = new List<double>[weight.Count]; //rows are [], columns are List<double>
            for (int i = 0; i < weight.Count; i++) { matrix[i] = new List<double>(); } //init the columns

            //build the matrix of relevant images with selected features
            foreach (string pic in db.sizeDB.Keys) {
                if (list.OfType<PictureClass>().Single(p => p.name == pic).relevant) {
                    ArrayList selected = SelectFeatures(pic, features);
                    for (int i = 0; i < selected.Count; i++) { matrix[i].Add((double)selected[i]); }
                }
            }

            //if not memoized, reset the DB from the file after building the matrix with the previous rounds adjustments
            //which will allow the weights to propogate through the rounds
            //***RESULTS OF MEMOIZATION***
            //if the database is memoized all values will converge to zero, but at different rates
            //if the database is not memoized the weights will cycle radically presenting rediculous results every other round or so
            if (!memoizedDB) { RefreshDB(dir.FullName,ref list,false); }

            //if we have at least 2 relevant pictures go ahead and adjust the weights; otherwise, don't do anything just leave the weights as they are
            if (matrix[0].Count > 1) {
                List<double> mean = new List<double>();  //mean == average value, mean != median
                List<double> sigma = new List<double>(); //sigma == standard deviation, sigma^2 == variance
                int S = matrix.Length;   //there are S selected features
                int N = matrix[0].Count; //there are N relevant images

                //find the means and sigmas, one for each feature (row)
                for (int i = 0; i < S; i++) {
                    mean.Add(FixFloatingPoint(matrix[i].Sum() / N));
                    sigma.Add(FixFloatingPoint(Math.Sqrt(matrix[i].Sum(fi => (fi - mean[i]) * (fi - mean[i])) / (N - 1))));
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
                    weight[i] = FixFloatingPoint((double)weight[i]);
                }
                //set any placeholders weights to the max weight
                for (int i = 0; i < S; i++) { if ((double)weight[i] == -1) { weight[i] = weight.OfType<double>().Max(); } }
                NoramlizeWeights(weight);  
            }

            //apply the selected feature weights to the entire database even the non relevant images or things will get weird fast
            //if only the relevant images are updated in the db, those values will move towards 0
            //while the nonrelevant images won't change causing the true ordering of each feature to be lost immediately
            foreach (string pic in db.sizeDB.Keys) {
                ArrayList selected = SelectFeatures(pic, features);
                for (int i = 0; i < selected.Count; i++) {
                    double weightedValue = FixFloatingPoint((double)weight[i] * (double)selected[i]);
                    UpdateMemoizedDBValues(weightedValue, i, pic, features);
                }
            }
        }

        //normalize the adjusted weights so that all weights sum to 1
        static private void NoramlizeWeights(ArrayList weight) {
            double sum = weight.OfType<double>().Sum();
            if (sum != 0) {
                for (int i = 0; i < weight.Count; i++) {
                    weight[i] = FixFloatingPoint((double)weight[i] / sum);
                }
            }
        }

        #endregion

        #region helperfunctions

        //any number smaller than the limit will be rounded to zero
        static private double FixFloatingPoint(double d) {
            double limit = 0;
            Double.TryParse("1.0E-100", out limit);
            if (Math.Abs(d) <= limit) { d = 0.0d; }
            return d;
        }

        //p = 1 is manhattan distance function
        //p = 2 is euclidean distance function
        //p = higher increases side-effects between dimensions
        static private double CalculateDist(ArrayList Qhistogram, ArrayList histogram, ArrayList weight, int p) {
            double distance = 0.0;
            if (p <= 0) { throw new Exception("Invalid distance function P must be > 0"); }
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int i = 0; i < histogram.Count; i++) {
                //don't multiply by weight here because it's being done in the matrix as the weights are adjusted
                distance += Math.Pow(Math.Abs((double)Qhistogram[i] - (double)histogram[i]), p);
            }
            return Math.Pow(distance, (double)(1.0) / p);
        }

        //find an intensity given RGB values
        static public double CalcIntensity(Color p) {
            return (.299 * p.R) + (.587 * p.G) + (.114 * p.B);
        }

        static private bool ListContainsPic(ArrayList list, string pic) {
            bool exists = false;
            foreach (PictureClass p in list) {
                if (p.name.Equals(pic)) { exists = true; }
            }
            return exists;
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

        #endregion
    }
}
