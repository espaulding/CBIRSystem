namespace CBIR
{
    partial class frmSearch
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnLoadPicture = new System.Windows.Forms.Button();
            this.ofdLoadPicture = new System.Windows.Forms.OpenFileDialog();
            this.pbQueryPicture = new System.Windows.Forms.PictureBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.rbIntensity = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbColor = new System.Windows.Forms.RadioButton();
            this.lblProcess = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbQueryPicture)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnLoadPicture
            // 
            this.btnLoadPicture.Location = new System.Drawing.Point(317, 211);
            this.btnLoadPicture.Name = "btnLoadPicture";
            this.btnLoadPicture.Size = new System.Drawing.Size(155, 23);
            this.btnLoadPicture.TabIndex = 0;
            this.btnLoadPicture.Text = "Load Query Picture";
            this.btnLoadPicture.UseVisualStyleBackColor = true;
            this.btnLoadPicture.Click += new System.EventHandler(this.btnLoadPicture_Click);
            // 
            // pbQueryPicture
            // 
            this.pbQueryPicture.Location = new System.Drawing.Point(33, 32);
            this.pbQueryPicture.Name = "pbQueryPicture";
            this.pbQueryPicture.Size = new System.Drawing.Size(243, 231);
            this.pbQueryPicture.TabIndex = 2;
            this.pbQueryPicture.TabStop = false;
            // 
            // btnSearch
            // 
            this.btnSearch.Enabled = false;
            this.btnSearch.Location = new System.Drawing.Point(317, 240);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(155, 23);
            this.btnSearch.TabIndex = 6;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // rbIntensity
            // 
            this.rbIntensity.AutoSize = true;
            this.rbIntensity.Location = new System.Drawing.Point(18, 19);
            this.rbIntensity.Name = "rbIntensity";
            this.rbIntensity.Size = new System.Drawing.Size(103, 17);
            this.rbIntensity.TabIndex = 7;
            this.rbIntensity.TabStop = true;
            this.rbIntensity.Text = "Intensity Method";
            this.rbIntensity.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbColor);
            this.groupBox1.Controls.Add(this.rbIntensity);
            this.groupBox1.Location = new System.Drawing.Point(317, 105);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(155, 75);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Search Method";
            // 
            // rbColor
            // 
            this.rbColor.AutoSize = true;
            this.rbColor.Location = new System.Drawing.Point(18, 42);
            this.rbColor.Name = "rbColor";
            this.rbColor.Size = new System.Drawing.Size(116, 17);
            this.rbColor.TabIndex = 8;
            this.rbColor.TabStop = true;
            this.rbColor.Text = "Color-Code Method";
            this.rbColor.UseVisualStyleBackColor = true;
            // 
            // lblProcess
            // 
            this.lblProcess.AutoSize = true;
            this.lblProcess.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProcess.Location = new System.Drawing.Point(307, 243);
            this.lblProcess.Name = "lblProcess";
            this.lblProcess.Size = new System.Drawing.Size(175, 16);
            this.lblProcess.TabIndex = 9;
            this.lblProcess.Text = "Processing your search.";
            this.lblProcess.Visible = false;
            // 
            // frmSearch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(511, 304);
            this.Controls.Add(this.lblProcess);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.pbQueryPicture);
            this.Controls.Add(this.btnLoadPicture);
            this.MaximizeBox = false;
            this.Name = "frmSearch";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Search";
            this.Load += new System.EventHandler(this.frmSearch_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbQueryPicture)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoadPicture;
        private System.Windows.Forms.OpenFileDialog ofdLoadPicture;
        private System.Windows.Forms.PictureBox pbQueryPicture;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.RadioButton rbIntensity;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbColor;
        private System.Windows.Forms.Label lblProcess;
    }
}

