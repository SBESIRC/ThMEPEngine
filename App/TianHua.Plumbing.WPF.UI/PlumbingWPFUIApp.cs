using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Command;
using TianHua.Plumbing.WPF.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public class PlumbingWPFUIApp : IExtensionApplication
    {
        uiDrainageSystem uiDrainage;
        uiDrainageSystemSet uiSet;
        public void Initialize()
        {
            return;
        }

        public void Terminate()
        {
            return;
        }
        [CommandMethod("TIANHUACAD", "THDSPSXT", CommandFlags.Modal)]
        public void THSSUI()
        {
            if (null != uiDrainage && uiDrainage.IsLoaded)
                return;

            uiDrainage = new uiDrainageSystem();
            AcadApp.ShowModelessWindow(uiDrainage);
        }
        [CommandMethod("TIANHUACAD", "THDSPSXTSet", CommandFlags.Modal)]
        public void THSSUI2()
        {
            if (null != uiSet && uiSet.IsLoaded)
                return;

            uiSet = new uiDrainageSystemSet("住户分组1");
            AcadApp.ShowModelessWindow(uiSet);
        }

        /// <summary>
        /// Tian Hua Create water suply system diagram
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCSSDBYUI", CommandFlags.Modal)]
        public void ThCreateWaterSuplySystemDiagramWithUI()
        {
            if (null != uiDrainage && uiDrainage.IsLoaded)
                return;

            uiDrainage = new uiDrainageSystem();
            AcadApp.ShowModelessWindow(uiDrainage);
        }

        /// <summary>
        /// Tian Hua arrange pumps in water well
        /// </summary>
        [CommandMethod("TIANHUACAD", "THAPBYUI", CommandFlags.Modal)]
        public void ThArrangePumpWithUI()
        {
            var ui = new uiWaterWellPump();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// Tian Hua create rain system diagram
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCRSDBYUI", CommandFlags.Modal)]
        public void ThCreateRainSystemDiagram()
        {
            if (ThMEPWSS.Pipe.Service.ThRainSystemService.commandContext != null) return;
            var ui = new uiRainSystem();
            AcadApp.ShowModelessWindow(ui);
        }
    }
}
