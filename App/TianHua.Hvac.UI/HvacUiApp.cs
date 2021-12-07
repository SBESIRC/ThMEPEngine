﻿using Autodesk.AutoCAD.Runtime;
using TianHua.Hvac.UI.Command;

namespace TianHua.Hvac.UI
{
    public class HvacUiApp : IExtensionApplication
    {
        public void Initialize()
        {

        }

        public void Terminate()
        {

        }

        [CommandMethod("TIANHUACAD", "THDKFPM", CommandFlags.Modal)]
        public void THDKFPM()
        {
            using (var cmd = new ThHvacDuctPortsCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THDKFPMFG", CommandFlags.Modal)]
        public void THDKFPMFG()
        {
            using (var cmd = new ThHvacDuctModifyCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFPMS", CommandFlags.Modal)]
        public void THFPMSuper()
        {
            using (var cmd = new ThHvacFpmCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THXFJ", CommandFlags.Modal)]
        public void THXFJ()
        {
            using (var cmd = new ThHvacXfjCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THSPM", CommandFlags.Modal)]
        public void THSPM()
        {
            using (var cmd = new ThHvacSpmCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFHJS", CommandFlags.Modal)]
        public void THFHJS()
        {
            using (var cmd = new ThHvacLoadCalculationCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THSWSZ", CommandFlags.Modal)]
        public void THSWSZ()
        {
            using (var cmd = new ThHvacOutdoorVentilationCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFJBH", CommandFlags.Modal)]
        public void THFJBH()
        {
            using (var cmd = new ThHvacRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFJGN", CommandFlags.Modal)]
        public void THFJGN()
        {
            using (var cmd = new ThHvacExtractRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }
    }
}
