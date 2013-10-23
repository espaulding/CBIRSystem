using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace CBIR {
    [Serializable()]
    public class FeaturesDB : ISerializable {
        public Dictionary<string, ArrayList> intensityDB;
        public Dictionary<string, ArrayList> colorCodeDB;
        public Dictionary<string, ArrayList> textureDB;
        public Dictionary<string, long> sizeDB;
        public List<decimal> meanByFeature;
        public List<decimal> sigmaByFeature;
        public bool normalized = false;

        public FeaturesDB() {
            intensityDB = new Dictionary<string, ArrayList>();
            colorCodeDB = new Dictionary<string, ArrayList>();
            textureDB = new Dictionary<string, ArrayList>();
            sizeDB = new Dictionary<string, long>();
            meanByFeature = new List<decimal>();
            sigmaByFeature = new List<decimal>();
        }

        //constructor used by the deserialization process
        public FeaturesDB(SerializationInfo info, StreamingContext ctxt) {
            this.sizeDB = (Dictionary<string, long>)info.GetValue("fileSize", typeof(Dictionary<string, long>));
            this.intensityDB = (Dictionary<string, ArrayList>)info.GetValue("intensityHistograms", typeof(Dictionary<string, ArrayList>));
            this.colorCodeDB = (Dictionary<string, ArrayList>)info.GetValue("colorCodeHistograms", typeof(Dictionary<string, ArrayList>));
            this.textureDB = (Dictionary<string, ArrayList>)info.GetValue("textureHistograms", typeof(Dictionary<string, ArrayList>));
            this.meanByFeature = (List<decimal>)info.GetValue("mean", typeof(List<decimal>));
            this.sigmaByFeature = (List<decimal>)info.GetValue("stddev", typeof(List<decimal>));
        }

        //used by the serialization process so that serialize knows what to save
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("fileSize", this.sizeDB);
            info.AddValue("intensityHistograms", this.intensityDB);
            info.AddValue("colorCodeHistograms", this.colorCodeDB);
            info.AddValue("textureHistograms", this.textureDB);
            info.AddValue("mean", this.meanByFeature);
            info.AddValue("stddev", this.sigmaByFeature);
        }

        //use gaussian normalization
        //this method gets good results, but still isn't matching up with Min's sample output
        public void NormalizeByFeatures() {
            if (normalized) { return; } //don't let the db get normalized more than once
            ComputeStdDevByFeature();
            List<string> record = intensityDB.Keys.ToList<string>();
            for (int c = 0; c < record.Count; c++) {
                int intensity = intensityDB[record[c]].Count;
                int colorcode = colorCodeDB[record[c]].Count + intensity;
                int texture = textureDB[record[c]].Count + colorcode;
                //decimal outlierCutoff = 4.5; //anything greater than 3 stdev from the mean is 1% of a gaussian distribution
                                            //however many features are falling 5 and 6 stdev which is a clear demonstration of how
                                            //poorly the gaussian distribution actually approximates these features

                //normalize intensityDB
                for (int i = 0; i < intensity; i++) {
                    if (intensityDB[record[c]].Count > 1 && (decimal)sigmaByFeature[i] != 0) {
                        intensityDB[record[c]][i] = ((decimal)intensityDB[record[c]][i] - meanByFeature[i]) / sigmaByFeature[i];

                        //I feel like this actually improves the search results, but it also deviates from the example docs
                        //so leave it commented for now

                        //put a cap on the intensity of outliers so that sigma cutoffs can be more consistent during weight adjustment
                        //if ((decimal)intensityDB[record[c]][i] > outlierCutoff) { intensityDB[record[c]][i] = outlierCutoff; }
                        //if ((decimal)intensityDB[record[c]][i] < -outlierCutoff) { intensityDB[record[c]][i] = -outlierCutoff; }
                    }
                }

                //normalize colorCodeDB
                for (int i = intensity; i < colorcode; i++) {
                    if (colorCodeDB[record[c]].Count > 1 && (decimal)sigmaByFeature[i] != 0) {
                        colorCodeDB[record[c]][i - intensity] = ((decimal)colorCodeDB[record[c]][i - intensity] - meanByFeature[i]) / sigmaByFeature[i];

                        //put a cap on the intensity of outliers so that sigma cutoffs can be more consistent during weight adjustment
                        //if ((decimal)colorCodeDB[record[c]][i - intensity] > outlierCutoff) { colorCodeDB[record[c]][i - intensity] = outlierCutoff; }
                        //if ((decimal)colorCodeDB[record[c]][i - intensity] < -outlierCutoff) { colorCodeDB[record[c]][i - intensity] = -outlierCutoff; }
                    }
                }

                //normalize textureDB
                for (int i = colorcode; i < texture; i++) {
                    if (textureDB[record[c]].Count > 1 && (decimal)sigmaByFeature[i] != 0) {
                        textureDB[record[c]][i - colorcode] = ((decimal)textureDB[record[c]][i - colorcode] - meanByFeature[i]) / sigmaByFeature[i];

                        //put a cap on the intensity of outliers so that sigma cutoffs can be more consistent during weight adjustment
                        //if ((decimal)textureDB[record[c]][i - colorcode] > outlierCutoff) { textureDB[record[c]][i - colorcode] = outlierCutoff; }
                        //if ((decimal)textureDB[record[c]][i - colorcode] < -outlierCutoff) { textureDB[record[c]][i - colorcode] = -outlierCutoff; }
                    }
                }
            }
            normalized = true;
        }

        public ArrayList[] GenerateFeatureMatrix() {
            //there are 25 intensity features, 64 color-code features, and 3 texture features
            ArrayList[] feature = new ArrayList[25 + 64 + 3];
            foreach (KeyValuePair<string, ArrayList> record in intensityDB) {
                int c = 0;
                foreach (decimal f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            foreach (KeyValuePair<string, ArrayList> record in colorCodeDB) {
                int c = 25;
                foreach (decimal f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            foreach (KeyValuePair<string, ArrayList> record in textureDB) {
                int c = 25 + 64;
                foreach (decimal f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            return feature;
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

        public void Add(string filename, long size, ArrayList intensityHist, ArrayList colorCodeHist, ArrayList textureHist) {
            if (normalized) { throw new Exception("Can't add items to a normalized DB. Reload or remake the DB before adding this item"); }
            sizeDB.Add(filename, size);
            intensityDB.Add(filename, intensityHist);
            colorCodeDB.Add(filename, colorCodeHist);
            textureDB.Add(filename, textureHist);
        }

        public void Remove(string filename) {
            sizeDB.Remove(filename);
            intensityDB.Remove(filename);
            colorCodeDB.Remove(filename);
            textureDB.Remove(filename);
        }
    }
}
