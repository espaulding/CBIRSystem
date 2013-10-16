//This class is used to hold informatio about every picture in the database
using System;

namespace CBIR {
    class PictureClass : IComparable {
        //set up important information about picture
        public string name;
        public string path;
        public long size;
        public double distance;

        //when object create set values to the ones passed in
        public PictureClass(string name, string path, long size, double dist) {
            this.name = name;
            this.path = path;
            this.size = size;
            this.distance = dist;
        }

        //has to be implemented to use PictureClass objects as keys in a SortedDictionary
        //but the real implementation does not use this to sort the gallery
        public int CompareTo(object obj) {
            PictureClass rhs = (PictureClass)obj;
            if (this.distance < rhs.distance) { return -1; }
            if (this.distance > rhs.distance) { return 1; }
            return 0;
        }
    }
}
