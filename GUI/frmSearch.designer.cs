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
            this.lblProcess = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbQueryPicture)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnLoadPicture
            // 
            this.btnLoadPicture.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnLoadPicture.Location = new System.Drawing.Point(3, 273);
            this.btnLoadPicture.Name = "btnLoadPicture";
            this.btnLoadPicture.Size = new System.Drawing.Size(288, 31);
            this.btnLoadPicture.TabIndex = 0;
            this.btnLoadPicture.Text = "Find Query Picture";
            this.btnLoadPicture.UseVisualStyleBackColor = true;
            this.btnLoadPicture.Click += new System.EventHandler(this.btnLoadPicture_Click);
            // 
            // pbQueryPicture
            // 
            this.pbQueryPicture.Location = new System.Drawing.Point(6, 16);
            this.pbQueryPicture.Name = "pbQueryPicture";
            this.pbQueryPicture.Size = new System.Drawing.Size(276, 242);
            this.pbQueryPicture.TabIndex = 2;
            this.pbQueryPicture.TabStop = false;
            // 
            // btnSearch
            // 
            this.btnSearch.Enabled = false;
            this.btnSearch.Location = new System.Drawing.Point(3, 310);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(288, 30);
            this.btnSearch.TabIndex = 6;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // lblProcess
            // 
            this.lblProcess.AutoSize = true;
            this.lblProcess.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProcess.Location = new System.Drawing.Point(58, 317);
            this.lblProcess.Name = "lblProcess";
            this.lblProcess.Size = new System.Drawing.Size(175, 16);
            this.lblProcess.TabIndex = 9;
            this.lblProcess.Text = "Processing your search.";
            this.lblProcess.Visible = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pbQueryPicture);
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(288, 264);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Query Image";
            // 
            // frmSearch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(294, 342);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.lblProcess);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.btnLoadPicture);
            this.MaximizeBox = false;
            this.Name = "frmSearch";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Search";
            this.Load += new System.EventHandler(this.frmSearch_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbQueryPicture)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoadPicture;
        private System.Windows.Forms.OpenFileDialog ofdLoadPicture;
        private System.Windows.Forms.PictureBox pbQueryPicture;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Label lblProcess;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

