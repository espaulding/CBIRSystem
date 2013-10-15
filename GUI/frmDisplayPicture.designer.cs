namespace CBIR
{
    partial class frmDisplayPicture
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
            this.pbDisplay = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblImage = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblDist = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // pbDisplay
            // 
            this.pbDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbDisplay.Location = new System.Drawing.Point(0, 38);
            this.pbDisplay.Name = "pbDisplay";
            this.pbDisplay.Size = new System.Drawing.Size(150, 150);
            this.pbDisplay.TabIndex = 0;
            this.pbDisplay.TabStop = false;
            this.pbDisplay.Paint += new System.Windows.Forms.PaintEventHandler(this.pbDisplay_Paint);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(47, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Image:";
            // 
            // lblImage
            // 
            this.lblImage.AutoSize = true;
            this.lblImage.Location = new System.Drawing.Point(92, 6);
            this.lblImage.Name = "lblImage";
            this.lblImage.Size = new System.Drawing.Size(49, 13);
            this.lblImage.TabIndex = 2;
            this.lblImage.Text = "Filename";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Distance:";
            // 
            // lblDist
            // 
            this.lblDist.AutoSize = true;
            this.lblDist.Location = new System.Drawing.Point(92, 22);
            this.lblDist.Name = "lblDist";
            this.lblDist.Size = new System.Drawing.Size(49, 13);
            this.lblDist.TabIndex = 4;
            this.lblDist.Text = "Distance";
            // 
            // frmDisplayPicture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(150, 189);
            this.Controls.Add(this.lblDist);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblImage);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pbDisplay);
            this.Name = "frmDisplayPicture";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DisplayPicture";
            this.ResizeEnd += new System.EventHandler(this.frmDisplayPicture_ResizeEnd);
            ((System.ComponentModel.ISupportInitialize)(this.pbDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbDisplay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblImage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblDist;
    }
}