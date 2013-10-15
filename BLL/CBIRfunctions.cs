using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CBIR
{
    static public class CBIRfunctions
    {
        static public ArrayList IntensityMethod(Bitmap myImg){
            Color p;
            double I;
            int intensity, numBins = 25;
            ArrayList hist = new ArrayList(numBins);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0); }

            for (int x = 0; x < myImg.Width; x++){
                for (int y = 0; y < myImg.Height; y++){
                    p = myImg.GetPixel(x, y);
                    I = (.299 * p.R) + (.587 * p.G) + (.114 * p.B);
                    intensity = (int)Math.Floor(I / 10);
                    if (intensity > (numBins - 1)) { intensity = numBins - 1; } //don't let 250 and up move out of the histogram
                    hist[intensity] = (double)hist[intensity] + 1.0;
                }
            }

            //normalize bins to account for images of different sizes
            for (int bin = 0; bin < hist.Count; bin++){
                hist[bin] = (double)hist[bin] / (myImg.Width * myImg.Height);
            }

            return hist;
        }

        static public ArrayList ColorCodeMethod(Bitmap myImg){
            Color p;
            int numBins = 64;
            ArrayList hist = new ArrayList(numBins);
            for (int x = 0; x < hist.Capacity; x++) { hist.Add(0.0); }
            string RED, GREEN, BLUE;
            int bin;
            for (int x = 0; x < myImg.Width; x++){
                for (int y = 0; y < myImg.Height; y++){
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

            //normalize bins to account for images of different sizes
            for (bin = 0; bin < hist.Count; bin++){
                hist[bin] = (double)hist[bin] / (myImg.Width * myImg.Height);
            }

            return hist;
        }

        //manhattan distance function
        public static double calculateDist(ArrayList Qhistogram, ArrayList histogram){
            double distance = 0.0;
            if (Qhistogram.Count != histogram.Count) { throw new Exception("invalid histograms given to distance measure."); }
            for (int bin = 0; bin < histogram.Count; bin++){
                distance += Math.Abs((double)Qhistogram[bin] - (double)histogram[bin]);
            }
            return distance;
        }

        //This function finds the color histograms for each picture as necessary
        //and then calculats the distance that each picture is from the query image
        public static ArrayList calculatePictures(string imageFoldPath, string HISTOGRAM_FILE, string qFilename){
            ArrayList list = new ArrayList();
            bool histogramFileUPdated = false; //keep track if we update the histogram db by adding or removing an image file

            DirectoryInfo d = new DirectoryInfo(imageFoldPath); //get all pictures in the path
            string dbFile = imageFoldPath + "\\" + HISTOGRAM_FILE;

            HistogramDB db = null;
            //read the existing histogram file if there is one
            if (File.Exists(dbFile)) { db = (HistogramDB)HF.DeSerialize(dbFile); }
            else { db = new HistogramDB(); }

            //make sure the query image is in the db first
            if (!db.sizeDB.ContainsKey(qFilename)){
                Bitmap picture = (Bitmap)Bitmap.FromFile(imageFoldPath + "\\" + qFilename);
                db.Add(qFilename, 0L, CBIRfunctions.IntensityMethod(picture), CBIRfunctions.ColorCodeMethod(picture));
                picture.Dispose();
            }

            foreach (var file in d.GetFiles("*.jpg")){
                //look the file up in the current histogram data
                if (db.sizeDB.ContainsKey(file.Name) && !(db.sizeDB[file.Name] == file.Length)){ //we've seen this image before
                    db.Remove(file.Name); //the file changed size so delete it from our DB 
                }

                if (!db.sizeDB.ContainsKey(file.Name)){ //we've never seen this image or we just removed it
                    histogramFileUPdated = true;        //so process it from scratch and add to the db
                    Bitmap picture = (Bitmap)Bitmap.FromFile(file.FullName);
                    ArrayList intensityHist = CBIRfunctions.IntensityMethod(picture);
                    ArrayList colorCodeHist = CBIRfunctions.ColorCodeMethod(picture);
                    db.Add(file.Name, file.Length, intensityHist, colorCodeHist);
                    picture.Dispose();
                }

                PictureClass pic = new PictureClass(file.Name, file.FullName, file.Length,
                 calculateDist(db.intensityDB[qFilename], db.intensityDB[file.Name]),
                 calculateDist(db.colorCodeDB[qFilename], db.colorCodeDB[file.Name]));
                list.Add(pic);
            }

            //changes were made to the histogram data so save over the old stuff
            if (histogramFileUPdated) { HF.Serialize(dbFile, db); }
            return list;
        }
    }
}
