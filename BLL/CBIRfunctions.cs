using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CBIR {
    static public class CBIRfunctions {
        #region preprocessdata

        //This function finds the color histograms for each picture as necessary
        //and then calculats the distance that each picture is from the query image
        public static ArrayList calculatePictures(string imageFoldPath, string HISTOGRAM_FILE, string qFilename, int distanceFunc, bool[] features) {
            //displacement by rows, columns used by texture features
            int dr = 1, dc = 1; //hardcoded for now but maybe a parameter later
            ArrayList list = new ArrayList();
            bool histogramFileUPdated = false; //keep track if we update the histogram db by adding or removing an image file

            DirectoryInfo d = new DirectoryInfo(imageFoldPath); //get all pictures in the path
            string dbFile = imageFoldPath + "\\" + HISTOGRAM_FILE;

            HistogramDB db = null;
            //read the existing histogram file if there is one
            if (File.Exists(dbFile)) { db = (HistogramDB)HF.DeSerialize(dbFile); } else { db = new HistogramDB(); }

            //make sure the query image is in the db first
            if (!db.sizeDB.ContainsKey(qFilename)) {
                Bitmap picture = (Bitmap)Bitmap.FromFile(imageFoldPath + "\\" + qFilename);
                db.Add(qFilename, 0L, CBIRfunctions.CalcIntensityHist(picture), 
                                      CBIRfunctions.CalcColorCodeHist(picture), 
                                      CBIRfunctions.CalcTextureFeatures(picture,dr,dc));
                picture.Dispose();
            }
            ArrayList qFeatures = CombineFeatures(db,qFilename,features);

            foreach (var file in d.GetFiles("*.jpg")) {
                //look the file up in the current histogram data
                if (db.sizeDB.ContainsKey(file.Name) && !(db.sizeDB[file.Name] == file.Length)) { //we've seen this image before
                    db.Remove(file.Name); //the file changed size so delete it from our DB 
                }

                if (!db.sizeDB.ContainsKey(file.Name)) { //we've never seen this image or we just removed it
                    histogramFileUPdated = true;        //so process it from scratch and add to the db
                    Bitmap picture = (Bitmap)Bitmap.FromFile(file.FullName);
                    ArrayList intensityHist = CBIRfunctions.CalcIntensityHist(picture);
                    ArrayList colorCodeHist = CBIRfunctions.CalcColorCodeHist(picture);
                    ArrayList textureHist = CBIRfunctions.CalcTextureFeatures(picture, dr, dc);
                    db.Add(file.Name, file.Length, intensityHist, colorCodeHist, textureHist);
                    picture.Dispose();
                }

                ArrayList picFeatures = CombineFeatures(db, file.Name, features);
                if (file.Name.Equals(qFilename)) { qFeatures = picFeatures; }
                PictureClass pic = new PictureClass(file.Name, file.FullName, file.Length,
                 calculateDist(qFeatures, picFeatures, distanceFunc));
                list.Add(pic);
            }

            //changes were made to the histogram data so save over the old stuff
            if (histogramFileUPdated) { HF.Serialize(dbFile, db); }
            return list;
        }

        //features[] => intensity, color-code, energy, entropy, contrast
        static public ArrayList CombineFeatures(HistogramDB db, string filename, bool[] features) {
            ArrayList combined = new ArrayList();
            if (features[0]) { combined.AddRange(db.intensityDB[filename]); }
            if (features[1]) { combined.AddRange(db.colorCodeDB[filename]); }
            if (features[2]) { combined.Add(db.textureDB[filename][0]); } //energy
            if (features[3]) { combined.Add(db.textureDB[filename][1]); } //entropy
            if (features[4]) { combined.Add(db.textureDB[filename][2]); } //contrast

            NormalizeGaussian(combined);
            return combined;
        }

        #endregion

        #region generatefeatures

        //get the texture features
        //they are always in the order: Energy, Entropy, Contrast
        static public ArrayList CalcTextureFeatures(Bitmap myImg, int dr, int dc) {
            Dictionary<int, Dictionary<int, double>> ngtcom = CalcGrayTone(CalcCoOccurrence(myImg, dr, dc));
            ArrayList hist = new ArrayList(3);
            hist.Add(CalcEnergy(ngtcom));
            hist.Add(CalcEntropy(ngtcom));
            hist.Add(CalcContrast(ngtcom));

            return hist;
        }

        //get the intensity histogram
        static public ArrayList CalcIntensityHist(Bitmap myImg) {
            double I;
            int intensity, numBins = 25;
            ArrayList hist = new ArrayList(numBins);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0); }

            for (int x = 0; x < myImg.Width; x++) {
                for (int y = 0; y < myImg.Height; y++) {
                    I = CalcIntensity(myImg.GetPixel(x, y));
                    intensity = (int)Math.Floor(I / 10);
                    if (intensity > (numBins - 1)) { intensity = numBins - 1; } //don't let 250 and up move out of the histogram
                    hist[intensity] = (double)hist[intensity] + 1.0;
                }
            }

            NormalizeBySize(hist, myImg.Width * myImg.Height);
            return hist;
        }

        //get the color-code histogram
        static public ArrayList CalcColorCodeHist(Bitmap myImg) {
            Color p;
            int numBins = 64;
            ArrayList hist = new ArrayList(numBins);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0); }
            string RED, GREEN, BLUE;
            int bin;
            for (int x = 0; x < myImg.Width; x++) {
                for (int y = 0; y < myImg.Height; y++) {
                    p = myImg.GetPixel(x, y);

                    //convert to base 2 and add leading zeros up until we have 8 digits
                    RED = Convert.ToString(p.R, 2).PadLeft(8, '0');
                    GREEN = Convert.ToString(p.G, 2).PadLeft(8, '0');
                    BLUE = Convert.ToString(p.B, 2).PadLeft(8, '0');

                    //convert from string of base 2 to an integer in base 10
                    bin = Convert.ToInt32(RED.Substring(0, 2) + GREEN.Substring(0, 2) + BLUE.Substring(0, 2), 2);
                    hist[bin] = (double)hist[bin] + 1;
                }
            }

            NormalizeBySize(hist, myImg.Width * myImg.Height);
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
        static public void NormalizeBySize(ArrayList features, int N) {
            //normalize bins by divding by the number of pixels
            //to account for images of different sizes
            for (int f = 0; f < features.Count; f++) {
                features[f] = (double)features[f] / N;
            }
        }

        //normlize the vector so that each feature is between [0,1]
        static public void NormalizeUniform(ArrayList features) {
            double min = features.OfType<double>().Min();
            double max = features.OfType<double>().Max();

            for (int f = 0; f < features.Count; f++) {
                features[f] = ((double)features[f] - min) / (max - min);
            }
        }

        //Intra-Normalization step
        //normlize the vector so that each feature is between [0,1]
        static public void NormalizeGaussian(ArrayList features) {
            // standard deviation is sqrt(sum(value - mean) / N) where N is number of items in the vector
            // see http://en.wikipedia.org/wiki/Standard_deviation#Discrete_random_variable for any questions

            double avg = features.OfType<double>().Average(); //find the mean
            double sum = features.OfType<double>().Sum(f => (f - avg) * (f - avg)); //get the numerator for std dev
            double stddev = Math.Sqrt(sum / features.Count);

            if (stddev != 0) { //apply normalization       
                for (int f = 0; f < features.Count; f++) {
                    features[f] = ((double)features[f] - avg) / (3 * stddev);
                    if ((double)features[f] < -1) { features[f] = -1.0; }
                    if ((double)features[f] > 1) { features[f] = 1.0; }
                    //features[f] = ((double)features[f] + 1) / 2; //finally normalize bewtween [0,1]
                }
            }
        }

        //Inter-Normalization step
        //see equations 14, 15, 16, 17

        #endregion

        #region helperfunctions

        // manhattan distance function
        public static double calculateDist(ArrayList Qhistogram, ArrayList histogram) {
            double distance = 0.0;
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int bin = 0; bin < histogram.Count; bin++) {
                distance += Math.Abs((double)Qhistogram[bin] - (double)histogram[bin]);
            }
            return distance;
        }

        //p = 1 is manhattan distance function
        //p = 2 is euclidean distance function
        //p = higher increases side-effects between dimensions
        public static double calculateDist(ArrayList Qhistogram, ArrayList histogram, int p) {
            double distance = 0.0;
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int bin = 0; bin < histogram.Count; bin++) {
                distance += Math.Pow(Math.Abs((double)Qhistogram[bin] - (double)histogram[bin]), p);
            }
            return Math.Pow(distance, (double)(1.0) / p);
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

        //find an intensity given RGB values
        static public double CalcIntensity(Color p) {
            return (.299 * p.R) + (.587 * p.G) + (.114 * p.B);
        }

        #endregion
    }
}
