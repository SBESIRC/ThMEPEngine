using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmFixLayout.Command;
using ThMEPElectrical.FireAlarmArea;
using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmArea.Data;

#if (ACAD2016 || ACAD2018)
using ThMEPElectrical.FireAlarmDistance;
using ThMEPElectrical.FireAlarmDistance.Data;
#endif

namespace ThMEPElectrical.AFAS
{
    public class ThAFASCmds
    {
        [CommandMethod("TIANHUACAD", "THFASmokeNoUI", CommandFlags.Modal)]
        public void THFASmokeNoUI()
        {
            using (var cmd = new ThAFASSmokeCmd(false))
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFASmoke", CommandFlags.Modal)]
        public void THFASmoke()
        {
            using (var cmd = new ThAFASSmokeCmd(true))
            {
                cmd.Execute();
            }
        }


        [CommandMethod("TIANHUACAD", "THFADisplayNoUI", CommandFlags.Modal)]
        public void THFADisplayNoUI()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFADisplay", CommandFlags.Modal)]
        public void THFADisplay()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd(true))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitorNoUI", CommandFlags.Modal)]
        public void THFAMonitorNoUI()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitor", CommandFlags.Modal)]
        public void THFAMonitor()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd(true))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATelNoUI", CommandFlags.Modal)]
        public void THFATelNoUI()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATel", CommandFlags.Modal)]
        public void THFATel()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd(true))
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFAGasNoUI", CommandFlags.Modal)]
        public void THFAGasNoUI()
        {
            using (var cmd = new ThAFASGasCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGas", CommandFlags.Modal)]
        public void THFAGas()
        {
            using (var cmd = new ThAFASGasCmd(true))
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFABroadcastNoUI", CommandFlags.Modal)]
        public void THFABroadcastNoUI()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd(false))
            {
                cmd.Execute();
            }
#else

#endif
        }

        [CommandMethod("TIANHUACAD", "THFABroadcast", CommandFlags.Modal)]
        public void THFABroadcast()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd(true))
            {
                cmd.Execute();
            }
#else

#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarmNoUI", CommandFlags.Modal)]
        public void THFAManualAlarmNoUI()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd(false))
            {
                cmd.Execute();
            }
#else

#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarm", CommandFlags.Modal)]
        public void THFAManualAlarm()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd(true))
            {
                cmd.Execute();
            }
#else

#endif
        }
    }

    public class THAFASDebugCmds
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFaDistData", CommandFlags.Modal)]
        public void THFaDistData()
        {
#if (ACAD2016 || ACAD2018)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var scale = 100;
                var referBeam = true;
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Broadcast_Ceiling, ThFaCommon.BlkName_Broadcast_Wall };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //画框，提数据，转数据
                var framePts = ThAFASUtils.GetFrame();
                if (framePts.Count == 0)
                {
                    return;
                }

                var geos = ThAFASUtils.GetDistLayoutData(framePts, extractBlkList, referBeam, true);

                var data = new ThAFASDistanceDataSet(geos);
                data.ExtendEquipment(cleanBlkName, scale);
                data.FilterBeam();
                data.print();

                var room = data.GetRoom();

                /////debug
                //var roomLable = data.GetRoomGeom();
                //for (int i = 0; i < roomLable.Count; i++)
                //{
                //    var pl = roomLable[i].Boundary as Polyline;
                //    var pt = pl.GetCentroidPoint();
                //    DrawUtils.ShowGeometry(pt, String.Format("placement：{0}", roomLable[i].Properties["Placement"]), "l0RoomPlacement", 3, 25, 200);
                //    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 1, 0), String.Format("name：{0}", roomLable[i].Properties["Name"]), "l0RoomName", 3, 25, 200);
                //    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 2, 0), String.Format("Privacy：{0}", roomLable[i].Properties["Privacy"]), "l0RoomPrivacy", 3, 25, 200);
                //}

                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name);
            }
#endif
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THFaAreaData", CommandFlags.Modal)]
        public void ThFaAreaData()
        {
            var extractBlkList = ThFaCommon.BlkNameList;
            var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Smoke, ThFaCommon.BlkName_Heat };
            var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

            //画框，提数据，转数据
            var pts = ThAFASUtils.GetFrame();
            if (pts.Count == 0)
            {
                return;
            }
            var referBeam = true;
            var wallThick = 100;
            var needDetective = true;

            var theta = 0;
            var floorHight = 2;
            var layoutType = ThFaSmokeCommon.layoutType.smoke;

            var geos = ThAFASUtils.GetSmokeData(pts, extractBlkList, referBeam, wallThick, needDetective);

            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name);


            var dataQuery = new ThAFASAreaDataQueryService(geos, cleanBlkName, avoidBlkName);

            DrawUtils.ShowGeometry(dataQuery.ArchitectureWalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Shearwalls.Select(x => x.Boundary).ToList(), "l0Wall", 10);
            DrawUtils.ShowGeometry(dataQuery.Columns.Select(x => x.Boundary).ToList(), "l0Column", 3);
            DrawUtils.ShowGeometry(dataQuery.LayoutArea.Select(x => x.Boundary).ToList(), "l0PlaceCoverage", 200);
            DrawUtils.ShowGeometry(dataQuery.Holes.Select(x => x.Boundary).ToList(), "l0hole", 140);
            DrawUtils.ShowGeometry(dataQuery.DetectArea.Select(x => x.Boundary).ToList(), "l0DetectArea", 96);

            //洞,必须先做找到框线
            dataQuery.AnalysisHoles();
            var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

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
        [CommandMethod("TIANHUACAD", "THFAFixData", CommandFlags.Modal)]
        public void THFAFixData()
        {
            var extractBlkList = ThFaCommon.BlkNameList;

            //把Cad图纸数据写出到Geojson File中
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pts = ThAFASUtils.GetFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThAFASUtils.GetFixLayoutData(pts, extractBlkList);

                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name);
            }
        }

        //[System.Diagnostics.Conditional("DEBUG")]
        //[CommandMethod("TIANHUACAD", "CleanDebugLayer", CommandFlags.Modal)]
        //public void ThCleanDebugLayer()
        //{
        //    // 调试按钮关闭且图层不是保护半径有效图层
        //    var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
        //    if (debugSwitch)
        //    {
        //        ThFaCleanService.ClearDrawing();
        //    }
        //}

        //[System.Diagnostics.Conditional("DEBUG")]
        //[CommandMethod("TIANHUACAD", "THFaBuffer", CommandFlags.Modal)]
        //public void ThFaBuffer()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var result = Active.Editor.GetEntity("\n请选择对象");
        //        if (result.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        var obj = acadDatabase.Element<Ellipse>(result.ObjectId);
        //        var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
        //        var a = bufferService.Buffer(obj, 1000);

        //        DrawUtils.ShowGeometry(a, "l0buffer");

        //    }



        //}

        //[System.Diagnostics.Conditional("DEBUG")]
        //[CommandMethod("TIANHUACAD", "THGetOffsetCurveTest", CommandFlags.Modal)]
        //public void ThGetOffsetCurveTest()
        //{
        //    var frame = ThAFASUtils.SelectFrame();
        //    var dir = 1;
        //    if (frame.IsCCW() == false)
        //    {
        //        dir = -1;
        //    }
        //    var newFrame = frame.GetOffsetCurves(dir * 15).Cast<Polyline>().OrderByDescending(y => y.Area).FirstOrDefault();
        //    DrawUtils.ShowGeometry(newFrame, "l0buffer", 140);
        //}
    }
}
