﻿using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThSecurityPlaneCmds
    {
        [CommandMethod("TIANHUACAD", "THVMSYSTEM", CommandFlags.Modal)]
        public void ThVideoMSystem()
        {
            using (var cmd = new ThVideoMonitoringSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THIASYSTEM", CommandFlags.Modal)]
        public void ThIntrusionAlarmSystem()
        {
            using (var cmd = new ThIntrusionAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THGTSYSTEM", CommandFlags.Modal)]
        public void ThGuardToourSystem()
        {
            using (var cmd = new ThGuardToourSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THACSYSTEM", CommandFlags.Modal)]
        public void ThAccessControlSystem()
        {
            using (var cmd = new ThAccessControlSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THSPPIPE", CommandFlags.Modal)]
        public void ThSPConnectPipe()
        {
            using (var cmd = new ThSecurityPlaneSystemPipeCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THTXTINFO", CommandFlags.Modal)]
        public void ThGetStruInfoToTxt()
        {
            using (var cmd = new TxtStrucInfoCommand())
            {
                cmd.Execute();
            }
        }
    }
}
