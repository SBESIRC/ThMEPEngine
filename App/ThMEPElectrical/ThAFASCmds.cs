using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmFixLayout.Command;

#if (ACAD2016 || ACAD2018)
using ThMEPElectrical.FireAlarmDistance;
#endif

namespace ThMEPElectrical
{
    public class ThAFASCmds
    {
        [CommandMethod("TIANHUACAD", "THFASmokeNoUI", CommandFlags.Modal)]
        public void THFASmokeNoUI()
        {
            using (var cmd = new ThAFASSmokeCmd(false))
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFASmoke", CommandFlags.Modal)]
        public void THFASmoke()
        {
            using (var cmd = new ThAFASSmokeCmd(true))
            {
                cmd.Execute();
            }
        }


        [CommandMethod("TIANHUACAD", "THFADisplayNoUI", CommandFlags.Modal)]
        public void THFADisplayNoUI()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFADisplay", CommandFlags.Modal)]
        public void THFADisplay()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd(true))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitorNoUI", CommandFlags.Modal)]
        public void THFAMonitorNoUI()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitor", CommandFlags.Modal)]
        public void THFAMonitor()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd(true))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATelNoUI", CommandFlags.Modal)]
        public void THFATelNoUI()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATel", CommandFlags.Modal)]
        public void THFATel()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd(true))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGasNoUI", CommandFlags.Modal)]
        public void THFAGasNoUI()
        {
            using (var cmd = new ThAFASGasCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGas", CommandFlags.Modal)]
        public void THFAGas()
        {
            using (var cmd = new ThAFASGasCmd(true))
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFABroadcastNoUI", CommandFlags.Modal)]
        public void THFABroadcastNoUI()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd(false))
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFABroadcast", CommandFlags.Modal)]
        public void THFABroadcast()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd(true))
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarmNoUI", CommandFlags.Modal)]
        public void THFAManualAlarmNoUI()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd(false))
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarm", CommandFlags.Modal)]
        public void THFAManualAlarm()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd(true))
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }
    }
}
