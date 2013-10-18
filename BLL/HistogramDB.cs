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
        public List<double> mean;
        public List<double> stddev;

        public HistogramDB() {
            intensityDB = new Dictionary<string, ArrayList>();
            colorCodeDB = new Dictionary<string, ArrayList>();
            textureDB = new Dictionary<string, ArrayList>();
            sizeDB = new Dictionary<string, long>();
            mean = new List<double>();
            stddev = new List<double>();
        }

        //constructor used by the deserialization process
        public HistogramDB(SerializationInfo info, StreamingContext ctxt) {
            this.sizeDB = (Dictionary<string, long>)info.GetValue("fileSize", typeof(Dictionary<string, long>));
            this.intensityDB = (Dictionary<string, ArrayList>)info.GetValue("intensityHistograms", typeof(Dictionary<string, ArrayList>));
            this.colorCodeDB = (Dictionary<string, ArrayList>)info.GetValue("colorCodeHistograms", typeof(Dictionary<string, ArrayList>));
            this.textureDB = (Dictionary<string, ArrayList>)info.GetValue("textureHistograms", typeof(Dictionary<string, ArrayList>));
            this.mean = (List<double>)info.GetValue("mean", typeof(List<double>));
            this.stddev = (List<double>)info.GetValue("stddev", typeof(List<double>));
        }

        //used by the serialization process so that serialize knows what to save
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("fileSize", this.sizeDB);
            info.AddValue("intensityHistograms", this.intensityDB);
            info.AddValue("colorCodeHistograms", this.colorCodeDB);
            info.AddValue("textureHistograms", this.textureDB);
            info.AddValue("mean", this.mean);
            info.AddValue("stddev", this.stddev);
        }

        //use gaussian normalization
        public void NormalizeFeatures() {
            ComputeStdDev();
            List<string> record = intensityDB.Keys.ToList<string>();
            for (int c = 0; c < record.Count; c++) {
                int intensity = intensityDB[record[c]].Count;
                int colorcode = colorCodeDB[record[c]].Count + intensity;
                int texture = textureDB[record[c]].Count + colorcode;

                //normalize intensityDB
                for (int i = 0; i < intensity; i++) {
                    if (intensityDB[record[c]].Count > 1 && (double)stddev[i] != 0) {
                        intensityDB[record[c]][i] = ((double)intensityDB[record[c]][i] - mean[i]) / stddev[i];
                        //if ((double)intensityDB[record[c]][i] > 3) { intensityDB[record[c]][i] = 3.0; }
                        //if ((double)intensityDB[record[c]][i] < -3) { intensityDB[record[c]][i] = -3.0; }
                    }
                }

                //normalize colorCodeDB
                for (int i = intensity; i < colorcode; i++) {
                    if (colorCodeDB[record[c]].Count > 1 && (double)stddev[i] != 0) {
                        colorCodeDB[record[c]][i - intensity] = ((double)colorCodeDB[record[c]][i - intensity] - mean[i]) / stddev[i];
                        //if ((double)colorCodeDB[record[c]][i - intensity] > 3) { colorCodeDB[record[c]][i - intensity] = 3.0; }
                        //if ((double)colorCodeDB[record[c]][i - intensity] < -3) { colorCodeDB[record[c]][i - intensity] = -3.0; }
                    }
                }

                //normalize textureDB
                for (int i = colorcode; i < texture; i++) {
                    if (textureDB[record[c]].Count > 1 && (double)stddev[i] != 0) {
                        textureDB[record[c]][i - colorcode] = ((double)textureDB[record[c]][i - colorcode] - mean[i]) / stddev[i];
                        //if ((double)textureDB[record[c]][i - colorcode] > 3) { textureDB[record[c]][i - colorcode] = 3.0; }
                        //if ((double)textureDB[record[c]][i - colorcode] < -3) { textureDB[record[c]][i - colorcode] = -3.0; }
                    }
                }
            }
        }

        public void ComputeStdDev() {
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

            for (int c = 0; c < feature.Length; c++) {
                double avg = ((ArrayList)feature[c]).OfType<double>().Average(); //find the mean
                double sum = ((ArrayList)feature[c]).OfType<double>().Sum(f => (f - avg) * (f - avg)); //get the numerator for std dev
                double stddev = Math.Sqrt(sum / (((ArrayList)feature[c]).Count - 1));
                this.mean.Add(avg);
                this.stddev.Add(stddev);
            }
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
            crap = BadNumberFinder(mean);
            crap = BadNumberFinder(stddev);
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
