namespace TianHua.AutoCAD.ThCui
{
    partial class ThFeedBackDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThFeedBackDlg));
            this.LinkLab = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // LinkLab
            // 
            this.LinkLab.Cursor = System.Windows.Forms.Cursors.Hand;
            this.LinkLab.Location = new System.Drawing.Point(241, 125);
            this.LinkLab.Name = "LinkLab";
            this.LinkLab.Size = new System.Drawing.Size(180, 19);
            this.LinkLab.TabIndex = 0;
            this.LinkLab.TabStop = true;
            this.LinkLab.Text = "airdcenter@thape.com.cn";
            this.LinkLab.Click += new System.EventHandler(this.LinkLab_Click);
            // 
            // ThFeedBackDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(473, 298);
            this.Controls.Add(this.LinkLab);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(489, 337);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(489, 337);
            this.Name = "ThFeedBackDlg";
            this.Text = "反馈建议";
            this.Load += new System.EventHandler(this.fmFeedBack_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.LinkLabel LinkLab;
    }
}