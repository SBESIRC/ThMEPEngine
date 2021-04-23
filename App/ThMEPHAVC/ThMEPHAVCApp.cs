using Autodesk.AutoCAD.Runtime;
using ThMEPHVAC.Command;

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
    }
}
