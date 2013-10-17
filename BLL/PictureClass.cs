//This class is used to hold informatio about every picture in the database
using System;

namespace CBIR {
    class PictureClass {
        //set up important information about picture
        public string name;
        public string path;
        public long size;
        public double distance;
        public bool relevant;

        //when object create set values to the ones passed in
        public PictureClass(string name, string path, long size, bool relevant,double dist) {
            this.name = name;
            this.path = path;
            this.size = size;
            this.distance = dist;
            this.relevant = relevant;
        }
    }
}
