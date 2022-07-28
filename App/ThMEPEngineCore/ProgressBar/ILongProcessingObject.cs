using System;

namespace ShowProgressBar
{
    public interface ILongProcessingObject
    {
        event LongProcessStarted ProcessingStarted;
        event LongProcessingProgressed ProcessingProgressed;
        event EventHandler ProcessingEnded;
        event EventHandler CloseProgressUIRequested;
        void DoLongProcessingWork();
    }

    public class LongProcessStartedEventArgs : EventArgs
    {
        private int _loopCount;
        private string _description;
        private bool _canStop;

        public LongProcessStartedEventArgs(
            string description, int loopCount = 0, bool canStop = false)
        {
            _loopCount = loopCount;
            _description = description;
            _canStop = canStop;
        }

        public int LoopCount
        {
            get { return _loopCount; }
        }

        public string Description
        {
            get { return _description; }
        }

        public bool CanStop
        {
            get { return _canStop; }
        }
    }

    public class LongProcessingProgressEventArgs : EventArgs
    {
        private string _progressDescription;
        private bool _cancel = false;

        public LongProcessingProgressEventArgs(string progressDescription)
        {
            _progressDescription = progressDescription;
        }

        public string ProgressDescription
        {
            get { return _progressDescription; }
        }

        public bool Cancel
        {
            set { _cancel = value; }
            get { return _cancel; }
        }
    }

    public delegate void LongProcessStarted(
        object sender, LongProcessStartedEventArgs e);

    public delegate void LongProcessingProgressed(
        object sender, LongProcessingProgressEventArgs e);
}
