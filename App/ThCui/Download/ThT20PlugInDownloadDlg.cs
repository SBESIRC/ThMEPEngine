using System.Net;
using System.Windows.Forms;
using ThCADExtension;

namespace TianHua.AutoCAD.ThCui
{
    public partial class ThT20PlugInDownloadDlg : Form, IDownloadProgress
    {
        public ThT20PlugInDownloadDlg(string appName, string appVersion)
        {
            InitializeComponent();

            // 初始化界面
            label_header.Text = string.Format("正在下载 {0} {1}", appName, appVersion);
            label_download_progress.Text = "";
            progressBar_download.Maximum = 100;
            progressBar_download.Minimum = 0;
            progressBar_download.Step = 1;
        }

        public void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar_download.Value = e.ProgressPercentage;
            label_download_progress.Text = " (" + ThStringTools.NumBytesToUserReadableString(e.BytesReceived) + " / " +
                ThStringTools.NumBytesToUserReadableString(e.TotalBytesToReceive) + ")";
        }
    }
}
