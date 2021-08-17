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
        UIEmgLightLayout uiEmgLight = null;

        public void Initialize()
        {
            uiEmgLight = null;
        }
        public void Terminate()
        {
            uiEmgLight = null;
        }

        [CommandMethod("TIANHUACAD", "THYJZM", CommandFlags.Modal)]
        public void THYJZMUI()
        {
            if (uiEmgLight != null  && uiEmgLight.IsLoaded)
                return;

            uiEmgLight = new UIEmgLightLayout();
            AcadApp.ShowModelessWindow(uiEmgLight);
        
        }

    }
}
