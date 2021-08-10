using AcHelper;
using Linq2Acad;
using ThMEPElectrical;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.BlockConvert;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Electrical.UI.SecurityPlaneUI;
using TianHua.Electrical.UI.CapitalConverter;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.UI
{
    public class ElectricalUIApp : IExtensionApplication
    {
        private fmSmokeLayout SmokeLayoutUI { get; set; }
        private fmBasementLighting BasementLightingUI { get; set; }

        public void Initialize()
        {
            SmokeLayoutUI = null;
            BasementLightingUI = null;
        }

        public void Terminate()
        {
            SmokeLayoutUI = null;
            BasementLightingUI = null;
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

        [CommandMethod("TIANHUACAD", "THTZZH", CommandFlags.Modal)]
        public void THTZZH()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var uiCapitalConverter = new CapitalConverterUI();
                uiCapitalConverter.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                AcadApp.ShowModalWindow(uiCapitalConverter);
                if (!uiCapitalConverter.GoOn)
                {
                    return;
                }
                // 执行命令
                var cmd = new ThBConvertCommand()
                {
                    Scale = uiCapitalConverter.Parameter.BlkScaleValue,
                };
                if (uiCapitalConverter.Parameter.HavcOps &&
                    uiCapitalConverter.Parameter.WssOps)
                {
                    cmd.Category = ConvertCategory.ALL;
                }
                else if (uiCapitalConverter.Parameter.HavcOps)
                {
                    cmd.Category = ConvertCategory.HVAC;
                }
                else if (uiCapitalConverter.Parameter.WssOps)
                {
                    cmd.Category = ConvertCategory.WSS;
                }
                else
                {
                    return;
                }
                switch (uiCapitalConverter.Parameter.EquipOps)
                {
                    case CapitalOP.Strong:
                        cmd.Mode = ConvertMode.STRONGCURRENT;
                        break;
                    case CapitalOP.Weak:
                        cmd.Mode = ConvertMode.WEAKCURRENT;
                        break;
                    case CapitalOP.All:
                        cmd.Mode = ConvertMode.ALL;
                        break;
                }
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THCDZM", CommandFlags.Modal)]
        public void THCDZM()
        {
            if (BasementLightingUI == null)
            {
                BasementLightingUI = new fmBasementLighting();
            }
            AcadApp.ShowModelessDialog(BasementLightingUI);
        }

        [CommandMethod("TIANHUACAD", "THAFPM", CommandFlags.Modal)]
        public void THAFPM()
        {
            SecurityPlaneSystemUI securityPlaneSystemUI = new SecurityPlaneSystemUI();
            AcadApp.ShowModalWindow(securityPlaneSystemUI);
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
    }
}
