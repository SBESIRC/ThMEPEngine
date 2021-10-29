using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using AcHelper.Commands;
//using ThAnalytics;

namespace ThMEPStructure.Command
{
    public abstract class ThMEPBaseCommand : IAcadCommand
    {
        protected Stopwatch _stopwatch = new Stopwatch();

        public string CommandName { get; set; }
        public string ActionName { get; set; } = "Execute";
        public object ThCybrosService { get; private set; }

        public void Execute()
        {
            try
            {
                BeforeExecute();
                SubExecute();
                AfterExecute();
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message + "\n");
            }
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
            //ThCybrosService.Instance.RecordTHCommandEvent(CommandName, ActionName, _stopwatch.Elapsed.TotalSeconds);
        }
    }
}
