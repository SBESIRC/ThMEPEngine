using Autodesk.AutoCAD.Runtime;
using TianHua.Structure.WPF.UI.Command;

namespace TianHua.Structure.WPF.UI
{
    public class StructureWPFUIApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THCLSCUI", CommandFlags.Session)]
        public void THCLUI()
        {
            using (var cmd = new SecondaryBeamCmd())
            {
                cmd.Execute();
            }
        }
    }
}
