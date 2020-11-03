using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI
{
    public class ElectricalUIApp : IExtensionApplication
    {
        private fmSmokeLayout SmokeLayoutUI { get; set; }


        public void Initialize()
        {
            SmokeLayoutUI = null;
        }

        public void Terminate()
        {
            SmokeLayoutUI = null;
        }

        [CommandMethod("TIANHUACAD", "THYWG", CommandFlags.Modal)]
        public void THYWG()
        {
            if (SmokeLayoutUI == null)
            {
                SmokeLayoutUI = new fmSmokeLayout();
                SmokeLayoutDataModel _SmokeLayoutDataModel = new SmokeLayoutDataModel();
                if (ThMEPElectricalService.Instance.Parameter == null) { ThMEPElectricalService.Instance.Parameter = new ThMEPElectrical.Model.PlaceParameter(); }
                _SmokeLayoutDataModel.LayoutType = ThMEPElectricalService.Instance.Parameter.sensorType == ThMEPElectrical.Model.SensorType.SMOKESENSOR ? "烟感" : "温感";
                _SmokeLayoutDataModel.RoofThickness = ThMEPElectricalService.Instance.Parameter.RoofThickness;
                _SmokeLayoutDataModel.AreaLayout = "车库、走除道外房间";
                SmokeLayoutUI.InitForm(_SmokeLayoutDataModel);
            }
            AcadApp.ShowModelessDialog(SmokeLayoutUI);
        }
    }
}
