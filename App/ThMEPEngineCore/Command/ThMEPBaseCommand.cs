using AcHelper.Commands;
using System.Diagnostics;
using ThAnalytics;

namespace ThMEPEngineCore.Command
{
    public abstract class ThMEPBaseCommand : IAcadCommand
    {
        protected Stopwatch _stopwatch = new Stopwatch();

        public string CommandName { get; set; } = "";
        public string ActionName { get; set; } = "";

        public void Execute()
        {
            BeforeExecute();
            SubExecute();
            AfterExecute();
        }

        abstract public void SubExecute();

        virtual public void BeforeExecute()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        virtual public void AfterExecute()
        {
            _stopwatch.Stop();
            ThCybrosService.Instance.RecordTHCommandEvent(CommandName, ActionName, _stopwatch.Elapsed.TotalSeconds);
        }
    }
}
