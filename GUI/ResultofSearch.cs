﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections;

namespace CBIR {
    public partial class ResultofSearch : Form {
        //set up global variables for class
        public const int IMAGES_PER_PAGE = 20;
        ArrayList list; //list of PictureClass objects
        ArrayList weight; //the weight of each feature column in this round of relevance feedback
        frmSearch originalForm;
        int page, totalPages, distanceFunc = 1;
        string imageFoldPath, queryPic;

        //form class constructor
        public ResultofSearch(frmSearch form) {
            //initialize information when form is created
            originalForm = form;
            InitializeComponent();
        }

        //get or set all of the top radio buttons and check boxes on the form
        public bool[][] State {
            get {
                bool[][] state = new bool[][] { 
                                  new bool[] { rbManhattan.Checked, rbEuclidean.Checked },
                                  new bool[] { cbRelevanceFeedback.Checked },
                                  new bool[] { cbIntensity.Checked, cbColorCode.Checked, cbTextureEnergy.Checked, cbTextureEntropy.Checked, cbTextureContrast.Checked}
                                 };
                return state;
            }
            set {
                rbManhattan.Checked         = value[0][0];
                rbEuclidean.Checked         = value[0][1];
                cbRelevanceFeedback.Checked = value[1][0];
                cbIntensity.Checked         = value[2][0];
                cbColorCode.Checked         = value[2][1];
                cbTextureEnergy.Checked     = value[2][2];
                cbTextureEntropy.Checked    = value[2][3];
                cbTextureContrast.Checked   = value[2][4];
            }
        }

        #region FormAndControlEvents

        //basically a form_load function to initialize the form as it's brought up
        //function to search through database based on the Query Picture sent in
        public void DoSearch(string queryPicture, string folderPath) {
            imageFoldPath = folderPath;
            queryPic = queryPicture;

            //display the query image
            pbQueryPicture.Image = HF.reScaleImage(Bitmap.FromFile(imageFoldPath + "\\" + queryPicture),
                                                   pbQueryPicture.Width, pbQueryPicture.Height, "noscale");

            InitNewSearch();
            btnSearch_Click(new object(), new EventArgs());
        }

        //clear info if application closed
        private void ResultofSearch_FormClosing(object sender, FormClosingEventArgs e) {
            originalForm.Close();
        }

        #region ControlPanelButtons

        //save the forms state and let the user choose a new query image
        private void btnChangeQuery_Click(object sender, EventArgs e) {
            originalForm.State = this.State; //hand off the state so the next search result will match this one
            originalForm.Show();
            this.Dispose();
        }

        private void btnSearch_Click(object sender, EventArgs e) {
            page = 0;
            bool[] features = { cbIntensity.Checked, cbColorCode.Checked, cbTextureEnergy.Checked, cbTextureEntropy.Checked, cbTextureContrast.Checked };
            bool memoizedDB = true;
            //***RESULTS OF MEMOIZATION***
            //if the database is memoized all values will converge to zero, but at different rates, causing some features to drop off early
            //if the database is not memoized the weights will cycle radically presenting rediculous results every other round or so

            //do the search
            CBIRfunctions.RankPictures(queryPic, distanceFunc, weight, features, list, cbRelevanceFeedback.Checked, memoizedDB);

            //re-sort the list of images by their distances
            List<PictureClass> sorted = list.OfType<PictureClass>().OrderBy(pic => pic.distance).ToList<PictureClass>();
            for (int x = 0; x < list.Count; x++) { list[x] = sorted[x]; }

            //calculate the number of pages needed if we want 20 pictures per page
            totalPages = (int)Math.Ceiling(list.Count / (double)IMAGES_PER_PAGE);
            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
            DisplayImageResults();
        }

        //close the form. i.e. quit the appliation
        private void btnClose_Click(object sender, EventArgs e) {
            this.Close();
        }

        //reset window is next button is clicked
        private void Next_Click(object sender, EventArgs e) {
            if (page < (totalPages - 1)) {
                page += 1;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                DisplayImageResults();
            } else {
                page = 0;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                DisplayImageResults();
            }
        }

        //reset window if previous button is clicked
        private void Previous_Click(object sender, EventArgs e) {
            if (page > 0) {
                page -= 1;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                DisplayImageResults();
            } else {
                page = (totalPages - 1);
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                DisplayImageResults();
            }
        }

        #endregion

        #region ControlPanelOptions

        private void cbFeature_CheckedChanged(object sender, EventArgs e) {
            bool selected = false;

            //if any feature is selected allow searches to proceed; otherwise, disable searches
            foreach (object cb in gbFeatures.Controls) {
                if (((CheckBox)cb).Checked) {
                    selected = true;
                }
            }

            if (selected) {
                btnSearch.Enabled = true;
            } else {
                btnSearch.Enabled = false;
            }

            InitNewSearch();
        }

        //set program to use manhattan distance function
        private void rbManhattan_CheckedChanged(object sender, EventArgs e) {
            if (rbManhattan.Checked) {
                distanceFunc = 1;
            }
        }

        //set program to use euclidean distance function
        private void rbEuclidean_CheckedChanged(object sender, EventArgs e) {
            if (rbEuclidean.Checked) {
                distanceFunc = 2;
            }
        }

        //turn relevance feedback on and off
        private void cbRelevanceFeedback_CheckedChanged(object sender, EventArgs e) {
            //toggle the feedback check boxes to display or not display
            for (int c = 20; c < 40; c++) {
                ((CheckBox)gbGallery.Controls[c]).Visible = cbRelevanceFeedback.Checked;
            }

            InitNewSearch();
        }

        #endregion

        #region Gallery

        //this function is used to display the results of the search
        public void DisplayImageResults() {
            //offset used to scan through the list and get the right pictures based on the current page we are on 
            int offSet = page * IMAGES_PER_PAGE;

            //set thumbnail as the image of the picturebox starting from one and moving across then down
            for (int pic = 0; pic < IMAGES_PER_PAGE; pic++) {
                PictureBox box = (PictureBox)gbGallery.Controls[pic]; //pictureboxes are in the gallery.Controls [0,19]
                //check to make sure we are still with in the array and picture exists
                if ((pic + offSet) < list.Count && File.Exists(((PictureClass)list[pic + offSet]).path)) {
                    //get the image from the file using the file path and create thumbnail of it
                    Bitmap img = (Bitmap)Bitmap.FromFile(((PictureClass)list[pic + offSet]).path);
                    img = HF.reScaleImage(img, 0, 95, "height");
                    box.Width = img.Width;
                    box.Height = img.Height;
                    box.Image = img;
                } else {
                    //if we are past the array or image does not exist them set image in picturebox to null to null
                    box.Image = null;
                }
            }
            DisplayRelevantImages();
        }

        //this function is used to display the which images are marked as relevant
        public void DisplayRelevantImages() {
            //offset used to scan through the list and get the right pictures based on the current page we are on 
            int offSet = page * IMAGES_PER_PAGE;

            for (int pic = 0; pic < IMAGES_PER_PAGE; pic++) {
                //check to make sure we are still with in the array and picture exists
                if ((pic + offSet) < list.Count && File.Exists(((PictureClass)list[pic + offSet]).path)) {
                    //set the relevant checkbox for this image... checkboxs are in the gallery.Controls [20,39]
                    ((CheckBox)gbGallery.Controls[pic + 20]).Checked = ((PictureClass)list[pic + offSet]).relevant;
                } else {
                    ((PictureClass)list[pic + offSet]).relevant = false; //TODO: hmm does this really make sense?
                }
            }
        }

        //Pop open a new form with a full size version of the thumbnail that was just clicked
        private void PictureBox_Click(object sender, EventArgs e) {
            PictureBox pic = (PictureBox)sender;
            int number;
            Int32.TryParse(pic.Name.Substring(10, 2), out number);
            int gNumber = --number + IMAGES_PER_PAGE * page;

            if (list[gNumber] != null) {
                PictureClass objPicData = (PictureClass)list[gNumber];
                frmDisplayPicture displayForm = new frmDisplayPicture();
                displayForm.displayPicture(objPicData.path, objPicData.name, objPicData.distance);
                displayForm.Show();
            }
        }

        //toggle a picture as relevant or not relevant
        private void cbRelevant_CheckedChanged(object sender, EventArgs e) {
            CheckBox pic = (CheckBox)sender;
            int number;
            Int32.TryParse(pic.Name.Substring(10, 2), out number);
            int gNumber = --number + IMAGES_PER_PAGE * page;
            ((PictureClass)list[gNumber]).relevant = pic.Checked;
        }

        //mark every picture as not relevant and reset weights on all features
        private void InitNewSearch() {
            if (imageFoldPath != null) {
                CBIRfunctions.RefreshDB(imageFoldPath, ref list, false);
                if (list != null) {
                    //wipe out any old information concerning whether a picture is relevant or not
                    for (int c = 0; c < list.Count; c++) {
                        ((PictureClass)list[c]).relevant = false;
                    }
                }
                bool[] features = { cbIntensity.Checked, cbColorCode.Checked, cbTextureEnergy.Checked, cbTextureEntropy.Checked, cbTextureContrast.Checked };
                weight = CBIRfunctions.GetInitialWeights(features);
                if (list != null) { DisplayRelevantImages(); }
            }
        }

        #endregion //Gallery

        #endregion //FormAndControlEvents 
    }
}
