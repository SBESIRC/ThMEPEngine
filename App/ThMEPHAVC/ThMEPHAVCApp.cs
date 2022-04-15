using Autodesk.AutoCAD.Runtime;
using ThMEPHVAC.Command;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanLayout.Command;

namespace ThMEPHVAC
{
    public class ThMEPHAVCApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THFJJC", CommandFlags.Modal)]
        public void THFOUNDATIONEXTRACT()
        {
            using (var cmd = new ThModelBaseExtractCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFGLD", CommandFlags.Modal)]
        public void THFGLDEXTRACT()
        {
            using (var cmd = new ThFanHoleExtractCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFGSGLG", CommandFlags.Modal)]
        public void THFGSGLG()
        {
            using (var cmd = new ThWaterPipeConnectExtractCmd_Temp())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFJXXCK", CommandFlags.NoUndoMarker|CommandFlags.NoHistory)]
        public void THFanSelectInsertBlock()
        {
            using (var cmd = new ThHvacFanTypeSelectCmd())
            {
                cmd.Execute();
            }
        }
    }
}
