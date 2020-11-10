using System.Net;
using System;
using System.ComponentModel;

namespace TianHua.AutoCAD.ThCui
{
    /// <summary>
    /// Interface for UI element that shows the progress bar
    /// and a method to install and relaunch the appliction
    /// </summary>
    public interface IDownloadProgress
    {
        /// <summary>
        /// Called when the download progress changes
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">used to resolve the progress of the download. Also contains the total size of the download</param>
        void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e);
    }
}
