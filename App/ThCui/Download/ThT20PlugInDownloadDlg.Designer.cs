namespace TianHua.AutoCAD.ThCui
{
    partial class ThT20PlugInDownloadDlg
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
            this.label_header = new System.Windows.Forms.Label();
            this.label_download_progress = new System.Windows.Forms.Label();
            this.progressBar_download = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // label_header
            // 
            this.label_header.AutoSize = true;
            this.label_header.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_header.Location = new System.Drawing.Point(90, 9);
            this.label_header.Name = "label_header";
            this.label_header.Size = new System.Drawing.Size(127, 14);
            this.label_header.TabIndex = 0;
            this.label_header.Text = "Downloading APP";
            // 
            // label_download_progress
            // 
            this.label_download_progress.AutoSize = true;
            this.label_download_progress.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_download_progress.Location = new System.Drawing.Point(98, 30);
            this.label_download_progress.Name = "label_download_progress";
            this.label_download_progress.Size = new System.Drawing.Size(119, 14);
            this.label_download_progress.TabIndex = 1;
            this.label_download_progress.Text = "(0 MB / 10 MB)";
            // 
            // progressBar_download
            // 
            this.progressBar_download.Location = new System.Drawing.Point(13, 57);
            this.progressBar_download.Name = "progressBar_download";
            this.progressBar_download.Size = new System.Drawing.Size(304, 23);
            this.progressBar_download.TabIndex = 2;
            // 
            // ThT20PlugInDownloadDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(329, 90);
            this.Controls.Add(this.progressBar_download);
            this.Controls.Add(this.label_download_progress);
            this.Controls.Add(this.label_header);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ThT20PlugInDownloadDlg";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "T20天正插件下载";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_header;
        private System.Windows.Forms.Label label_download_progress;
        private System.Windows.Forms.ProgressBar progressBar_download;
    }
}