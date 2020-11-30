using ThMEPElectrical;
using AcHelper.Commands;
using System.Windows.Forms;
using ThMEPElectrical.Model;
using ThMEPElectrical.BlockConvert;
using Autodesk.AutoCAD.Runtime;
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
                SmokeLayoutDataModel _SmokeLayoutDataModel = new SmokeLayoutDataModel()
                {
                    LayoutType = LayoutType,
                    AreaLayout = AreaLayout,
                    RoofThickness = Parameter.RoofThickness,
                };
                SmokeLayoutUI.InitForm(_SmokeLayoutDataModel);
            }
            AcadApp.ShowModelessDialog(SmokeLayoutUI);
        }

        [CommandMethod("TIANHUACAD", "THTZL", CommandFlags.Modal)]
        public void ThBlockConvert()
        {
            using (var dlg = new fmBlockConvert())
            {
                var result = AcadApp.ShowModalDialog(dlg);
                if (result == DialogResult.OK)
                {
                    switch (dlg.ActiveConvertMode)
                    {
                        case ConvertMode.STRONGCURRENT:
                            CommandHandlerBase.ExecuteFromCommandLine(false, "THPBE");
                            break;
                        case ConvertMode.WEAKCURRENT:
                            CommandHandlerBase.ExecuteFromCommandLine(false, "THLBE");
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private string LayoutType
        {
            get
            {
                return Parameter.sensorType == SensorType.SMOKESENSOR ? 
                    ElectricalUICommon.SMOKE_INDUCTION : ElectricalUICommon.TEMPERATURE_INDUCTION;
            }
        }

        private string AreaLayout
        {
            get
            {
                return ElectricalUICommon.AREA_COMMON;
            }
        }

        private PlaceParameter Parameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.Parameter == null)
                {
                    ThMEPElectricalService.Instance.Parameter = new PlaceParameter();
                }
                return ThMEPElectricalService.Instance.Parameter;
            }
        }

        private ThBConvertParameter ConvertParameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.ConvertParameter == null)
                {
                    ThMEPElectricalService.Instance.ConvertParameter = new ThBConvertParameter();
                }
                return ThMEPElectricalService.Instance.ConvertParameter;
            }
        }
    }
}
