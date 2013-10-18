using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace CBIR {
    [Serializable()]
    public class HistogramDB : ISerializable {
        public Dictionary<string, ArrayList> intensityDB;
        public Dictionary<string, ArrayList> colorCodeDB;
        public Dictionary<string, ArrayList> textureDB;
        public Dictionary<string, long> sizeDB;
        public List<double> meanByFeature;
        public List<double> sigmaByFeature;
        public double meanGlobal = 0;
        public double sigmaGlobal = 0;

        public HistogramDB() {
            intensityDB = new Dictionary<string, ArrayList>();
            colorCodeDB = new Dictionary<string, ArrayList>();
            textureDB = new Dictionary<string, ArrayList>();
            sizeDB = new Dictionary<string, long>();
            meanByFeature = new List<double>();
            sigmaByFeature = new List<double>();
        }

        //constructor used by the deserialization process
        public HistogramDB(SerializationInfo info, StreamingContext ctxt) {
            this.sizeDB = (Dictionary<string, long>)info.GetValue("fileSize", typeof(Dictionary<string, long>));
            this.intensityDB = (Dictionary<string, ArrayList>)info.GetValue("intensityHistograms", typeof(Dictionary<string, ArrayList>));
            this.colorCodeDB = (Dictionary<string, ArrayList>)info.GetValue("colorCodeHistograms", typeof(Dictionary<string, ArrayList>));
            this.textureDB = (Dictionary<string, ArrayList>)info.GetValue("textureHistograms", typeof(Dictionary<string, ArrayList>));
            this.meanByFeature = (List<double>)info.GetValue("mean", typeof(List<double>));
            this.sigmaByFeature = (List<double>)info.GetValue("stddev", typeof(List<double>));
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
        //this method gets aweful results
        public void NormalizeByGlobal() {
            ComputeGlobalStdDev();
            List<string> record = intensityDB.Keys.ToList<string>();
            for (int c = 0; c < record.Count; c++) {
                int intensity = intensityDB[record[c]].Count;
                int colorcode = colorCodeDB[record[c]].Count + intensity;
                int texture = textureDB[record[c]].Count + colorcode;

                //normalize intensityDB
                for (int i = 0; i < intensity; i++) {
                    if (intensityDB[record[c]].Count > 1 && sigmaGlobal != 0) {
                        intensityDB[record[c]][i] = ((double)intensityDB[record[c]][i] - meanGlobal) / sigmaGlobal;
                    }
                }

                //normalize colorCodeDB
                for (int i = intensity; i < colorcode; i++) {
                    if (colorCodeDB[record[c]].Count > 1 && sigmaGlobal != 0) {
                        colorCodeDB[record[c]][i - intensity] = ((double)colorCodeDB[record[c]][i - intensity] - meanGlobal) / sigmaGlobal;
                    }
                }

                //normalize textureDB
                for (int i = colorcode; i < texture; i++) {
                    if (textureDB[record[c]].Count > 1 && sigmaGlobal != 0) {
                        textureDB[record[c]][i - colorcode] = ((double)textureDB[record[c]][i - colorcode] - meanGlobal) / sigmaGlobal;
                    }
                }
            }
        }

        //use gaussian normalization
        //this method gets good results, but still isn't matching up with Min's sample output
        public void NormalizeByFeatures() {
            ComputeStdDevByFeature();
            List<string> record = intensityDB.Keys.ToList<string>();
            for (int c = 0; c < record.Count; c++) {
                int intensity = intensityDB[record[c]].Count;
                int colorcode = colorCodeDB[record[c]].Count + intensity;
                int texture = textureDB[record[c]].Count + colorcode;

                //normalize intensityDB
                for (int i = 0; i < intensity; i++) {
                    if (intensityDB[record[c]].Count > 1 && (double)sigmaByFeature[i] != 0) {
                        intensityDB[record[c]][i] = ((double)intensityDB[record[c]][i] - meanByFeature[i]) / sigmaByFeature[i];
                    }
                }

                //normalize colorCodeDB
                for (int i = intensity; i < colorcode; i++) {
                    if (colorCodeDB[record[c]].Count > 1 && (double)sigmaByFeature[i] != 0) {
                        colorCodeDB[record[c]][i - intensity] = ((double)colorCodeDB[record[c]][i - intensity] - meanByFeature[i]) / sigmaByFeature[i];
                    }
                }

                //normalize textureDB
                for (int i = colorcode; i < texture; i++) {
                    if (textureDB[record[c]].Count > 1 && (double)sigmaByFeature[i] != 0) {
                        textureDB[record[c]][i - colorcode] = ((double)textureDB[record[c]][i - colorcode] - meanByFeature[i]) / sigmaByFeature[i];
                    }
                }
            }
        }

        public ArrayList[] GenerateFeatureMatrix() {
            //there are 25 intensity features, 64 color-code features, and 3 texture features
            ArrayList[] feature = new ArrayList[25 + 64 + 3];
            foreach (KeyValuePair<string, ArrayList> record in intensityDB) {
                int c = 0;
                foreach (double f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            foreach (KeyValuePair<string, ArrayList> record in colorCodeDB) {
                int c = 25;
                foreach (double f in record.Value) {
                    if (feature[c] == null) { feature[c] = new ArrayList(); }
                    feature[c].Add(f);
                    c++;
                }
            }
            foreach (KeyValuePair<string, ArrayList> record in textureDB) {
                int c = 25 + 64;
                foreach (double f in record.Value) {
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
                double avg = ((ArrayList)feature[c]).OfType<double>().Average(); //find the mean
                double sum = ((ArrayList)feature[c]).OfType<double>().Sum(f => (f - avg) * (f - avg)); //get the numerator for std dev
                double sigma = Math.Sqrt(sum / (((ArrayList)feature[c]).Count - 1));
                this.meanByFeature.Add(avg);
                this.sigmaByFeature.Add(sigma);
            }
        }

        public void ComputeGlobalStdDev() {
            int numGlobal = 92 * sizeDB.Count; //The number of features times the number of images in the DB
            ArrayList[] feature = GenerateFeatureMatrix();

            //find the global mean and standard deviation by looking at the entire DB all at once
            meanGlobal = feature.Sum(al => al.OfType<double>().Sum()) / numGlobal;
            sigmaGlobal = feature.Sum(al => al.OfType<double>().Sum(f => (f - meanGlobal) * (f - meanGlobal))) / (numGlobal - 1);
        }

        public bool CheckData() {
            bool crap = false;
            foreach (ArrayList hist in intensityDB.Values) {
                crap = BadNumberFinder(hist.OfType<double>().ToList<double>());
            }
            foreach (ArrayList hist in colorCodeDB.Values) {
                crap = BadNumberFinder(hist.OfType<double>().ToList<double>());
            }
            foreach (ArrayList hist in textureDB.Values) {
                crap = BadNumberFinder(hist.OfType<double>().ToList<double>());
            }
            crap = BadNumberFinder(meanByFeature);
            crap = BadNumberFinder(sigmaByFeature);
            return crap;
        }

        private bool BadNumberFinder(List<double> vector) {
            bool crap = false;
            foreach (double d in vector) {
                if (double.IsNaN(d)) {
                    crap = true;
                }
                if (double.IsInfinity(d)) {
                    crap = true;
                }
                if (double.Epsilon == d) {
                    crap = true;
                }
                if (double.MaxValue == d) {
                    crap = true;
                }
                if (double.MinValue == d) {
                    crap = true;
                }

            }
            return crap;
        }

        public void Add(string filename, long size, ArrayList intensityHist, ArrayList colorCodeHist, ArrayList textureHist) {
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
