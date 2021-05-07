using AcHelper.Commands;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace ThMEPLighting.UI
{
    public class MEPLightingUIApp : IExtensionApplication
    {
        uiEvaIndicatorSign uiSign = null;
        ExampleUI exampleUI = null;
        public void Initialize()
        {
            uiSign = null;
            exampleUI = null;
        }

        public void Terminate()
        {
            uiSign = null;
            exampleUI = null;
        }
        [CommandMethod("TIANHUACAD", "THSSZSD", CommandFlags.Modal)]
        public void THSSUI()
        {
            if (null != uiSign && uiSign.IsLoaded) 
                return;
                
            uiSign = new uiEvaIndicatorSign();
            AcadApp.ShowModelessWindow(uiSign);
            //var isOk = AcadApp.ShowModalWindow(uiSign);
            //if (isOk == true) 
            //{
            //    if (uiSign.commondType == 0)
            //    {
            //        CommandHandlerBase.ExecuteFromCommandLine(false, "THFEI");
            //    }
            //    else if (uiSign.commondType == 1)
            //    {
            //        CommandHandlerBase.ExecuteFromCommandLine(false, "THSSZSDBZ");
            //    }
            //}
        }

        [CommandMethod("TIANHUACAD", "THEXUI", CommandFlags.Modal)]
        public void THExampleUI()
        {
            //exampleUI = new ExampleUI();
            //AcadApp.ShowModelessWindow(exampleUI);
        }
    }
}
