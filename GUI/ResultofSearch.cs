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
        public const string HISTOGRAM_FILE = "imageFeatures.dat";
        public const int BOX_HEIGHT = 110;

        List<ImageMetaData> list; //list of ImageMetaData objects
        frmSearch originalForm;
        private int page, totalPages, distanceFunc = 1;
        private string queryPic;
        DirectoryInfo imageFolder;

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
                                  new bool[] { cbRelevanceFeedback.Checked, cbGaussian.Checked, cbUniform.Checked },
                                  new bool[] { cbIntensity.Checked, cbColorCode.Checked, cbTextureEnergy.Checked, cbTextureEntropy.Checked, cbTextureContrast.Checked}
                                 };
                return state;
            }
            set {
                rbManhattan.Checked         = value[0][0];
                rbEuclidean.Checked         = value[0][1];
                cbRelevanceFeedback.Checked = value[1][0];
                cbGaussian.Checked          = value[1][1];
                cbUniform.Checked           = value[1][2];
                cbIntensity.Checked         = value[2][0];
                cbColorCode.Checked         = value[2][1];
                cbTextureEnergy.Checked     = value[2][2];
                cbTextureEntropy.Checked    = value[2][3];
                cbTextureContrast.Checked   = value[2][4];
            }
        }

        #region FormAndControlEvents

        //basically a form_load function to initialize the form as it's brought up
        //also complete the first search with default settings
        public void DoSearch(string queryPicture, string folderPath) {
            imageFolder = new DirectoryInfo(folderPath);
            queryPic = queryPicture;

            //display the query image
            pbQueryPicture.Image = HF.ScaleImage(Bitmap.FromFile(imageFolder.FullName + "\\" + queryPicture),
                                                 pbQueryPicture.Width, pbQueryPicture.Height, "noscale");

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
            bool[] options = { cbRelevanceFeedback.Checked, cbGaussian.Checked, cbUniform.Checked };
            CBIRfunctions.RankPictures(imageFolder, HISTOGRAM_FILE, queryPic, distanceFunc, features, options, ref list);

            //re-sort the list of images by the distances found during ranking
            List<ImageMetaData> sorted = list.OfType<ImageMetaData>().OrderBy(pic => pic.distance).ToList<ImageMetaData>();
            for (int x = 0; x < list.Count; x++) { list[x] = sorted[x]; }

            DisplayImageResults();
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
        }

        //set program to use manhattan distance function
        private void rbDistFunction_CheckedChanged(object sender, EventArgs e) {     
            RadioButton rb = (RadioButton)sender;
            if (rb.Name.Equals("rbManhattan")) {
                distanceFunc = 1; //P == 1 is manhattan dist
            } else {
                distanceFunc = 2; //P == 2 is euclidean dist
            }
            if (!btnSearch.Enabled) { return; } //searching isn't allowed right now
            btnSearch_Click(sender, e);
        }

        //turn relevance feedback on and off
        private void cbRelevanceFeedback_CheckedChanged(object sender, EventArgs e) {
            InitNewSearch();
            if (cbRelevanceFeedback.Checked) {
                MessageBox.Show("Left click the images to toggle relevance.\nGreen bordered images are relevant.\nRed bordered images are not relevant.");
            }
        }

        private void cbGaussian_CheckedChanged(object sender, EventArgs e) {
            if (cbGaussian.Checked) {
                cbUniform.Checked = false;
            }
        }

        private void cbUniform_CheckedChanged(object sender, EventArgs e) {
            if (cbUniform.Checked) {
                cbGaussian.Checked = false;
            }
        }

        #endregion

        #region Gallery

        //this function is used to display the results of the search
        public void DisplayImageResults() {
            //calculate the number of pages needed if we want 20 pictures per page
            totalPages = (int)Math.Ceiling(list.Count / (double)IMAGES_PER_PAGE);
            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;

            //offset used to scan through the list and get the right pictures based on the current page we are on 
            int offSet = page * IMAGES_PER_PAGE;

            //set thumbnail as the image of the picturebox starting from one and moving across then down
            for (int pic = 0; pic < IMAGES_PER_PAGE; pic++) {
                PictureBox box = (PictureBox)gbGallery.Controls[pic]; //pictureboxes are in the gallery.Controls [0,19]
                //check to make sure we are still with in the array and picture exists
                if ((pic + offSet) < list.Count && File.Exists(list[pic + offSet].path)) {
                    //get the image from the file using the file path and create thumbnail of it
                    Bitmap img = (Bitmap)Bitmap.FromFile(list[pic + offSet].path);
                    img = HF.ScaleImage(img, 0, BOX_HEIGHT);
                    box.Width = img.Width + box.Padding.Left + box.Padding.Right;
                    box.Height = img.Height + box.Padding.Top + box.Padding.Bottom;
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
                if ((pic + offSet) < list.Count && File.Exists(list[pic + offSet].path)) {
                    //set the relevant checkbox for this image... checkboxs are in the gallery.Controls [20,39]
                    if (cbRelevanceFeedback.Checked) {
                        if (!list[pic].name.Equals(queryPic)) {
                            if (list[pic + offSet].relevant) {
                                gbGallery.Controls[pic].BackColor = Color.Green;
                            } else {
                                gbGallery.Controls[pic].BackColor = Color.Red;
                            }
                        } else {
                            gbGallery.Controls[pic].BackColor = Color.Green;
                        }
                    } else {
                        gbGallery.Controls[pic].BackColor = this.BackColor;
                    }
                } else {
                    //use the form's color if relevance feedback is off or there is no image for the box
                    gbGallery.Controls[pic].BackColor = this.BackColor;
                }
            }
        }

        //Pop open a new form with a full size version of the thumbnail that was just clicked
        private void PictureBox_Click(object sender, MouseEventArgs e) {
            PictureBox pic = (PictureBox)sender;
            int number;
            Int32.TryParse(pic.Name.Substring(10, 2), out number);
            int gNumber = --number + IMAGES_PER_PAGE * page;

            if (e.Button == MouseButtons.Left) {
                ToggleRelevant(number, gNumber, !list[gNumber].relevant);
            }
            if (e.Button == MouseButtons.Right) {
                if (gNumber < list.Count && list[gNumber] != null) {
                    ImageMetaData objPicData = (ImageMetaData)list[gNumber];
                    frmDisplayPicture displayForm = new frmDisplayPicture();
                    displayForm.displayPicture(objPicData.path, objPicData.name, objPicData.distance);
                    displayForm.Show();
                }
            }
        }

        //toggle a picturebox/picture as relevant or not
        private void ToggleRelevant(int number, int gNumber, bool relevant) {
            if (cbRelevanceFeedback.Checked) {
                if (list[gNumber].name.Equals(queryPic)) {
                    list[gNumber].relevant = true;
                    gbGallery.Controls[number].BackColor = Color.Green;
                } else {
                    if (gNumber < list.Count) {
                        list[gNumber].relevant = relevant;
                        if (list[gNumber].relevant) {
                            gbGallery.Controls[number].BackColor = Color.Green;
                        } else {
                            gbGallery.Controls[number].BackColor = Color.Red;
                        }
                    } else {
                        //use the form's color if there is no image for this box
                        gbGallery.Controls[number].BackColor = this.BackColor;
                    }
                }
            } else {
                gbGallery.Controls[number].BackColor = this.BackColor;
            }
        }

        //mark every picture as not relevant and reset weights on all features
        private void InitNewSearch() {
            if (imageFolder != null) {
                if (list != null) {
                    //wipe out any old information concerning whether a picture is relevant or not
                    for (int c = 0; c < list.Count; c++) {
                        list[c].relevant = false;
                        if (list[c].name.Equals(queryPic)) {
                            list[c].relevant = true;
                        }
                    }
                }
                if (list != null) { DisplayRelevantImages(); }
            }
        }

        #endregion //Gallery

        #endregion //FormAndControlEvents 

       
    }
}
