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

        public HistogramDB() {
            intensityDB = new Dictionary<string, ArrayList>();
            colorCodeDB = new Dictionary<string, ArrayList>();
            textureDB   = new Dictionary<string, ArrayList>();
            sizeDB      = new Dictionary<string, long>();
        }

        //constructor used by the deserialization process
        public HistogramDB(SerializationInfo info, StreamingContext ctxt) {
            sizeDB =      (Dictionary<string, long>)info.GetValue("fileSize", typeof(SortedDictionary<string, long>));
            intensityDB = (Dictionary<string, ArrayList>)info.GetValue("intensityHistograms", typeof(Dictionary<string, ArrayList>));
            colorCodeDB = (Dictionary<string, ArrayList>)info.GetValue("colorCodeHistograms", typeof(Dictionary<string, ArrayList>));
            textureDB =   (Dictionary<string, ArrayList>)info.GetValue("textureHistograms", typeof(Dictionary<string, ArrayList>));
        }

        //used by the serialization process so that serialize knows what to save
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("fileSize", this.sizeDB);
            info.AddValue("intensityHistograms", this.intensityDB);
            info.AddValue("colorCodeHistograms", this.colorCodeDB);
            info.AddValue("textureHistograms", this.textureDB);
        }

        public double Mean {
            get {
                ArrayList all = new ArrayList();
                foreach (KeyValuePair<string, ArrayList> record in intensityDB) {
                    all.AddRange(record.Value);
                }
                foreach (KeyValuePair<string, ArrayList> record in colorCodeDB) {
                    all.AddRange(record.Value);
                }
                foreach (KeyValuePair<string, ArrayList> record in textureDB) {
                    all.AddRange(record.Value);
                }
                double mean = all.OfType<double>().Average(); //find the mean

                return mean;
            }
        }

        public double StdDev {
            get {
                ArrayList all = new ArrayList();
                foreach (KeyValuePair<string, ArrayList> record in intensityDB) {
                    all.AddRange(record.Value);
                }
                foreach (KeyValuePair<string, ArrayList> record in colorCodeDB) {
                    all.AddRange(record.Value);
                }
                foreach (KeyValuePair<string, ArrayList> record in textureDB) {
                    all.AddRange(record.Value);
                }
                double mean = all.OfType<double>().Average(); //find the mean
                double sum = all.OfType<double>().Sum(f => (f - mean) * (f - mean)); //get the numerator for std dev
                double stddev = Math.Sqrt(sum / (all.Count - 1));
                return stddev;
            }
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
