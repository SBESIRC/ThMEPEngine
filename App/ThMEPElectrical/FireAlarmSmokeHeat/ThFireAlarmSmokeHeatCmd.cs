﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;

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
        [CommandMethod("TIANHUACAD", "ThFaBuffer", CommandFlags.Modal)]
        public void ThBuffer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var obj = acadDatabase.Element<Ellipse>(result.ObjectId);
                var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
                var a = bufferService.Buffer(obj, 1000);
           
                DrawUtils.ShowGeometry(a, "l0buffer");

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
            var pts = ThFireAlarmUtils.GetFrame();
            if (pts.Count == 0)
            {
                return;
            }
            var bBean = true;
            var wallThick = 100;
            var theta = 0;
            var floorHight = 2;
            var layoutType = ThFaSmokeCommon.layoutType.smoke;

            var geos = ThFireAlarmUtils.WriteSmokeData(pts, extractBlkList, bBean, wallThick);
            if (geos.Count == 0)
            {
                return;
            }

            var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);

            DrawUtils.ShowGeometry(dataQuery.ArchitectureWalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Shearwalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Columns.Select(x => x.Boundary).ToList(), "l0Column", 3);
            DrawUtils.ShowGeometry(dataQuery.LayoutArea.Select(x => x.Boundary).ToList(), "l0PlaceCoverage", 200);
            DrawUtils.ShowGeometry(dataQuery.Holes.Select(x => x.Boundary).ToList(), "l0hole", 140);
            DrawUtils.ShowGeometry(dataQuery.DetectArea.Select(x => x.Boundary).ToList(), "l0DetectArea", 96);
            //洞,必须先做找到框线
            dataQuery.AnalysisHoles();
            var roomType = ThFaAreaLayoutRoomTypeService.GetAreaSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

            foreach (var frame in dataQuery.FrameList)
            {
                var centPt = frame.GetCentroidPoint();
                DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0analysisHole"), 190);
                DrawUtils.ShowGeometry(centPt, string.Format("roomType:{0}", roomType[frame].ToString()), "l0roomType", 25, 25, 200);

                var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, floorHight, theta, layoutType);//to do...frame.area need to remove hole's area
                var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, 0.025);

                DrawUtils.ShowGeometry(new Autodesk.AutoCAD.Geometry.Point3d(centPt.X, centPt.Y - 350 * 1, 0), string.Format("r:{0}", radius), "l0radius", 25, 25, 200);
                DrawUtils.ShowGeometry(new Autodesk.AutoCAD.Geometry.Point3d(centPt.X, centPt.Y - 350 * 2, 0), string.Format("bIsAisleArea:{0}", bIsAisleArea), "l0Area", 25, 25, 200);
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
        private double _wallThick = 100;
        public ThFireAlarmSmokeHeatCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmSmokeLayout";
            ActionName = "布置";
            SetInfo();
        }

        public ThFireAlarmSmokeHeatCmd()
        {
            SetInfo();
        }

        public override void SubExecute()
        {
            FireAlarmSmokeHeatLayoutExecute();
        }

        private void SetInfo()
        {
            if (_UiConfigs != null)
            {
                _theta = _UiConfigs.SelectedIndexForAngle;
                _floorHight = _UiConfigs.SelectedIndexForH;
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;
                _referBeam = _UiConfigs.ShouldConsiderBeam;
                _wallThick = 50;
            }
            else
            {
                _theta = 0;
                _floorHight = 2;
                _scale = 100;
                _referBeam = true;
                _wallThick = 50;
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
                var pts = ThFireAlarmUtils.GetFrameBlk();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThFireAlarmUtils.GetSmokeData(pts, extractBlkList, _referBeam, _wallThick);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThFireAlarmUtils.TransformToOrig(pts, geos);
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThFireAlarmUtils.transformToOrig(newPts, geos);

                var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.AnalysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                var priorityExtend = ThFaAreaLayoutParamterCalculationService.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);
                var roomType = ThFaAreaLayoutRoomTypeService.GetAreaSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

                foreach (var frame in dataQuery.FrameList)
                {
                    DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
                    //DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), "l0PlaceCoverage", 200);
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
                var stairBlkResult = ThStairService.LayoutStair(layoutParameter);
                ////

                ThFireAlarmSmokeHeatEngine.ThFaSmokeHeatLayoutEngine(dataQuery, layoutParameter, out var layoutResult, out var blindsResult);

                //转回到原始位置
                layoutResult.ForEach(x => x.TransformBack(transformer));

                //打印
                ThFireAlarmInsertBlk.InsertBlock(layoutResult, _scale);
                ThFireAlarmInsertBlk.InsertBlockAngle(stairBlkResult, _scale);

            }
        }
    }
}
