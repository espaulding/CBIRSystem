using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;

namespace CBIR
{
    public partial class ResultofSearch : Form
    {
        //set up global variables for class
        public const int IMAGES_PER_PAGE = 20;
        public const string HISTOGRAM_FILE = "histogram.dat";
        ArrayList list; //list of PictureClass objects
        frmSearch originalForm;
        int page, totalPages;
        string imageFoldPath;

        //form class constructor
        public ResultofSearch(frmSearch form){
            //initialize information when form is created
            originalForm = form;
            InitializeComponent();
        }

        #region FormAndControlEvents

        //basically a form_load function to initialize the form as it's brought up
        //function to search through database based on the Query Picture sent in
        public void doSearch(string queryPicture, string folderPath, bool method1){
            imageFoldPath = folderPath;

            //get color histograms for all the other images in the database and find their distace from the query image
            list = CBIRfunctions.calculatePictures(folderPath, HISTOGRAM_FILE, queryPicture);

            //calculate the number of pages needed if we want 20 pictures per page
            double numberPerPage = (list.Count / (double)IMAGES_PER_PAGE);
            totalPages = (int)Math.Ceiling(numberPerPage);
            pageLabel.Text = "Page 1 /Out of " + totalPages;
            page = 0;

            //display the query image
            pbQueryPicture.Image = HF.reScaleImage(Bitmap.FromFile(imageFoldPath + "\\" + queryPicture), 
                                                   pbQueryPicture.Width, pbQueryPicture.Height, "noscale");

            //set up the check next to the right check box
            if (method1) { rbIntensity_Click(new object(), new EventArgs()); }
            else         { rbColor_Click(new object(), new EventArgs()); }
        }

        //this function is used to display the results of the search to the screen
        //TODO: fix so that the last page doesn't have errors when the total number
        //      of images is not divisable by 20
        public void displayResults(){
            //offset used to scan through the list and get the right pictures based on the current page we are on 
            int offSet = page * IMAGES_PER_PAGE;

            //set thumbnail as the image of the picturebox starting from one and moving across then down
            for (int pic = 0; pic < IMAGES_PER_PAGE; pic++){
                PictureBox box = (PictureBox)pnlGallery.Controls[pic];
                //check to make sure we are still with in the array and picture exists
                if ((pic + offSet) < list.Count && File.Exists(((PictureClass)list[pic + offSet]).path)){
                    //get the image from the file using the file path and create thumbnail of it
                    Bitmap img = (Bitmap)Bitmap.FromFile(((PictureClass)list[pic + offSet]).path);
                    box.Image = HF.reScaleImage(img, box.Width, box.Height, "noscale"); 
                }else{
                    //if we are past the array or image does not exist them set image in picturebox to null and path to null
                    ((PictureClass)list[pic + offSet]).path = null;
                    box.Image = null;
                }
            }
        }

        //reset screen if new search is clicked
        private void NewSearch_Click(object sender, EventArgs e){
            originalForm.Show();
            this.Hide();
        }

        //reset window is next button is clicked
        private void Next_Click(object sender, EventArgs e){
            pnlGallery.VerticalScroll.Value = 0; 
            pnlGallery.HorizontalScroll.Value = 0;

            if (page < (totalPages - 1)){
                page += 1;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();
            }else{
                page = 0;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();
            }
        }

        //reset window if previous button is clicked
        private void Previous_Click(object sender, EventArgs e)
        {
            pnlGallery.VerticalScroll.Value = 0;
            pnlGallery.HorizontalScroll.Value = 0;

            if (page > 0){
                page -= 1;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();
            }else{
                page = (totalPages - 1);
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();
            }
        }

        //clear info if application closed
        private void ResultofSearch_FormClosing(object sender, FormClosingEventArgs e){
            originalForm.Close();
        }

        //reset window if method is changed
        private void rbIntensity_Click(object sender, EventArgs e){
            rbIntensity.Checked = true;
            page = 0;

            SortedDictionary<PictureClass,double> gallery = new SortedDictionary<PictureClass,double>();
            for (int x = 0; x < list.Count; x++){
                gallery.Add((PictureClass)list[x], ((PictureClass)list[x]).intensityDist);
            }
            sortPictures(gallery);

            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
            displayResults();
        }

        //reset window if method is changed
        private void rbColor_Click(object sender, EventArgs e)
        {
            rbColor.Checked = true;
            page = 0;

            SortedDictionary<PictureClass, double> gallery = new SortedDictionary<PictureClass, double>();
            for (int x = 0; x < list.Count; x++)
            {
                gallery.Add((PictureClass)list[x], ((PictureClass)list[x]).colorCodeDist);
            }
            sortPictures(gallery);

            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
            displayResults();
        }

        //Pop open a new form with a full size version of the thumbnail that was just clicked
        private void pictureBox_Click(object sender, EventArgs e){
            PictureBox pic = (PictureBox)sender;
            int number;
            Int32.TryParse(pic.Name.Substring(10, 2), out number);
            int gNumber = --number + IMAGES_PER_PAGE * page;

            if (list[gNumber] != null){
                double dist = 0.0;
                PictureClass objPicData = (PictureClass)list[gNumber];
                if (rbIntensity.Checked) { dist = objPicData.intensityDist; }
                else                     { dist = objPicData.colorCodeDist; }

                frmDisplayPicture displayForm = new frmDisplayPicture();
                displayForm.displayPicture(objPicData.path, objPicData.name, dist);
                displayForm.Show();
            }
        }

        #endregion

        #region Local_CBIRfunctions

        //sort the gallery by whatever list of doubles has been coupled with the pictures
        //and then update the global main list with pictures in the correct order
        private void sortPictures(SortedDictionary<PictureClass, double> gallery){
            //make an enumerator that follows the order of the distance values
            IOrderedEnumerable<KeyValuePair<PictureClass, double>> sortedGallery;
            sortedGallery = gallery.OrderBy(kvp => kvp.Value);

            int counter = 0;
            foreach (KeyValuePair<PictureClass, double> pic in sortedGallery){
                list[counter++] = pic.Key; //use the ordered enumerator to update the global image list
            }
        }

        #endregion
    }
}
