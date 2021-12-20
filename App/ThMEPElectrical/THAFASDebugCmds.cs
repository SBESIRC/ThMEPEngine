using System;
using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmDistance;
using ThMEPElectrical.FireAlarmDistance.Data;

namespace ThMEPElectrical
{
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

                var data = new ThAFASDistanceDataSet(geos, cleanBlkName, avoidBlkName);
                data.ClassifyData();
                data.CleanPreviousEquipment();
                data.ExtendEquipment(cleanBlkName, scale);
                data.FilterBeam();
                data.ProcessRoomPlacementLabel(ThFaDistCommon.BroadcastTag);

                data.print();

                ///debug
                var room = data.Room;
                for (int i = 0; i < room.Count; i++)
                {
                    var pl = room[i].Boundary as Polyline;
                    var pt = pl.GetCentroidPoint();
                    DrawUtils.ShowGeometry(pt, String.Format("placement：{0}", room[i].Properties["Placement"]), "l0RoomPlacement", 3, 25, 200);
                    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 1, 0), String.Format("name：{0}", room[i].Properties["Name"]), "l0RoomName", 3, 25, 200);
                    DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 300 * 2, 0), String.Format("Privacy：{0}", room[i].Properties["Privacy"]), "l0RoomPrivacy", 3, 25, 200);
                }

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

            //var pts = ThAFASUtils.GetRoomFrame();


            if (pts.Count == 0)
            {
                return;
            }
            var referBeam = true;
            var wallThick = 100;
            var needDetective = false;

            var theta = 0;
            var floorHight = 2;
            var layoutType = ThFaSmokeCommon.layoutType.smoke;

            var geos = ThAFASUtils.GetSmokeData(pts, extractBlkList, referBeam, wallThick, needDetective);

            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name);


            var dataQuery = new ThAFASAreaDataQueryService(geos, cleanBlkName, avoidBlkName);
            var beam = dataQuery.QueryC(ThMEPEngineCore.Model.BuiltInCategory.Beam.ToString());

            DrawUtils.ShowGeometry(dataQuery.Rooms.Select(x => x.Boundary).ToList(), "l0Room", 30);
            DrawUtils.ShowGeometry(dataQuery.ArchitectureWalls.Select(x => x.Boundary).ToList(), "l0ArchiWall", 10);
            DrawUtils.ShowGeometry(dataQuery.Shearwalls.Select(x => x.Boundary).ToList(), "l0ShearWall", 10);
            DrawUtils.ShowGeometry(dataQuery.Columns.Select(x => x.Boundary).ToList(), "l0Column", 3);
            DrawUtils.ShowGeometry(dataQuery.LayoutArea.Select(x => x.Boundary).ToList(), "l0PlaceCoverage", 200);
            DrawUtils.ShowGeometry(dataQuery.Holes.Select(x => x.Boundary).ToList(), "l0hole", 140);
            DrawUtils.ShowGeometry(dataQuery.DetectArea.Select(x => x.Boundary).ToList(), "l0DetectArea", 96);
            beam.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0beam", 127));

            //洞,必须先做找到框线
            dataQuery.AnalysisHoles();
            dataQuery.ClassifyData();
            var roomType = ThFaSmokeRoomTypeService.GetSmokeSensorType(dataQuery.Rooms, dataQuery.RoomFrameDict);

            foreach (var frame in dataQuery.FrameList)
            {
                var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, floorHight, theta, layoutType);//to do...frame.area need to remove hole's area
                var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(dataQuery.FrameLayoutList[frame], radius);
                var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, 0.025);
              
                var type = bIsAisleArea == true ? "centerline" : "grid";
                var centPt = frame.GetCentroidPoint();
                DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0analysisHole"), 190);
                DrawUtils.ShowGeometry(new Point3d(centPt.X, centPt.Y - 350 * 0, 0), string.Format("r:{0} aisle type:{1}", radius, type), "l4lastInfo", 3, 25, 200);
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

    }
}
