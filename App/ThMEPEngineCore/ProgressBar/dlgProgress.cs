using System;
using System.Windows.Forms;

namespace ShowProgressBar
{
    public partial class dlgProgress : Form
    {
        private ILongProcessingObject _executingObject = null;
        private bool _stop = false;
        private bool _isMarquee = false;
        private int _loopCount = 0;

        public dlgProgress()
        {
            InitializeComponent();
        }

        public dlgProgress(
            ILongProcessingObject executingObj)
            : this()
        {
            _executingObject = executingObj;

            _executingObject.ProcessingStarted +=
                new LongProcessStarted(ExecutingObject_ProcessStarted);
            _executingObject.ProcessingProgressed +=
                new LongProcessingProgressed(ExecutingObject_Progressed);
            _executingObject.ProcessingEnded +=
                new EventHandler(ExecutingObject_ProcessEnded);
            _executingObject.CloseProgressUIRequested +=
               new EventHandler(ExecutingObject_CloseProgressUIRequested);
        }

        private void ExecutingObject_CloseProgressUIRequested(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void ExecutingObject_ProcessEnded(object sender, EventArgs e)
        {
            pBar.Value = 0;
            lblTitle.Text = "";
            lblDescription.Text = "";
            this.Refresh();
        }

        private void ExecutingObject_ProcessStarted(
            object sender, LongProcessStartedEventArgs e)
        {

            if (e.LoopCount == 0)
            {
                pBar.Style = ProgressBarStyle.Marquee;
                lblDescription.Text = "Please wait...";
            }
            else
            {
                pBar.Style = ProgressBarStyle.Continuous;
                pBar.Minimum = 0;
                pBar.Maximum = e.LoopCount;
                pBar.Value = 0;
                lblDescription.Text = "";
                _loopCount = e.LoopCount;
            }

            _isMarquee = e.LoopCount == 0;
            btnStop.Visible = e.CanStop;
            lblTitle.Text = e.Description;
            Application.DoEvents();
            this.Refresh();
        }

        private void ExecutingObject_Progressed(
            object sender, LongProcessingProgressEventArgs e)
        {
            if (!_isMarquee)
            {
                pBar.Value++;
            }
 
            lblDescription.Text = e.ProgressDescription;
            lblDescription.Refresh();

            Application.DoEvents();
            if (_stop)
            {
                e.Cancel = true;
            }
        }

        private void dlgProgress_Shown(object sender, EventArgs e)
        {
            Application.DoEvents();

            if (_executingObject == null)
            {
                this.DialogResult = DialogResult.Cancel;
            }
            else
            {
                _executingObject.DoLongProcessingWork();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _stop = true;
        }
    }
}
