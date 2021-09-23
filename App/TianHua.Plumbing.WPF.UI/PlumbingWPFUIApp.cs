using Linq2Acad;
using System.Linq;
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FlushPoint.Data;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.UI;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public class PlumbingWPFUIApp : IExtensionApplication
    {
        
        uiDrainageSystemSet uiSet;
        FireHydrant uiFireHydrant;
        FlushPointUI uiFlushPoint;
        uiDrainageSysAboveGround uiAGSysDrain;
        public void Initialize()
        {
            uiFireHydrant = null;
            uiFlushPoint = null;
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
            var file = CadCache.CurrentFile;
            if (file == null) return ;
            var ok = !CadCache.Locks.Contains(CadCache.WaterGroupLock);
            if (!ok) return ;
            var w = new uiDrainageSystem();
            w.Loaded += (s, e) => { CadCache.Locks.Add(CadCache.WaterGroupLock); };
            w.Closed += (s, e) => { CadCache.Locks.Remove(CadCache.WaterGroupLock); };
            AcadApp.ShowModelessWindow(w);
        }

        /// <summary>
        /// 图块名称配置
        /// </summary>
        [CommandMethod("TIANHUACAD", "THWTKSB", CommandFlags.Modal)]
        public void ThBlockNameConfigWithUI()
        {
            if (!uiBlockNameConfig.staticUIBlockName.IsVisible)
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
            var ui = uiRainSystem.TryCreate(RainSystemDiagramViewModel.Singleton);
            if (ui != null) AcadApp.ShowModelessWindow(ui);
        }
        /// <summary>
        /// 地上排水系统图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THPSXTT", CommandFlags.Modal)]
        public void ThCreateDrainageSystemDiagram()
        {
            var ui = DrainageSystemUI.TryCreate(DrainageSystemDiagramViewModel.Singleton);
            if (ui != null) AcadApp.ShowModelessWindow(ui);
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
            THLayoutFlushPointCmd.FlushPointVM.Parameter.BlockNameDict =
                uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
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
            var file = CadCache.CurrentFile;
            if (file == null) return;
            var ok = !CadCache.Locks.Contains(CadCache.WaterGroupLock);
            if (!ok) return;
            var w = new uiFireControlSystem();
            w.Loaded += (s, e) => { CadCache.Locks.Add(CadCache.WaterGroupLock); };
            w.Closed += (s, e) => { CadCache.Locks.Remove(CadCache.WaterGroupLock); };
            AcadApp.ShowModelessWindow(w);
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

        /// <summary>
        /// 冷水给水轴侧
        /// </summary>
        [CommandMethod("TIANHUACAD", "THJSZC", CommandFlags.Modal)]
        public void ThDrainageAxonoCoolSupply()
        {
            var ui = new DrainageSystemSupplyAxonometricUI();
            AcadApp.ShowModelessWindow(ui);

        }

        [CommandMethod("TIANHUACAD", "THExtractWSSDrainageWell", CommandFlags.Modal)]
        public void THExtractWSSDrainageWell()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var per = AcHelper.Active.Editor.GetEntity("请框选一个范围");
                if (per.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    return;
                }

                var entity = acadDb.Element<Entity>(per.ObjectId);
                if (entity is Polyline poly)
                {
                    var pts = poly.EntityVertices();
                    var drainageWellBlkNames = new List<string>();
                    var blkNameDict = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
                    if (blkNameDict.ContainsKey("集水井"))
                    {
                        drainageWellBlkNames = blkNameDict["集水井"];
                    }
                    var drainFacilityExtractor = new ThDrainFacilityExtractor()
                    {
                        ColorIndex = 1,
                        DrainageBlkNames = drainageWellBlkNames.Distinct().ToList(),
                    };
                    drainFacilityExtractor.Extract(acadDb.Database, pts);
                    drainFacilityExtractor.CollectingWells.CreateGroup(acadDb.Database, 5);
                    drainFacilityExtractor.DrainageDitches.CreateGroup(acadDb.Database, 6);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThExtractWSSParkingStall", CommandFlags.Modal)]
        public void ThExtractParkingStall()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var per = AcHelper.Active.Editor.GetEntity("请框选一个范围");
                if (per.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    return;
                }

                var entity = acadDb.Element<Entity>(per.ObjectId);
                if (entity is Polyline poly)
                {
                    var pts = poly.EntityVertices();
                    var parkingStallBlkNames = new List<string>();
                    var blkNameDict = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
                    if (blkNameDict.ContainsKey("机械车位"))
                    {
                        parkingStallBlkNames.AddRange(blkNameDict["机械车位"]);
                    }
                    if (blkNameDict.ContainsKey("非机械车位"))
                    {
                        parkingStallBlkNames.AddRange(blkNameDict["非机械车位"]);
                    }
                    var parkingStallExtractor = new ThParkingStallExtractor()
                    {
                        ColorIndex = 1,
                        BlockNames = parkingStallBlkNames.Distinct().ToList(),
                    };
                    parkingStallExtractor.Extract(acadDb.Database, pts);
                    parkingStallExtractor.ParkingStalls.Cast<Entity>().ToList().CreateGroup(acadDb.Database, 1);
                }
            }
        }
    }
}
