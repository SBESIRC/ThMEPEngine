using Autodesk.AutoCAD.Runtime;
using TianHua.Architecture.WPI.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
namespace TianHua.Architecture.WPI.UI
{
    public class ArchitectureWPFUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        /// <summary>
        /// 地下车库车位排布, 天华地下车位(THZDCWBZ)
        /// </summary>
        [CommandMethod("TIANHUACAD", "THZDCWBZ", "CWBZ_CmdId", CommandFlags.Modal)]
        public void ThCreateParkingStallsWithUI()
        {
            var w = new UiParkingStallArrangement();
            AcadApp.ShowModelessWindow(w);
        }
    }
}
