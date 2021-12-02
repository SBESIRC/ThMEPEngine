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
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmFixLayout.Command;
using ThMEPElectrical.FireAlarmSmokeHeat;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Service;
using ThMEPElectrical.FireAlarmCombustibleGas;


#if (ACAD2016 || ACAD2018)
using ThMEPElectrical.FireAlarmDistance;
using ThMEPElectrical.FireAlarmDistance.Data;
#endif

namespace ThMEPElectrical.FireAlarm
{
    public class ThAFASCmds
    {
        //[CommandMethod("TIANHUACAD", "THAFAS", CommandFlags.Modal)]
        //public void THAFAS()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var result = Active.Editor.GetEntity("\n请选择对象");
        //        if (result.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        var frame = acadDatabase.Element<Polyline>(result.ObjectId);
        //        var factory = new ThAFASDistanceDataSetFactory();
        //        var ds = factory.Create(acadDatabase.Database, frame.Vertices());

        //        //
        //        var partId = ds.Container.Where(o => o.Properties["Category"].ToString() == "FireApart").First().Properties["Id"].ToString();
        //        ds.Container.RemoveAll(o =>
        //        {
        //            if (o.Properties.ContainsKey("ParentId"))
        //            {
        //                if (o.Properties["ParentId"] == null)
        //                {
        //                    return true;
        //                }
        //                if (o.Properties["ParentId"].ToString() != partId && o.Properties["Category"].ToString() != "FireApart")
        //                {
        //                    return true;
        //                }
        //            }
        //            return false;
        //        });

        //        ThGeoOutput.Output(ds.Container, Active.DocumentDirectory, Active.DocumentName);
        //        var geojson = ThGeoOutput.Output(ds.Container);

        //        ThAFASPlacementEngineMgd engine = new ThAFASPlacementEngineMgd();
        //        ThAFASPlacementContextMgd context = new ThAFASPlacementContextMgd()
        //        {
        //            StepDistance = 20000,
        //            MountMode = ThAFASPlacementMountModeMgd.Wall,
        //        };

        //        var features = Export2NTSFeatures(engine.Place(geojson, context));

        //        var dxfNames = new string[]
        //        {
        //            RXClass.GetClass(typeof(Polyline)).DxfName,
        //        };
        //        var filter = ThSelectionFilterTool.Build(dxfNames);
        //        var psr = Active.Editor.GetSelection(filter);
        //        if (psr.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        var objs = new DBObjectCollection();
        //        foreach (var obj in psr.Value.GetObjectIds())
        //        {
        //            objs.Add(acadDatabase.Element<Polyline>(obj));
        //        }
        //        objs = objs.BuildArea();

        //        var geos = objs
        //            .OfType<Entity>()
        //            .Select(o => new ThGeometry() { Boundary = o })
        //            .ToList();
        //        ThGeoOutput.Output(geos, Active.DocumentDirectory, Active.DocumentName);
        //    }
        //}

        [CommandMethod("TIANHUACAD", "THFASmokeNoUI", CommandFlags.Modal)]
        public void THFASmokeNoUI()
        {
            using (var cmd = new ThFireAlarmSmokeHeatCmd(false))
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFASmoke", CommandFlags.Modal)]
        public void THFASmoke()
        {
            using (var cmd = new ThFireAlarmSmokeHeatCmd(true))
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

        [CommandMethod("TIANHUACAD", "ThFATelNoUI", CommandFlags.Modal)]
        public void ThFATelNoUI()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThFATel", CommandFlags.Modal)]
        public void ThFATel()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd(true))
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFAGasNoUI", CommandFlags.Modal)]
        public void ThFAGasNoUI()
        {
            using (var cmd = new ThFireAlarmGasCmd(false))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGas", CommandFlags.Modal)]
        public void ThFAGas()
        {
            using (var cmd = new ThFireAlarmGasCmd(true))
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "ThFABroadcastNoUI", CommandFlags.Modal)]
        public void ThFABroadcastNoUI()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd(false))
            {
                cmd.Execute();
            }
#else

#endif
        }

        [CommandMethod("TIANHUACAD", "ThFABroadcast", CommandFlags.Modal)]
        public void ThFABroadcast()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd(true))
            {
                cmd.Execute();
            }
#else

#endif
        }

        [CommandMethod("TIANHUACAD", "ThFAManualAlarmNoUI", CommandFlags.Modal)]
        public void ThFAManualAlarmNoUI()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd(false))
            {
                cmd.Execute();
            }
#else

#endif
        }

        [CommandMethod("TIANHUACAD", "ThFAManualAlarm", CommandFlags.Modal)]
        public void ThFAManualAlarm()
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
                var framePts = ThFireAlarmUtils.GetFrame();
                if (framePts.Count == 0)
                {
                    return;
                }

                var geos = ThFireAlarmUtils.GetDistLayoutData(framePts, extractBlkList, referBeam, true);

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
        [CommandMethod("TIANHUACAD", "ThFaAreaData", CommandFlags.Modal)]
        public void ThFaAreaData()
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
            var referBeam = true;
            var wallThick = 100;
            var needDetective = true;

            var theta = 0;
            var floorHight = 2;
            var layoutType = ThFaSmokeCommon.layoutType.smoke;

            var geos = ThFireAlarmUtils.GetSmokeData(pts, extractBlkList, referBeam, wallThick, needDetective);

            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name);


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
        [CommandMethod("TIANHUACAD", "THFAFixData", CommandFlags.Modal)]
        public void THFAFixData()
        {
            var extractBlkList = ThFaCommon.BlkNameList;

            //把Cad图纸数据写出到Geojson File中
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pts = ThFireAlarmUtils.GetFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThFireAlarmUtils.GetFixLayoutData(pts, extractBlkList);

                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(geos, path, fileInfo.Name);
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
                ThMEPElectrical.ThFaCleanService.ClearDrawing();
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThFaBuffer", CommandFlags.Modal)]
        public void ThFaBuffer()
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
}
