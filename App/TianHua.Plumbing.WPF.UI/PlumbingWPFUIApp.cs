using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPWSS.UndergroundFireHydrantSystem.UI;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public class PlumbingWPFUIApp : IExtensionApplication
    {
        uiDrainageSystem uiDrainage;
        uiDrainageSystemSet uiSet;
        FireHydrant uiFireHydrant;
        FlushPointUI uiFlushPoint;
        uiDrainageSysAboveGround uiAGSysDrain;
        uiFireControlSystem uiTHXHSXTT;
        public void Initialize()
        {
            uiFireHydrant = null;
            uiFlushPoint = null;
            uiTHXHSXTT = null;
            if (ThHydrantProtectionRadiusCmd.FireHydrantVM == null)
            {
                ThHydrantProtectionRadiusCmd.FireHydrantVM = new ThFireHydrantVM();
            }
            if (THLayoutFlushPointCmd.FlushPointVM == null)
            {
                THLayoutFlushPointCmd.FlushPointVM = new ThFlushPointVM();
            }
        }

        public void Terminate()
        {
        }

        /// <summary>
        /// 给水系统图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THJSXTT", CommandFlags.Modal)]
        public void ThCreateWaterSuplySystemDiagramWithUI()
        {
            if (null != uiDrainage && uiDrainage.IsLoaded)
                return;

            uiDrainage = new uiDrainageSystem();
            AcadApp.ShowModelessWindow(uiDrainage);
        }

        /// <summary>
        /// 图块名称配置
        /// </summary>
        [CommandMethod("TIANHUACAD", "THWTKSB", CommandFlags.Modal)]
        public void ThBlockNameConfigWithUI()
        {
            if(!uiBlockNameConfig.staticUIBlockName.IsVisible)
                AcadApp.ShowModelessWindow(uiBlockNameConfig.staticUIBlockName);
        }

        /// <summary>
        /// 潜水泵布置
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSJSB", CommandFlags.Modal)]
        public void ThArrangePumpWithUI()
        {
            var ui = new UiWaterWellPump();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// 地上雨水系统图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THYSXTT", CommandFlags.Modal)]
        public void ThCreateRainSystemDiagram()
        {
            if (ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.commandContext != null) return;
            var ui = new uiRainSystem();
            AcadApp.ShowModelessWindow(ui);
        }
        /// <summary>
        /// 地上排水系统图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THPSXTT", CommandFlags.Modal)]
        public void ThCreateDrainageSystemDiagram()
        {
            if (ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext != null) return;
            var ui = new DrainageSystemUI();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// 排雨水平面	
        /// </summary>
        [CommandMethod("TIANHUACAD", "THPYSPM", CommandFlags.Modal)]
        public void ThDrainageSysAboveGround()
        {
            if (uiAGSysDrain != null && uiAGSysDrain.IsLoaded)
                return;
            uiAGSysDrain = new uiDrainageSysAboveGround();
            AcadApp.ShowModelessWindow(uiAGSysDrain);
        }

        /// <summary>
        /// 冲洗点位
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDXCX", CommandFlags.Modal)]
        public void THDXCX()
        {
            if (uiFlushPoint != null && uiFlushPoint.IsLoaded)
            {
                return;
            }
            uiFlushPoint = new FlushPointUI(THLayoutFlushPointCmd.FlushPointVM);
            AcadApp.ShowModelessWindow(uiFlushPoint);
        }

        /// <summary>
        /// 消火栓校核
        /// </summary>
        [CommandMethod("TIANHUACAD", "THXHSJH", CommandFlags.Modal)]
        public void THXHSJH()
        {
            if (uiFireHydrant != null && uiFireHydrant.IsLoaded)
                return;
            uiFireHydrant = new FireHydrant(ThHydrantProtectionRadiusCmd.FireHydrantVM);
            AcadApp.ShowModelessWindow(uiFireHydrant);
        }

        /// <summary>
        /// 地下压力排水系统图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDXPSXTT", CommandFlags.Modal)]
        public void ThCreateUNDPDrainageSystemDiagram()
        {
            var ui = new uiUNDPressureDrainageSystem();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// 地下消火栓系统图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDXXHSXTT", CommandFlags.Modal)]
        public void THDXXHSXTT()
        {
            var uiDrainage = new uiFireHydrantSystem();
            AcadApp.ShowModelessWindow(uiDrainage);
        }

        /// <summary>
        /// 消火栓连管
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDXXHS", CommandFlags.Modal)]
        public void ThHydrantConnectPipeUI()
        {
            var ui = new UiHydrantConnectPipe();
            AcadApp.ShowModelessWindow(ui);
        }
        /// <summary>
        /// 消火栓系统图（目前只实现了塔楼部分）
        /// </summary>
        [CommandMethod("TIANHUACAD", "THXHSXTT", CommandFlags.Modal)]
        public void THXHSXTT()
        {
            if (null != uiTHXHSXTT && uiTHXHSXTT.IsLoaded)
                return;

            uiTHXHSXTT = new uiFireControlSystem();
            AcadApp.ShowModelessWindow(uiTHXHSXTT);
        }

        /// <summary>
        ///
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLGHZ", CommandFlags.Modal)]
        public void ThPipeDrawUI()
        {
            var ui = new uiPipeDrawControl();
            AcadApp.ShowModelessWindow(ui);
        }
    }
}
