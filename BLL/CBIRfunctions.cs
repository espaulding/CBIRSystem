using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CBIR {
    static public class CBIRfunctions {

        //This function finds the feature vectors for each picture as necessary
        //and then calculats the distance that each picture is from the query image
        //this is the big cheese right here
        //the whole point of CBIR is to call this function get the query results by updating the distances in the list objects
        static public void RankPictures(FeaturesDB db, string qFilename, int distanceFunc, bool[] features, ArrayList list, bool feedback) {
            db.SynchDB(list);
            List<decimal> weight = GetInitialWeights(features); //get unbiased weights
            GetPictureByName(list, qFilename).relevant = true; //the query image is always relevant
            if (feedback) { AdjustWeights(db, weight, features, list); } //bias weights with selected features of relevant images

            //update picture distances
            ArrayList qFeatures = db.SelectFeatures(qFilename, features);
            foreach (var file in db.folder.GetFiles("*.jpg")) {
                ArrayList picFeatures = db.SelectFeatures(file.Name, features);

                //find the picture object in the old list that matches the current file being processed
                ImageMetaData pic = GetPictureByName(list,file.Name);
                pic.distance = GetMinkowskiDist(qFeatures, picFeatures, weight, distanceFunc);
            }
        }

        #region generatefeatures

        //get the intensity histogram
        static public ArrayList CalcIntensityHist(Bitmap picture) {
            double I;
            int intensity;
            ArrayList hist = new ArrayList(FeaturesDB.INTENSITY_BIN_COUNT);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0M); }

            for (int x = 0; x < picture.Width; x++) {
                for (int y = 0; y < picture.Height; y++) {
                    I = CalcIntensity(picture.GetPixel(x, y));
                    intensity = (int)Math.Floor(I / 10);
                    if (intensity > (FeaturesDB.INTENSITY_BIN_COUNT - 1)) { intensity = FeaturesDB.INTENSITY_BIN_COUNT - 1; } //don't let 250 and up move out of the histogram
                    hist[intensity] = (decimal)hist[intensity] + 1M;
                }
            }

            //allow true comparison between images of different sizes
            NormalizeBySize(hist, picture.Width * picture.Height);
            return hist;
        }

        //get the color-code histogram
        static public ArrayList CalcColorCodeHist(Bitmap picture) {
            Color p;
            ArrayList hist = new ArrayList(FeaturesDB.COLOR_CODE_BIN_COUNT);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0M); }
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
                    hist[bin] = (decimal)hist[bin] + 1;
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
                features[f] = (decimal)features[f] / N;
            }
        }

        //get the texture features
        //they are always in the order: Energy, Entropy, Contrast
        static public ArrayList CalcTextureFeatures(Bitmap picture, int dr, int dc) {
            Dictionary<int, Dictionary<int, decimal>> ngtcom = CalcGrayTone(CalcCoOccurrence(picture, dr, dc));
            ArrayList hist = new ArrayList(3);
            hist.Add(CalcEnergy(ngtcom));
            hist.Add(CalcEntropy(ngtcom));
            hist.Add(CalcContrast(ngtcom));

            return hist;
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the energy
        static public decimal CalcEnergy(Dictionary<int, Dictionary<int, decimal>> ngtcom) {
            decimal energy = 0;
            foreach (KeyValuePair<int, Dictionary<int, decimal>> row in ngtcom) {
                foreach (KeyValuePair<int, decimal> col in row.Value) {
                    energy += col.Value * col.Value; //N^2
                }
            }

            return energy;
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the entropy
        static public decimal CalcEntropy(Dictionary<int, Dictionary<int, decimal>> ngtcom) {
            decimal entropy = 0;
            foreach (KeyValuePair<int, Dictionary<int, decimal>> row in ngtcom) {
                foreach (KeyValuePair<int, decimal> col in row.Value) {
                    if (col.Value != 0) {
                        entropy += col.Value * Convert.ToDecimal(Math.Log((double)col.Value, 2)); //N Log N with log base 2
                    }  
                }
            }

            return entropy;
        }

        //given a Normalized Gray-Tone Co-Occurrence Matrix, com, find the entropy
        static public decimal CalcContrast(Dictionary<int, Dictionary<int, decimal>> ngtcom) {
            decimal contrast = 0;
            foreach (KeyValuePair<int, Dictionary<int, decimal>> row in ngtcom) {
                foreach (KeyValuePair<int, decimal> col in row.Value) {
                    contrast += Convert.ToDecimal(Math.Pow(row.Key - col.Key, 2)) * col.Value; //(i - j)^2 N
                }
            }

            return contrast;
        }

        //given an image and a displacement vector (dr,dc) find the co-occurrence matrix
        //the upper left corner of the image is (0,0)
        //dr is the displacement rows downward
        //dc is the displacement columns to the right
        static public Dictionary<int, Dictionary<int, int>> CalcCoOccurrence(Bitmap Img, int dr, int dc) {
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
            Dictionary<int, Dictionary<int, int>> counts = new Dictionary<int, Dictionary<int, int>>();
            foreach (int v in values) {
                counts.Add(v, new Dictionary<int, int>());
                foreach (int va in values) { counts[v].Add(va, 0); }
            }

            //update counts using the displacement vector that defines this texture
            for (int r = 0; r < image.Length - dr; r++) {
                for (int c = 0; c < image[r].Length - dc; c++) {
                    counts[image[r][c]][image[r + dr][c + dc]]++;
                }
            }

            return counts;
        }

        static public Dictionary<int, Dictionary<int, decimal>> CalcGrayTone(Dictionary<int, Dictionary<int, int>> com) {
            //sum up all the values in the co-occurrence matrix
            int sum = com.Sum(kvpRow => kvpRow.Value.Sum(kvpCol => kvpCol.Value));

            //initalize a hash of hashes for the normalized gray tone matrix
            Dictionary<int, Dictionary<int, decimal>> gray = new Dictionary<int, Dictionary<int, decimal>>();

            //normalize each value by the sum. i.e. uniform distribution
            foreach (KeyValuePair<int, Dictionary<int, int>> row in com) {
                //instantiate the column and add it to the current row
                gray.Add(row.Key, new Dictionary<int, decimal>());

                //normalize and add the com value to the ngtcom
                foreach (KeyValuePair<int, int> col in row.Value) {
                    gray[row.Key][col.Key] = Convert.ToDecimal(col.Value) / sum;
                }
            }

            return gray;
        }

        #endregion

        #region relevancefeedback

        //get initial weights such that all weights sum to 1 and are equal
        static public List<decimal> GetInitialWeights(bool[] features) {
            int vectorLength = 0;
            if (features[0]) { vectorLength += FeaturesDB.INTENSITY_BIN_COUNT; }
            if (features[1]) { vectorLength += FeaturesDB.COLOR_CODE_BIN_COUNT; }
            if (features[2]) { vectorLength += 1; } //energy
            if (features[3]) { vectorLength += 1; } //entropy
            if (features[4]) { vectorLength += 1; } //contrast

            decimal w = 1.0M / vectorLength;
            List<decimal> weight = new List<decimal>(vectorLength);
            for (int x = 0; x < weight.Capacity; x++) { weight.Add(w); }
            return weight;
        }

        //generate a matrix with rows as selected features, and columns being images marked as relevant
        //if this matrix has less than 2 columns set weight to the balanced initial weights
        //otherwise find the standard deviation of each row (features) and adjust the weights
        //CRITICAL this function assumes that the database file is in SYNC with the list of PictureClass objects
        static private void AdjustWeights(FeaturesDB db, List<decimal> weight, bool[] features, ArrayList list) {
            if (list == null || db == null) { return; } //stop immediately the function has been prematurely called
            //possibly consider throwing an exception here rather than quietly ignoring the call

            //initialize the matrix: rows (features), columns (relevant images)
            List<decimal>[] matrix = new List<decimal>[weight.Count]; //rows are [], columns are List<double>
            for (int i = 0; i < weight.Count; i++) { matrix[i] = new List<decimal>(); } //init the columns

            //build the matrix of relevant images with selected features
            foreach (string pic in db.sizeDB.Keys) {
                if (list.OfType<ImageMetaData>().Single(p => p.name == pic).relevant) {
                    ArrayList selected = db.SelectFeatures(pic, features);
                    for (int i = 0; i < selected.Count; i++) { matrix[i].Add((decimal)selected[i]); }
                }
            }

            //if we have at least 2 relevant pictures go ahead and adjust the weights; otherwise, don't do anything
            if (matrix[0].Count > 1) {
                List<decimal> mean = new List<decimal>();  //mean == average value, mean != median
                List<decimal> sigma = new List<decimal>(); //sigma == standard deviation, sigma^2 == variance
                int S = matrix.Length;   //there are S selected features
                int N = matrix[0].Count; //there are N relevant images

                //find the means and sigmas, one for each feature (row)
                for (int i = 0; i < S; i++) {
                    mean.Add(matrix[i].Sum() / N);
                    double stdev = Math.Sqrt((double)(matrix[i].Sum(fi => (fi - mean[i]) * (fi - mean[i])) / (N - 1)));
                    sigma.Add(Convert.ToDecimal(stdev));
                }

                //update the weights         
                for (int i = 0; i < S; i++) {
                    if (mean[i] != 0 && sigma[i] == 0) { //nonzero mean but the column has no variance: should get high weight
                        if (sigma.Where(stddev => stddev != 0).Count() > 0) {
                            sigma[i] = 0.5M * sigma.Where(stddev => stddev != 0).Min();
                            weight[i] = 1.0M / sigma[i];
                        } else {
                            weight[i] = -1.0M; //placeholder so we can come back in a moment and set this to the max weight
                        }
                    } else if (mean[i] == 0) { //mean is zero so this feature has no information, set weight to zero
                        weight[i] = 0.0M;
                    } else {                   //adjust weight so that features with less variance get more emphasis
                        weight[i] = 1.0M / sigma[i];
                    }
                    weight[i] = HF.FixFloatingPoint(weight[i], "1.0E-15");
                }
                //set any placeholders weights to the max weight
                for (int i = 0; i < S; i++) { if (weight[i] == -1) { weight[i] = weight.Max(); } }
                NoramlizeWeights(weight);
            }
        }

        //normalize the adjusted weights so that all weights sum to 1
        static private void NoramlizeWeights(List<decimal> weight) {
            decimal sum = weight.Sum();
            if (sum != 0) {
                for (int i = 0; i < weight.Count; i++) {
                    weight[i] = HF.FixFloatingPoint(weight[i] / sum, "5.0E-8");
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
        static private double GetMinkowskiDist(ArrayList Qhistogram, ArrayList histogram, List<decimal> weight, int p) {
            decimal distance = 0; //use decimal for intermediate computations to help avoid floating point errors
            if (p <= 0) { throw new Exception("Invalid distance function P must be > 0"); }
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int i = 0; i < histogram.Count; i++) {
                distance += weight[i] * Convert.ToDecimal(Math.Pow((double)Math.Abs(Convert.ToDecimal(Qhistogram[i]) - Convert.ToDecimal(histogram[i])), p));
            }
            return Math.Pow((double)distance, 1.0d / p);
        }

        //find an intensity given RGB values
        static public double CalcIntensity(Color p) {
            return (.299 * p.R) + (.587 * p.G) + (.114 * p.B);
        }

        #endregion
    }
}
