using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPElectrical.AFAS.Utils
{
    public class LogUtil
    {
        public string Path { get; private set; } = "";
        public TimeSpan FlushToDiskInterval { get; private set; }
        public Serilog.Core.Logger Log { get; private set; }

        public bool DebugSwitch { get; private set; } = false;

        public LogUtil(string LogFileName, TimeSpan? flashToDistInterval = null)
        {
            DebugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);

            if (DebugSwitch == true)
            {
                Path = LogFileName;
                if (flashToDistInterval == null)
                {
                    FlushToDiskInterval = new TimeSpan(0, 0, 5);
                }
                else
                {
                    FlushToDiskInterval = (TimeSpan)flashToDistInterval;
                }

                CreateLog();
            }
        }

        private void CreateLog()
        {
            if (Path != "")
            {
                Log = new Serilog.LoggerConfiguration().WriteTo.File(Path, flushToDiskInterval: FlushToDiskInterval, rollingInterval: RollingInterval.Hour).CreateLogger();
            }
        }

        public void WriteErrLog(string msg)
        {
            if (Log != null)
            {
                Log.Error(msg);
            }
        }
    }
}
