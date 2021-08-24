using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.UI.emgLightLayout;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPLighting.UI
{
    public class MEPLightingUIEmgLightApp: IExtensionApplication
    {
        UIEmgLightLayout uiEmgLightLayout = null;
        UIEmgLightConnect uiEmgLightConnect = null;

        public void Initialize()
        {
            uiEmgLightLayout = null;
            uiEmgLightConnect = null;
        }
        public void Terminate()
        {
            uiEmgLightLayout = null;
            uiEmgLightConnect = null;
        }

        [CommandMethod("TIANHUACAD", "THYJZM", CommandFlags.Modal)]
        public void THYJZMUI()
        {
            if (uiEmgLightLayout != null  && uiEmgLightLayout.IsLoaded)
                return;

            uiEmgLightLayout = new UIEmgLightLayout();
            AcadApp.ShowModelessWindow(uiEmgLightLayout);
        
        }

        [CommandMethod("TIANHUACAD", "THYJZMLXUI", CommandFlags.Modal)]
        public void THYJZMLXUI()
        {
            if (uiEmgLightConnect != null && uiEmgLightConnect.IsLoaded)
                return;

            uiEmgLightConnect = new UIEmgLightConnect();
            AcadApp.ShowModelessWindow(uiEmgLightConnect);

        }


    }
}
