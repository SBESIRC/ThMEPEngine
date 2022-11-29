using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical;
using ThMEPElectrical.Model;
using TianHua.Electrical.UI.Command;
using TianHua.Electrical.UI.FireAlarm;
using TianHua.Electrical.UI.ThBroadcast;
using TianHua.Electrical.UI.EarthingGrid;
using TianHua.Electrical.UI.BlockConvert;
using TianHua.Electrical.UI.SecurityPlaneUI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using TianHua.Electrical.UI.UI;

namespace TianHua.Electrical.UI
{
    public class ElectricalUIApp : IExtensionApplication
    {
        private fmSmokeLayout SmokeLayoutUI { get; set; }
        private BlockConvertUI UiCapitalConverter { get; set; }
        private ChargerDistributionUI ChargerDistributionUI { get; set; }


        public void Initialize()
        {
            SmokeLayoutUI = null;
            UiCapitalConverter = null;
            ChargerDistributionUI = null;
        }

        public void Terminate()
        {
            SmokeLayoutUI = null;
            UiCapitalConverter = null;
            ChargerDistributionUI = null;
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
                if (UiCapitalConverter != null && UiCapitalConverter.IsLoaded)
                {
                    return;
                }
                UiCapitalConverter = new BlockConvertUI
                {
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                };
                AcadApp.ShowModelessWindow(UiCapitalConverter);
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
        /// 充电桩配电平面
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCDZPD", CommandFlags.Modal)]
        public void THParkLightUI()
        {
            if (ChargerDistributionUI == null)
            {
                ChargerDistributionUI = new ChargerDistributionUI();
            }
            AcadApp.ShowModelessWindow(ChargerDistributionUI);
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
