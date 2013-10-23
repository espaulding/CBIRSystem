using System;
using System.Linq;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CBIR {
    //HF == HelperFunctions
    static class HF {
        #region serialization

        public static void Serialize(string filename, ISerializable objectToSerialize) {
            Stream stream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, objectToSerialize);
            stream.Close();
        }

        public static ISerializable DeSerialize(string filename) {
            ISerializable objectToSerialize;
            Stream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFormatter bFormatter = new BinaryFormatter();
            objectToSerialize = (ISerializable)bFormatter.Deserialize(stream);
            stream.Close();
            return objectToSerialize;
        }

        #endregion

        #region statistics

        //any number smaller than the threshold will be rounded to zero
        //threshold should be a string in scientific notation like "1.0E-10"
        static public float FixFloatingPoint(float number, string threshold) {
            return (float)FixFloatingPoint((double)number, threshold);
        }

        //any number smaller than the threshold will be rounded to zero
        //threshold should be a string in scientific notation like "1.0E-10"
        static public double FixFloatingPoint(double number, string threshold) {
            double limit = 0;
            Double.TryParse(threshold, out limit);
            if (Math.Abs(number) <= limit) { number = 0.0d; }
            return number;
        }

        //normlize the vector so that each feature is set to its number of stdev from the mean
        static public void NormalizeGaussian(List<double> vector) {
            // standard deviation is sqrt(sum((value - mean)^2) / (N-1)) where N is number of items in the vector
            // see http://en.wikipedia.org/wiki/Standard_deviation#Discrete_random_variable for any questions

            if (vector.Count > 1) {
                double mean = vector.Average(); //find the mean
                double sigma = Math.Sqrt(vector.Sum(f => Math.Pow(f - mean, 2) / (vector.Count - 1)));

                if (sigma != 0) { //apply normalization       
                    for (int f = 0; f < vector.Count; f++) {
                        vector[f] = (vector[f] - mean) / (sigma);
                    }
                }
            }
        }

        //normlize the vector so that each feature is between [0,1]
        //this does not mean the vector will sum to 1 afterwards
        static public void NormalizeUniform(List<double> vector) {
            double min = vector.Min();
            double max = vector.Max();

            for (int f = 0; f < vector.Count; f++) {
                vector[f] = (vector[f] - min) / (max - min);
            }
        }

        //normlize the vector so that each feature is between [0,1]
        //the vector will sum to 1 after this
        static public void NormalizeProportions(List<double> vector) {
            double sum = vector.Sum();
            if (sum == 0) { throw new Exception("The vector sums to zero"); }
            for (int i = 0; i < vector.Count; i++) {
                vector[i] = vector[i] / sum;
            }
        }

        //p = 1 is manhattan distance function
        //p = 2 is euclidean distance function
        //p = higher increases side-effects between dimensions
        static public double GetMinkowskiDist(List<double> point1, List<double> point2, int p) {
            double distance = 0.0;
            if (p <= 0) { throw new Exception("Invalid distance function P must be > 0"); }
            if (point1.Count != point2.Count) { throw new Exception("The points must have the same number of dimensions"); }
            for (int i = 0; i < point2.Count; i++) {
                //don't multiply by weight here because it's being done in the matrix as the weights are adjusted
                distance += Math.Pow(Math.Abs(point1[i] - point2[i]), p);
            }
            return Math.Pow(distance, 1.0d / p);
        }

        #endregion

        #region images

        static public Image BytesToImage(byte[] byteArray, ImageFormat formatOfImage) {
            try {
                using (MemoryStream ms = new MemoryStream(byteArray)) {
                    Image img = Image.FromStream(ms);
                    //img.Save(ms, formatOfImage); //this line is causing an exception
                    //TODO: look into this
                    return img;
                }
            } catch (Exception) {
                throw;
            }
        }

        static public Bitmap BytesToBitmap(byte[] byteArray) {
            try {
                using (MemoryStream ms = new MemoryStream(byteArray)) {
                    Bitmap img = (Bitmap)Image.FromStream(ms);
                    return img;
                }
            } catch (Exception) {
                throw;
            }
        }

        //overloaded function allowing scale to be an optional parameter
        static public Bitmap ScaleImage(Image image, int width, int height) {
            if (width == 0) {
                return ScaleImage(image, 0, height, "height");
            }
            if (height == 0) {
                return ScaleImage(image, width, 0, "width");
            }
            return ScaleImage(image, width, height, "");
        }

        //scale should be one of the following
        //ignore or noscale => the height and width will be set exactly ignoring the images aspect ratio
        //height => set the height explicity and then compute the width so that the original aspect ratio is maintained
        //width => set width explicity and then compute the width so that the original aspect ratio is maintained
        //default behavior => maintain the apsect ratio, and explicitely scale whichever dimension of the image is larger
        static public Bitmap ScaleImage(Image image, int width, int height, string scale) {
            /// Originally found at http://west-wind.com/weblog/posts/283.aspx
            /// returns bitmap or null
            Bitmap bmpOut = null;
            try {
                decimal aspectRatio; //use decimal because the floating point calculations
                // are much more precise as compared to float and double
                int scaledWidth = 0, scaledHeight = 0;

                //if no option is given default behavior is to maintain the apsect ratio
                //and explicitely scale whichever dimension of the image is larger
                if (!Regex.IsMatch(scale, @"ignore|noscale|height|width", RegexOptions.IgnoreCase)) {
                    if (image.Width > image.Height) {
                        scale = "width";
                    } else {
                        scale = "height";
                    }
                }

                switch (scale.ToLower()) {
                    case "ignore":
                    case "noscale": {
                            scaledHeight = height;
                            scaledWidth = width;
                            break;
                        }
                    case "height": {
                            aspectRatio = (decimal)height / image.Height;
                            scaledHeight = height;
                            scaledWidth = (int)(image.Width * aspectRatio);
                            break;
                        }
                    case "width": {
                            aspectRatio = (decimal)width / image.Width;
                            scaledWidth = width;
                            scaledHeight = (int)(image.Height * aspectRatio);
                            break;
                        }
                }

                // *** This code creates cleaner (though bigger) thumbnails and properly
                // *** handles GIF files better by generating a white background for
                // *** transparent images (as opposed to black)
                bmpOut = new Bitmap(scaledWidth, scaledHeight);
                Graphics g = Graphics.FromImage(bmpOut);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, scaledWidth, scaledHeight);
                g.Dispose(); //TODO: hmm see if this really is ok
            } catch (Exception) {
                if (bmpOut != null) { bmpOut.Dispose(); }
                return null; //if this is happening a lot look for possible memory leaks
            }

            return bmpOut;
        }

        #endregion
    }
}
