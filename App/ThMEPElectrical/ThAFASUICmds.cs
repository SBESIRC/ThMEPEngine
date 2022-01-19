using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmFixLayout.Command;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Model;

#if (ACAD2016 || ACAD2018)
using CLI;
using ThMEPElectrical.AFAS.Command;
using ThMEPElectrical.FireAlarmDistance.Command;
#endif

namespace ThMEPElectrical
{
    public class ThAFASUICmds
    {
        [CommandMethod("TIANHUACAD", "THFASmoke", CommandFlags.Session)]
        public void THFASmoke()
        {
            using (var cmd = new ThAFASSmokeCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFADisplay", CommandFlags.Session)]
        public void THFADisplay()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitor", CommandFlags.Session)]
        public void THFAMonitor()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATel", CommandFlags.Session)]
        public void THFATel()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGas", CommandFlags.Session)]
        public void THFAGas()
        {
            using (var cmd = new ThAFASGasCmd())
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFABroadcast", CommandFlags.Session)]
        public void THFABroadcast()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd())
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarm", CommandFlags.Session)]
        public void THFAManualAlarm()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd())
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }
    }
}
