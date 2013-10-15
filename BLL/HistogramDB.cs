using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Collections;

namespace CBIR
{
    [Serializable()]
    public class HistogramDB : ISerializable
    {
        public SortedDictionary<string, ArrayList> intensityDB;
        public SortedDictionary<string, ArrayList> colorCodeDB;
        public SortedDictionary<string, long> sizeDB;

        public HistogramDB(){
            intensityDB = new SortedDictionary<string, ArrayList>();
            colorCodeDB = new SortedDictionary<string, ArrayList>();
            sizeDB      = new SortedDictionary<string, long>();
        }

        //constructor used by the deserialization process
        public HistogramDB(SerializationInfo info, StreamingContext ctxt){
            sizeDB      = (SortedDictionary<string, long>)info.GetValue("fileSize", typeof(SortedDictionary<string, long>));
            intensityDB = (SortedDictionary<string, ArrayList>)info.GetValue("intensityHistograms", typeof(SortedDictionary<string, ArrayList>));
            colorCodeDB = (SortedDictionary<string, ArrayList>)info.GetValue("colorCodeHistograms", typeof(SortedDictionary<string, ArrayList>));
        }

        //used by the serialization process so that serialize knows what to save
        public void GetObjectData(SerializationInfo info, StreamingContext context){
            info.AddValue("fileSize", this.sizeDB);
            info.AddValue("intensityHistograms", this.intensityDB);
            info.AddValue("colorCodeHistograms", this.colorCodeDB);
        }

        public void Add(string filename, long size, ArrayList intensityHist, ArrayList colorCodeHist){
            sizeDB.Add(filename, size);
            intensityDB.Add(filename, intensityHist);
            colorCodeDB.Add(filename, colorCodeHist);
        }

        public void Remove(string filename){
            sizeDB.Remove(filename);
            intensityDB.Remove(filename);
            colorCodeDB.Remove(filename);
        }
    }
}
