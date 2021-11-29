using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.UI.UI;
using ThMEPLighting.UI.WiringConnecting;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPLighting.UI
{
    public class MEPLightingUIApp : IExtensionApplication
    {
        uiEvaIndicatorSign uiSign = null;
        ThWiringConnectingUI uiConnect = null;
        public void Initialize()
        {
            uiSign = null;
            uiConnect = null;
        }

        public void Terminate()
        {
            uiSign = null;
            uiConnect = null ;
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
        [CommandMethod("TIANHUACAD", "THCWZM", CommandFlags.Modal)]
        public void THParkLightUI()
        {
            uiParkingLight uiPLight = new uiParkingLight();
            AcadApp.ShowModelessWindow(uiPLight);
        }

        //照明
        [CommandMethod("TIANHUACAD", "THZM", CommandFlags.Modal)]
        public void THZMUI()
        {
            var ui = new TianHua.Lighting.UI.uiThLighting();
            AcadApp.ShowModelessWindow(ui);
        }


        /// <summary>
        /// 天华连线
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLX", CommandFlags.Modal)]
        public void THLX()
        {
            if (uiConnect == null)
            {
                uiConnect = new ThWiringConnectingUI();
            }
            AcadApp.ShowModelessWindow(uiConnect);
        }
    }
}
