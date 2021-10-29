using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
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
            var extractBlkList = ThFaCommon.BlkNameList;
            var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
            var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

            //画框，提数据，转数据
            var pts = ThFireAlarmUtils.getFrameBlk ();
            if (pts.Count == 0)
            {
                return;
            }
            var bBean = true;
            var geos = ThFireAlarmUtils.writeSmokeData(pts, extractBlkList, bBean);
            if (geos.Count == 0)
            {
                return;
            }

            ////转回原点
            //var transformer = ThFireAlarmUtils.transformToOrig(pts, geos);
            ////var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
            ////newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
            ////var transformer = ThFireAlarmUtils.transformToOrig(newPts, geos);

            var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);

            DrawUtils.ShowGeometry(dataQuery.ArchitectureWalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Shearwalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Columns.Select(x => x.Boundary).ToList(), "l0Column", 3);
            DrawUtils.ShowGeometry(dataQuery.LayoutArea.Select(x => x.Boundary).ToList(), "l0PlaceCoverage", 200);
            DrawUtils.ShowGeometry(dataQuery.Holes .Select(x => x.Boundary).ToList(), "l0hole", 140);

            //洞,必须先做找到框线
            dataQuery.analysisHoles();
            var roomType = ThFaAreaLayoutRoomTypeService.getAreaSensorType(dataQuery.Rooms, dataQuery.roomFrameDict);

            foreach (var frame in dataQuery.FrameList)
            {
                DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0analysisHole"), 190);
                DrawUtils.ShowGeometry(frame.GetPoint3dAt(0), string.Format("roomType:{0}", roomType[frame].ToString()), "l0roomType", 25, 25, 200);
            }

        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThGetOffsetCurveTest", CommandFlags.Modal)]
        public void ThGetOffsetCurveTest()
        {
            var frame = ThFireAlarmUtils.SelectFrame();
            var dir = 1;
            if (frame.IsCCW() == false)
            {
                dir = -1;
            }
            var newFrame = frame.GetOffsetCurves(dir * 15).Cast<Polyline>().OrderByDescending(y => y.Area).FirstOrDefault();
            DrawUtils.ShowGeometry(newFrame, "l0buffer", 140);
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
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat, ThFaCommon.BlkName_Smoke_ExplosionProf, ThFaCommon.BlkName_Heat_ExplosionProf };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameSmoke = ThFaCommon.BlkName_Smoke;
                var layoutBlkNameHeat = ThFaCommon.BlkName_Heat;
                var layoutBlkNameSmokePrf = ThFaCommon.BlkName_Smoke_ExplosionProf;
                var layoutBlkNameHeatPrf = ThFaCommon.BlkName_Heat_ExplosionProf;

                //导入块图层。free图层
                ThFireAlarmInsertBlk.prepareInsert(extractBlkList, ThFaCommon.blk_layer.Select(x => x.Value).Distinct().ToList());

                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrameBlk();
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
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThFireAlarmUtils.transformToOrig(newPts, geos);

                var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.analysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                var priorityExtend = ThFaAreaLayoutParamterCalculationService.getPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.extendPriority(priorityExtend);
                var roomType = ThFaAreaLayoutRoomTypeService.getAreaSensorType(dataQuery.Rooms, dataQuery.roomFrameDict);

                foreach (var frame in dataQuery.FrameList)
                {
                    DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
                    DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), "l0PlaceCoverage", 200);
                    DrawUtils.ShowGeometry(frame.GetPoint3dAt(0), string.Format("roomType:{0}", roomType[frame].ToString()), "l0roomType", 25, 25, 200);

                }


                var layoutParameter = new ThFaAreaLayoutParameter();
                layoutParameter.FloorHightIdx = _floorHight;
                layoutParameter.RootThetaIdx = _theta;
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = 0.025;
                layoutParameter.BlkNameHeat = layoutBlkNameHeat;
                layoutParameter.BlkNameSmoke = layoutBlkNameSmoke;
                layoutParameter.BlkNameHeatPrf = layoutBlkNameHeatPrf;
                layoutParameter.BlkNameSmokePrf = layoutBlkNameSmokePrf;
                layoutParameter.RoomType = roomType;
                layoutParameter.framePts = pts;
                layoutParameter.transformer = transformer;
                layoutParameter.priorityExtend = priorityExtend;

                //接入楼梯
                var stairBlkResult = ThStairService.layoutStair(layoutParameter);
                ////

                ThFireAlarmSmokeHeatEngine.thFaSmokeHeatLayoutEngine(dataQuery, layoutParameter, out var layoutResult, out var blindsResult);

                //转回到原始位置
                layoutResult.ForEach(x => x.transformBack(transformer));

                //打印
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);

            }
        }
    }
}
