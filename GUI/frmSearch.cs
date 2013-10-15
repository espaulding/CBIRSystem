using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace CBIR
{
    public partial class frmSearch : Form
    {
        string queryFileName;
        ResultofSearch resultForm;
        public frmSearch()
        {
            InitializeComponent();
        }

        private void frmSearch_Load(object sender, EventArgs e)
        {
            rbIntensity.Checked = true;
        }

        private void btnLoadPicture_Click(object sender, EventArgs e)
        {
           //open windows dialog and limit it to jpeg file only
            ofdLoadPicture.Filter = "JPEG Files|*.jpg";
            ofdLoadPicture.FilterIndex = 1;
            ofdLoadPicture.FileName = "";
            ofdLoadPicture.ShowDialog();

            if (ofdLoadPicture.FileName != "")
            {
                //open the file from the dialog and show it in the picute box
                queryFileName = ofdLoadPicture.FileName;
                Bitmap myImg = (Bitmap)Bitmap.FromFile(queryFileName);
                pbQueryPicture.Image = HF.reScaleImage(myImg, pbQueryPicture.Width, pbQueryPicture.Height,"noscale");
                btnSearch.Enabled = true;
            }
        }

        //when search button clicked pass information to the form to display it to screen
        private void btnSearch_Click(object sender, EventArgs e)
        {
            FileInfo newFile = new FileInfo(queryFileName);
            string path = newFile.DirectoryName;
            string qFile = newFile.Name;
            resultForm = new ResultofSearch(this);
            btnSearch.Visible = false;
            lblProcess.Visible = true;
            this.Refresh();
            resultForm.doSearch(qFile, path, rbIntensity.Checked);
            resultForm.Show();
            this.Hide();
            btnSearch.Visible = true;
            lblProcess.Visible = false;
        }
    }
}


