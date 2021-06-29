using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public class PlumbingWPFUIApp : IExtensionApplication
    {
        uiDrainageSystem uiDrainage;
        uiDrainageSystemSet uiSet;
        FireHydrant uiFireHydrant;
        FlushPointUI uiFlushPoint;
        public void Initialize()
        {
            uiFireHydrant = null;
            uiFlushPoint = null;
            if (ThHydrantProtectionRadiusCmd.FireHydrantVM==null)
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
        [CommandMethod("TIANHUACAD", "THDSPSXT", CommandFlags.Modal)]
        public void THSSUI()
        {
            if (null != uiDrainage && uiDrainage.IsLoaded)
                return;

            uiDrainage = new uiDrainageSystem();
            AcadApp.ShowModelessWindow(uiDrainage);
        }
        [CommandMethod("TIANHUACAD", "THDSPSXTSet", CommandFlags.Modal)]
        public void THSSUI2()
        {
            if (null != uiSet && uiSet.IsLoaded)
                return;

            uiSet = new uiDrainageSystemSet();
            AcadApp.ShowModelessWindow(uiSet);
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
            if (ThMEPWSS.Pipe.Service.ThRainSystemService.commandContext != null) return;
            var ui = new uiRainSystem();
            AcadApp.ShowModelessWindow(ui);
        }


        //[CommandMethod("TIANHUACAD", "THFCSD", CommandFlags.Modal)]
        public void ThCreateFireControlSystemDiagram()
        {
            var ui = new uiFireControlSystem();
            AcadApp.ShowModelessWindow(ui);
        }

        /// <summary>
        /// 地上标准层排水、雨水平面	
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDSPSYSXT", CommandFlags.Modal)]
        public void ThDrainageSysAboveGround()
        {
            var ui = new uiDrainageSysAboveGround();
            AcadApp.ShowModelessWindow(ui);
        }

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

        [CommandMethod("TIANHUACAD", "THXHSJH", CommandFlags.Modal)]
        public void THXHSJH()
        {
            if (uiFireHydrant != null && uiFireHydrant.IsLoaded)
                return;
            uiFireHydrant = new FireHydrant(ThHydrantProtectionRadiusCmd.FireHydrantVM);
            AcadApp.ShowModelessWindow(uiFireHydrant);
        }
    }
}
