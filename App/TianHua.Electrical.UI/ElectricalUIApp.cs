using System.Windows.Forms;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical;
using ThMEPElectrical.Model;
using ThMEPElectrical.Command;
using ThMEPElectrical.BlockConvert;
using TianHua.Electrical.UI.FireAlarm;
using TianHua.Electrical.UI.ThBroadcast;
using TianHua.Electrical.UI.SecurityPlaneUI;
using TianHua.Electrical.UI.CapitalConverter;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPEngineCore.Algorithm.FrameComparer;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using TianHua.Electrical.UI.Command;
using TianHua.Electrical.UI.EarthingGrid;

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
                    BlockScale = Parameter.BlockScale,
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
                    FrameStyle = uiCapitalConverter.Parameter.BlkFrameValue,
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

        /// <summary>
        /// 安防平面
        /// </summary>
        [CommandMethod("TIANHUACAD", "THAFPM", CommandFlags.Modal)]
        public void THAFPM()
        {
            var ui = new SecurityPlaneSystemUI();
            AcadApp.ShowModalWindow(ui);
        }

        /// <summary>
        /// 消防广播
        /// </summary>
        [CommandMethod("TIANHUACAD", "THXFGB", CommandFlags.Modal)]
        public void THXFGB()
        {
            var ui = new ThBroadcastUI();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// 火灾报警
        /// </summary>
        [CommandMethod("TIANHUACAD", "THHZBJ", CommandFlags.Modal)]
        public void THHZBJUI()
        {
            var ui = new UIThFireAlarm();
            AcadApp.ShowModelessWindow(ui);
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

        ///// <summary>
        ///// 框线比较
        ///// </summary>
        //[CommandMethod("TIANHUACAD", "THKXDB", CommandFlags.Modal)]
        //public void FrameComparerUI()
        //{
        //    var dlg = new UIFrameComparer();
        //    AcadApp.ShowModelessDialog(dlg);
        //}

        /// <summary>
        /// 用电负荷
        /// </summary>
        [CommandMethod("TIANHUACAD", "THYDFHJS", CommandFlags.Modal)]
        public void ElectricalLoadUI()
        {
            using (var cmd = new ThElectricalLoadCalculationUICmd())
            {
                cmd.Execute();
            }
        }
        /// <summary>
        /// 防雷接地网
        /// </summary>
        [CommandMethod("TIANHUACAD", "THJDPM", CommandFlags.Modal)]
        public void THJDPM()
        {
            var earthGridUI = new UIEarthingGrid();
            earthGridUI.WindowStartupLocation = System.Windows.
                WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(earthGridUI);
        }
    }
}
