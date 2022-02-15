using Autodesk.AutoCAD.Runtime;
using TianHua.Electrical.PDS.Command;

namespace TianHua.Electrical.PDS.UI
{
    public class ThElectricalPDSUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THPDSTest", CommandFlags.Modal)]
        public void THPDSTest()
        {
            var cmd = new ThPDSCommand();
            cmd.Execute();
        }
    }
}
