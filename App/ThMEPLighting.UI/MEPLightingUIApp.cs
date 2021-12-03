using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.UI.UI;
using ThMEPLighting.UI.emgLightLayout;
using ThMEPLighting.UI.WiringConnecting;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPLighting.UI
{
    public class MEPLightingUIApp : IExtensionApplication
    {
        uiEvaIndicatorSign uiSign = null;
        ThWiringConnectingUI uiConnect = null;
        UIEmgLightLayout uiEmgLightLayout = null;
        UIEmgLightConnect uiEmgLightConnect = null;

        public void Initialize()
        {
            uiSign = null;
            uiConnect = null;
            uiEmgLightLayout = null;
            uiEmgLightConnect = null;
        }

        public void Terminate()
        {
            uiSign = null;
            uiConnect = null;
            uiEmgLightLayout = null;
            uiEmgLightConnect = null;
        }

        [CommandMethod("TIANHUACAD", "THSSZSD", CommandFlags.Modal)]
        public void THSSUI()
        {
            if (null != uiSign && uiSign.IsLoaded)
                return;

            uiSign = new uiEvaIndicatorSign();
            AcadApp.ShowModelessWindow(uiSign);
        }

        [CommandMethod("TIANHUACAD", "THCWZM", CommandFlags.Modal)]
        public void THParkLightUI()
        {
            uiParkingLight uiPLight = new uiParkingLight();
            AcadApp.ShowModelessWindow(uiPLight);
        }

        [CommandMethod("TIANHUACAD", "THYJZM", CommandFlags.Modal)]
        public void THYJZMUI()
        {
            if (uiEmgLightLayout != null && uiEmgLightLayout.IsLoaded)
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
