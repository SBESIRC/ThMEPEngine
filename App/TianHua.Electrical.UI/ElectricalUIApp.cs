using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using ThMEPElectrical;
using ThMEPElectrical.Model;
using ThMEPElectrical.Command;
using ThMEPElectrical.BlockConvert;
using TianHua.Electrical.UI.SecurityPlaneUI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

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
            // TODO：跳出模态对话框，获取转换参数
            // 暂时用命令行获取转换参数
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 专业
                var options = new PromptKeywordOptions("\n选择专业");
                options.Keywords.Add("暖通", "H", "暖通(H)");
                options.Keywords.Add("给排水", "W", "给排水(W)");
                options.Keywords.Add("所有", "A", "所有(A)");
                options.Keywords.Default = "所有";
                var category = Active.Editor.GetKeywords(options);
                if (category.Status != PromptStatus.OK)
                {
                    return;
                }

                // 模式
                options = new PromptKeywordOptions("\n选择模式");
                options.Keywords.Add("强电", "S", "强电(S)");
                options.Keywords.Add("弱电", "W", "弱电(W)");
                options.Keywords.Add("所有", "A", "所有(A)");
                options.Keywords.Default = "所有";
                var mode = Active.Editor.GetKeywords(options);
                if (mode.Status != PromptStatus.OK)
                {
                    return;
                }

                // 图纸比例
                var scale = Active.Editor.GetDouble(new PromptDoubleOptions("\n图纸比例")
                {
                    DefaultValue = 100.0,
                });
                if (scale.Status != PromptStatus.OK)
                {
                    return;
                }

                // 执行命令
                var cmd = new ThBConvertCommand()
                {
                    Scale = scale.Value,
                };
                if (category.StringResult == "暖通")
                {
                    cmd.Category = ConvertCategory.HVAC;
                }
                else if (category.StringResult == "给排水")
                {
                    cmd.Category = ConvertCategory.WSS;
                }
                else if (category.StringResult == "所有")
                {
                    cmd.Category = ConvertCategory.ALL;
                }
                if (mode.StringResult == "强电")
                {
                    cmd.Mode = ConvertMode.STRONGCURRENT;
                }
                else if (mode.StringResult == "弱电")
                {
                    cmd.Mode = ConvertMode.WEAKCURRENT;
                }
                else if (mode.StringResult == "所有")
                {
                    cmd.Mode = ConvertMode.ALL;
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
