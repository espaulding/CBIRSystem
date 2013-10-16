//This class is used to display images in full size 
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CBIR {
    public partial class frmDisplayPicture : Form {
        public frmDisplayPicture() {
            InitializeComponent();
        }

        public void displayPicture(string filepath, string filename, double distance) {
            //set up an image object and set it to the image of the picture box
            //set the label to the file name
            Bitmap myImg = (Bitmap)Bitmap.FromFile(filepath);
            lblImage.Text = filename;
            lblDist.Text = distance.ToString("0.#####");
            Size s = new Size(myImg.Width + 16, myImg.Height + 78);
            this.Size = s; this.MinimumSize = s;
            pbDisplay.Image = myImg;

            //TODO: put in check for screen resolution so that if the image is larger than the screen
            //      the panel should automatically gain scrollbars and not let the form out grow the screen
        }

        private void frmDisplayPicture_ResizeEnd(object sender, EventArgs e) {
            pbDisplay.Image = HF.reScaleImage(pbDisplay.Image, pbDisplay.Width, pbDisplay.Height, "noscale");
        }

        private void pbDisplay_Paint(object sender, PaintEventArgs e) {
            frmDisplayPicture_ResizeEnd(sender, new EventArgs());
        }
    }
}
