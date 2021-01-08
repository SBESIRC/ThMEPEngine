using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Business.UserInteraction;
using ThCADExtension;
using ThMEPLighting.ParkingStall.CAD;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Worker.ParkingGroup;
using ThMEPLighting.ParkingStall.Worker.PlaceLight;
using ThMEPLighting.ParkingStall.Business.Block;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Worker.LightAdjustor;

namespace ThMEPLighting.ParkingStall.Core
{
    public class CommandManager
    {
        public void ExtractParkStallProfiles()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);
                DrawUtils.DrawProfileDebug(selectRelatedParkProfiles.Polylines2Curves(), "parkingStall");
            }
        }

        public void GenerateParkGroup()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);

                // 去除内部的车位信息
                var validParkProfiles = GenerateValidParkPolys.MakeValidParkPolylines(selectRelatedParkProfiles, polygonInfo.InnerProfiles);
                // 分组车位信息处理
                var parkingRelatedGroups = ParkingGroupGenerator.MakeParkingGroupGenerator(validParkProfiles);
                DrawUtils.DrawGroup(parkingRelatedGroups);
            }
        }

        public void GenerateGroupLight()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);

                // 去除内部的车位信息
                var validParkProfiles = GenerateValidParkPolys.MakeValidParkPolylines(selectRelatedParkProfiles, polygonInfo.InnerProfiles);
                // 分组车位信息处理
                var parkingRelatedGroups = ParkingGroupGenerator.MakeParkingGroupGenerator(validParkProfiles);

                // 车位分组布置灯信息
                var groupLights = ParkingGroupPlaceLightGenerator.MakeParkingPlaceLightGenerator(parkingRelatedGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(groupLights, Light_Place_Type.LONG_EDGE);
                BlockInsertor.MakeBlockInsert(groupLights);
                //GroupLightViewer.MakeGroupLightViewer(groupLights);
            }
        }

        public List<Polyline> GenerateSrcLaneInfo()
        {
            var resPolys = new List<Polyline>();
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return resPolys;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();

                var curves = GetLanes(polygonInfo.ExternalProfile);
                var lines = new List<Line>();
                curves.ForEach(e =>
                {
                    if (e is Line line)
                    {
                        lines.Add(line);
                    }
                });

                curves.ForEach(e =>
               {
                   if (e is Polyline poly)
                   {
                       var polyCurves = GeomUtils.Polyline2Curves(poly,false);
                       polyCurves.ForEach(pe =>
                      {
                          if (pe is Line cline)
                              lines.Add(cline);
                      });
                   }
               });

                //var draWCurves = new List<Curve>();
                //lines.ForEach(e => draWCurves.Add(e));
                //DrawUtils.DrawProfileDebug(draWCurves, "draWCurves");
                var parkingLineService = new ParkingLinesService();
                List<List<Line>> tempData;
                var linesLst = parkingLineService.CreateNodedParkingLines(polygonInfo.ExternalProfile, lines, out tempData);

                //linesLst.AddRange(tempData);
                //var draWCurves = new List<Curve>();
                //linesLst.SelectMany(x => x).ForEach(e =>
                //{
                //    draWCurves.Add(e);
                //    });
                //DrawUtils.DrawProfileDebug(draWCurves, "draWCurves");


                var horiPolys = Lines2Polyline(linesLst);
                DrawUtils.DrawProfileDebug(horiPolys.Polylines2Curves(), "lanePolys");

                resPolys.AddRange(horiPolys);

                var veriPolys = Lines2Polyline(tempData);
                DrawUtils.DrawProfileDebug(veriPolys.Polylines2Curves(), "lanePolys");
                resPolys.AddRange(veriPolys);
            }

            return resPolys;
        }

        private List<Polyline> Lines2Polyline(List<List<Line>> linesLst)
        {
            var polys = new List<Polyline>();
            foreach (var lines in linesLst)
            {
                var dbObjectCollection = new DBObjectCollection();
                foreach (var line in lines)
                {
                    dbObjectCollection.Add(line);
                }

                foreach (DBObject entity in dbObjectCollection.LineMerge())
                {
                    if (entity is Polyline poly)
                        polys.Add(poly);
                }
            }

            return polys;
        }

        public List<Polyline> ExtendLaneInfo()
        {
            var resPolys = new List<Polyline>();
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return resPolys;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                var lines = new List<Line>();
                curves.ForEach(e =>
                {
                    if (e is Line line)
                    {
                        lines.Add(line);
                    }
                });

                curves.ForEach(e =>
                {
                    if (e is Polyline poly)
                    {
                        var polyCurves = GeomUtils.Polyline2Curves(poly, false);
                        polyCurves.ForEach(pe =>
                        {
                            if (pe is Line cline)
                                lines.Add(cline);
                        });
                    }
                });

                var lanePolys = new List<Polyline>();
                var parkingLineService = new ParkingLinesService();
                List<List<Line>> tempData;
                var linesLst = parkingLineService.CreateNodedParkingLines(polygonInfo.ExternalProfile, lines, out tempData);

                var horiPolys = Lines2Polyline(linesLst);
                lanePolys.AddRange(horiPolys);
                var veriPolys = Lines2Polyline(tempData);
                lanePolys.AddRange(veriPolys);

                var extendPolys = LaneCentralLineGenerator.MakeLaneCentralPolys(lanePolys, polygonInfo, ParkingStallCommon.LaneLineExtendLength);
                DrawUtils.DrawProfileDebug(extendPolys.Polylines2Curves(), "extendPolys");
            }

            return resPolys;
        }

        public List<Curve> GetLanes(Polyline polyline)
        {
            using (var acdb = AcadDatabase.Active())
            {
                var objs = new DBObjectCollection();
                var laneLines = acdb.ModelSpace
                    .OfType<Curve>()
                    .Where(o => o.Layer == ParkingStallCommon.LANELINE_LAYER_NAME);
                laneLines.ForEach(x => objs.Add(x));

                //var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

                return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();
            }
        }

        public void GenerateLaneGroup()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                var lines = new List<Line>();
                curves.ForEach(e =>
                {
                    if (e is Line line)
                    {
                        lines.Add(line);
                    }
                });

                curves.ForEach(e =>
                {
                    if (e is Polyline poly)
                    {
                        var polyCurves = GeomUtils.Polyline2Curves(poly, false);
                        polyCurves.ForEach(pe =>
                        {
                            if (pe is Line cline)
                                lines.Add(cline);
                        });
                    }
                });

                var lanePolys = new List<Polyline>();
                var parkingLineService = new ParkingLinesService();
                List<List<Line>> tempData;
                var linesLst = parkingLineService.CreateNodedParkingLines(polygonInfo.ExternalProfile, lines, out tempData);

                var horiPolys = Lines2Polyline(linesLst);
                lanePolys.AddRange(horiPolys);
                var veriPolys = Lines2Polyline(tempData);
                lanePolys.AddRange(veriPolys);

                // 延长的车位线
                var extendPolys = LaneCentralLineGenerator.MakeLaneCentralPolys(lanePolys, polygonInfo, ParkingStallCommon.LaneLineExtendLength);

                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);

                // 去除内部的车位信息
                var validParkProfiles = GenerateValidParkPolys.MakeValidParkPolylines(selectRelatedParkProfiles, polygonInfo.InnerProfiles);
                // 分组车位信息处理
                var parkingRelatedGroups = ParkingGroupGenerator.MakeParkingGroupGenerator(validParkProfiles);

                // 车位分组布置灯信息
                var groupLights = ParkingGroupPlaceLightGenerator.MakeParkingPlaceLightGenerator(parkingRelatedGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(groupLights, Light_Place_Type.LONG_EDGE);

                // 根据车道线信息编组
                LaneGroupCalculator.MakeLaneGroupCalculator(groupLights, extendPolys);
            }
        }

        public void LaneSubGroupOptimization()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                var lines = new List<Line>();
                curves.ForEach(e =>
                {
                    if (e is Line line)
                    {
                        lines.Add(line);
                    }
                });

                curves.ForEach(e =>
                {
                    if (e is Polyline poly)
                    {
                        var polyCurves = GeomUtils.Polyline2Curves(poly, false);
                        polyCurves.ForEach(pe =>
                        {
                            if (pe is Line cline)
                                lines.Add(cline);
                        });
                    }
                });

                var lanePolys = new List<Polyline>();
                var parkingLineService = new ParkingLinesService();
                List<List<Line>> tempData;
                var linesLst = parkingLineService.CreateNodedParkingLines(polygonInfo.ExternalProfile, lines, out tempData);

                var horiPolys = Lines2Polyline(linesLst);
                lanePolys.AddRange(horiPolys);
                var veriPolys = Lines2Polyline(tempData);
                lanePolys.AddRange(veriPolys);

                // 延长的车位线
                var extendPolys = LaneCentralLineGenerator.MakeLaneCentralPolys(lanePolys, polygonInfo, ParkingStallCommon.LaneLineExtendLength);

                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);

                // 去除内部的车位信息
                var validParkProfiles = GenerateValidParkPolys.MakeValidParkPolylines(selectRelatedParkProfiles, polygonInfo.InnerProfiles);
                // 分组车位信息处理
                var parkingRelatedGroups = ParkingGroupGenerator.MakeParkingGroupGenerator(validParkProfiles);

                // 车位分组布置灯信息
                var groupLights = ParkingGroupPlaceLightGenerator.MakeParkingPlaceLightGenerator(parkingRelatedGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(groupLights, Light_Place_Type.LONG_EDGE);

                // 根据车道线信息编组
                var laneGroups = LaneGroupCalculator.MakeLaneGroupCalculator(groupLights, extendPolys);

                // 子分组点位调整
                SubGroupPosOptimization.MakeSubGroupPosOptimization(laneGroups);

                var optimzeLightPlaceInfos = LightPlaceInfoExtractor.MakeLightPlaceInfoExtractor(laneGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(optimzeLightPlaceInfos, Light_Place_Type.LONG_EDGE);
                BlockInsertor.MakeBlockInsert(optimzeLightPlaceInfos);
            }
        }
    }
}
