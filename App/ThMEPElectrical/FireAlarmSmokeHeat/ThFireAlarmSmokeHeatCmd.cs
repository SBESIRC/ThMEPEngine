using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThMEPEngineCore.Command;
using ThMEPEngineCore.Stair;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm;

using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Model;

namespace ThMEPElectrical.FireAlarmSmokeHeat
{
    public class ThFireAlarmSmokeHeatCmdsNoUI
    {
        [CommandMethod("TIANHUACAD", "THSmokeLayout", CommandFlags.Modal)]
        public void FireAlarmSmokeHeatCmd()
        {
            using (var cmd = new ThFireAlarmSmokeHeatCmd())
            {
                cmd.Execute();
            }
        }


        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "CleanDebugLayer", CommandFlags.Modal)]
        public void ThCleanDebugLayer()
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                Common.ThFaCleanService.ClearDrawing();
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThFaDataGJson", CommandFlags.Modal)]
        public void ThFaDataGJson()
        {
            var extractBlkList = ThFaCommon.BlkNameListAreaLayout;
            var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
            var avoidBlkName = ThFaCommon.BlkNameListAreaLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();

            //画框，提数据，转数据
            var pts = ThFireAlarmUtils.getFrame();
            if (pts.Count == 0)
            {
                return;
            }

            var geos = ThFireAlarmUtils.writeSmokeData(pts, extractBlkList, false);
            if (geos.Count == 0)
            {
                return;
            }

            var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
           
            DrawUtils.ShowGeometry(dataQuery.ArchitectureWalls.Select (x=>x.Boundary ).ToList (), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Shearwalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Columns.Select(x => x.Boundary).ToList(), "l0Column", 3);
            DrawUtils.ShowGeometry(dataQuery.LayoutArea.Select(x => x.Boundary ).ToList(), "l0PlaceCoverage", 200);

            //洞,必须先做找到框线
            dataQuery.analysisHoles();
            //墙，柱，可布区域，避让
           
            foreach (var frame in dataQuery.FrameHoleList)
            {
                DrawUtils.ShowGeometry(frame.Key, string.Format("l0room"), 30);
                DrawUtils.ShowGeometry(frame.Value, string.Format("l0hole"), 140);
            }

        }
    }

    public class ThFireAlarmSmokeHeatCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs;

        private int _theta = 0;
        private int _floorHight = 0;
        private double _scale = 100;
        private bool _referBeam = true;

        public ThFireAlarmSmokeHeatCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmSmokeLayout";
            ActionName = "布置";
            setInfo();
        }

        public ThFireAlarmSmokeHeatCmd()
        {

        }

        public override void SubExecute()
        {
            FireAlarmSmokeHeatLayoutExecute();
        }

        private void setInfo()
        {
            if (_UiConfigs != null)
            {
                _theta = _UiConfigs.SelectedIndexForAngle;
                _floorHight = _UiConfigs.SelectedIndexForH;
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;
                _referBeam = _UiConfigs.ShouldConsiderBeam;
            }
        }

        public void Dispose()
        {
        }

        private void FireAlarmSmokeHeatLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameListAreaLayout;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
                var avoidBlkName = ThFaCommon.BlkNameListAreaLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameSmoke = ThFaCommon.BlkName_Smoke;
                var layoutBlkNameHeat = ThFaCommon.BlkName_Heat;

                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThFireAlarmUtils.getSmokeData(pts, extractBlkList, _referBeam);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThFireAlarmUtils.transformToOrig(pts, geos);

                var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.analysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                //dataQuery.getAreaSensorType();
                var roomType = ThFaAreaLayoutRoomTypeService.getAreaSensorType(dataQuery.Rooms, dataQuery.roomFrameDict);

                foreach (var frame in dataQuery.FrameHoleList)
                {
                    DrawUtils.ShowGeometry(frame.Key, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(frame.Value, string.Format("l0hole"), 140);
                }

                var layoutParameter = new ThFaAreaLayoutParameter();

                //接入楼梯
                var stairBoundary = layoutParameter.RoomType.Where(x => x.Value == ThFaSmokeCommon.layoutType.stair).Select(x => x.Key).ToList();
                //boundary 到原位置
                stairBoundary.ForEach(x => transformer.Reset(x));
                var stairEngine = new ThStairEquimentLayout();
                var stairFireDetector = stairEngine.StairFireDetector(acadDatabase.Database, stairBoundary, pts, _scale);
                var stairFirePts = stairFireDetector.Select(x => x.Key).ToList();
                //楼梯间结果，楼梯房间框线转到原点位置
                stairFirePts.ForEach(x => transformer.Transform(x));
                stairBoundary.ForEach(x => transformer.Transform(x));
                ////

                var smokeResult = new ThFaAreaLayoutResult();
                var heatResult = new ThFaAreaLayoutResult();

                
                layoutParameter.FloorHightIdx = _floorHight;
                layoutParameter.RootThetaIdx = _theta;
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = 0.025;
                layoutParameter.BlkNameHeat = layoutBlkNameHeat;
                layoutParameter.stairPartResult = stairFirePts;
                layoutParameter.RoomType = roomType;

                ThFireAlarmSmokeHeatEngine.thFaSmokeHeatLayoutEngine(dataQuery, heatResult, smokeResult, layoutParameter);

                //转回到原始位置
                heatResult.transformBack(transformer);
                smokeResult.transformBack(transformer);
                stairFireDetector = stairFireDetector.ToDictionary(x => transformer.Reset(x.Key), x => x.Value);

                //打印
                ThFireAlarmInsertBlk.InsertBlock(heatResult.layoutPts.ToList(), _scale, layoutBlkNameHeat, ThFaCommon.blk_layer[layoutBlkNameHeat],false); ;
                ThFireAlarmInsertBlk.InsertBlock(smokeResult.layoutPts.ToList(), _scale, layoutBlkNameSmoke, ThFaCommon.blk_layer[layoutBlkNameSmoke],false);
                ThFireAlarmInsertBlk.InsertBlock(stairFireDetector, _scale, layoutBlkNameSmoke, ThFaCommon.blk_layer[layoutBlkNameSmoke]);

            }
        }
    }
}
