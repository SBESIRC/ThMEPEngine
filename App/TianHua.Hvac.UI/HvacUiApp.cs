using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using TianHua.Hvac.UI.Command;
using TianHua.Hvac.UI.EQPMFanSelect.EventMonitor;
using TianHua.Hvac.UI.SmokeProofSystemUI;
using TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofEventMonitor;
using TianHua.Hvac.UI.UI;
using TianHua.Hvac.UI.FloorHeatingCoil;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI
{
    public class HvacUiApp : IExtensionApplication
    {
        public void Initialize()
        {
            AcadApp.DocumentManager.DocumentToBeDestroyed += DocumentManager_DocumentToBeDestroyed;
            EQPMSelectAddEvents();
            SmokeProofAddEvents();
        }

        public void Terminate()
        {
            AcadApp.DocumentManager.DocumentToBeDestroyed -= DocumentManager_DocumentToBeDestroyed;
            EQPMSelectRemoveEvents();
            SmokeProofRemoveEvents();
        }
        private void DocumentManager_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (AcadApp.DocumentManager.Count == 1)
            {
                if (uiAirPortParameter.Instance != null)
                {
                    uiAirPortParameter.Instance.Hide();
                }
                if (uiFGDXParameter.Instance != null)
                {
                    uiFGDXParameter.Instance.Hide();
                }
            }
        }
        [CommandMethod("TIANHUACAD", "THDKFPMFG", CommandFlags.Modal)]
        public void THDKFPMFG()
        {
            using (var cmd = new ThHvacDuctModifyCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFPM", CommandFlags.Modal)]
        public void THFPM()
        {
            using (var cmd = new ThHvacFpmCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THXFJ", CommandFlags.Modal)]
        public void THXFJ()
        {
            using (var cmd = new ThHvacXfjCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THSPM", CommandFlags.Modal)]
        public void THSPM()
        {
            using (var cmd = new ThHvacSpmCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFHJS", CommandFlags.Modal)]
        public void THFHJS()
        {
            using (var cmd = new ThHvacLoadCalculationCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THSWSZ", CommandFlags.Modal)]
        public void THSWSZ()
        {
            using (var cmd = new ThHvacOutdoorVentilationCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFJBH", CommandFlags.Modal)]
        public void THFJBH()
        {
            using (var cmd = new ThHvacRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFJGN", CommandFlags.Modal)]
        public void THFJGN()
        {
            using (var cmd = new ThHvacExtractRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THSNJ", CommandFlags.Modal)]
        public void THSNJ()
        {
            using (var cmd = new ThHvacIndoorFanCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFGLY", CommandFlags.Modal)]
        public void THFGLY()
        {
            using (var cmd = new ThHvacRouteCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFGLG", CommandFlags.Modal)]
        public void THFGLG()
        {
            using (var cmd = new ThHvacFGLGInsertCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THCRFK", CommandFlags.Modal)]
        public void THCRFK()
        {
            if (null != uiAirPortParameter.Instance && uiAirPortParameter.Instance.IsLoaded)
            {
                if (!uiAirPortParameter.Instance.IsVisible)
                {
                    uiAirPortParameter.Instance.Show();
                }
                return;
            }
            uiAirPortParameter.Instance.WindowStartupLocation =
                System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(uiAirPortParameter.Instance);
        }
        [CommandMethod("TIANHUACAD", "THFGDX", CommandFlags.Modal)]
        public void THFGDX()
        {
            using (var cmd = new ThHvacFGDXUiCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THSGLY", CommandFlags.Modal)]
        public void THSGLY()
        {
            using (var cmd = new ThWaterPipeRouteCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THSGDX", CommandFlags.Modal)]
        public void THSGDX()
        {
            using (var cmd = new ThHvacSGDXInsertCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFJXX", CommandFlags.Modal)]
        public void THFJXX()
        {
            EQPMUIServices.Instance.ShowFanSelectUI("");
        }
        [CommandMethod("TIANHUACAD", "THFJEDITEX", CommandFlags.Modal)]
        public void THFJEDIT()
        {
            EQPMUIServices.Instance.ShowFanSelectUI("");
            EQPMUIServices.Instance.SelectFanBlock();
        }

        SmokeCalculateUI smokeCalculateUI;
        [CommandMethod("TIANHUACAD", "THSMKPS", CommandFlags.Modal)]
        public void ThWSSUI()
        {
            if (smokeCalculateUI != null && smokeCalculateUI.IsLoaded)
            {
                smokeCalculateUI.InitData();
                return;
            }
            smokeCalculateUI = new SmokeCalculateUI();
            AcadApp.ShowModelessWindow(smokeCalculateUI);
        }
        #region 风机选型的相关事件
        private void EQPMSelectAddEvents()
        {
            AcadApp.BeginDoubleClick += EQPMEventMonitor.Application_BeginDoubleClick;
            AcadApp.DocumentManager.DocumentActivated += EQPMEventMonitor.DocumentManager_DocumentActivated;
            AcadApp.DocumentManager.DocumentLockModeChanged += EQPMEventMonitor.DocumentManager_DocumentLockModeChanged;
            AcadApp.DocumentManager.DocumentLockModeChangeVetoed += EQPMEventMonitor.DocumentManager_DocumentLockModeChangeVetoed;
            EQPMEventMonitor.SubscribeToObjectOverrule();
            EQPMEventMonitor.SubscribeToDocumentEvents(Active.Document);
        }
        private void EQPMSelectRemoveEvents()
        {
            AcadApp.BeginDoubleClick -= EQPMEventMonitor.Application_BeginDoubleClick;
            AcadApp.DocumentManager.DocumentActivated -= EQPMEventMonitor.DocumentManager_DocumentActivated;
            AcadApp.DocumentManager.DocumentLockModeChanged -= EQPMEventMonitor.DocumentManager_DocumentLockModeChanged;
            AcadApp.DocumentManager.DocumentLockModeChangeVetoed -= EQPMEventMonitor.DocumentManager_DocumentLockModeChangeVetoed;
            EQPMEventMonitor.UnSubscribeToObjectOverrule();
            EQPMEventMonitor.UnSubscribeToDocumentEvents();
        }
        #endregion

        #region 正压送风平面的相关事件
        private void SmokeProofAddEvents()
        {
            AcadApp.BeginDoubleClick += SmokeProofEventMonitor.Application_BeginDoubleClick;
            AcadApp.DocumentManager.DocumentLockModeChanged += SmokeProofEventMonitor.DocumentManager_DocumentLockModeChanged;
            AcadApp.DocumentManager.DocumentLockModeChangeVetoed += SmokeProofEventMonitor.DocumentManager_DocumentLockModeChangeVetoed;
        }
        private void SmokeProofRemoveEvents()
        {
            AcadApp.BeginDoubleClick -= SmokeProofEventMonitor.Application_BeginDoubleClick;
            AcadApp.DocumentManager.DocumentLockModeChanged -= SmokeProofEventMonitor.DocumentManager_DocumentLockModeChanged;
            AcadApp.DocumentManager.DocumentLockModeChangeVetoed -= SmokeProofEventMonitor.DocumentManager_DocumentLockModeChangeVetoed;
        }
        #endregion

        [CommandMethod("TIANHUACAD", "THDNPG", CommandFlags.Modal)]
        public void THDNPG()
        {
            if (uiFloorHeatingCoil.Instance != null && uiFloorHeatingCoil.Instance.IsLoaded)
            {
                if (!uiFloorHeatingCoil.Instance.IsVisible)
                {
                    uiFloorHeatingCoil.Instance.Show();
                }
                return;
            }

            uiFloorHeatingCoil.Instance.WindowStartupLocation =
                System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(uiFloorHeatingCoil.Instance);
        }
    }
}
