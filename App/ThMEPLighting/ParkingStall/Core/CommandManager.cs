using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Business.Block;
using ThMEPLighting.ParkingStall.Business.UserInteraction;
using ThMEPLighting.ParkingStall.CAD;
using ThMEPLighting.ParkingStall.Geometry;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Worker.LightAdjustor;
using ThMEPLighting.ParkingStall.Worker.ParkingGroup;
using ThMEPLighting.ParkingStall.Worker.PipeConnector;
using ThMEPLighting.ParkingStall.Worker.PlaceLight;
using ThMEPLighting.ParkingStall.Worker.RegionLaneConnect;

namespace ThMEPLighting.ParkingStall.Core
{
    public class CommandManager
    {
        public void ExtractParkStallProfiles()
        {
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);
                var srcPolys = new List<Polyline>();

                foreach (var relatedParkProfile in selectRelatedParkProfiles)
                {
                    foreach (var entity in relatedParkProfile.Buffer(-ParkingStallCommon.ParkingPolyEnlargeLength))
                    {
                        if (entity is Polyline poly && poly.Closed)
                            srcPolys.Add(poly);
                    }
                }

                DrawUtils.DrawProfileDebug(srcPolys.Polylines2Curves(), "parkingStall");
            }
        }

        public void GenerateParkGroup()
        {
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
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
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
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
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return resPolys;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
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
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return resPolys;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
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
            
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                List<Curve> otherLanes = new List<Curve>();
                var objs = new DBObjectCollection();
                var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPLightingCommon.LANELINE_LAYER_NAME);
                laneLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    objs.Add(transCurve);
                });
                var _lanLineSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);

                var sprayLines = _lanLineSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
                otherLanes.Clear();
                if (sprayLines.Count <= 0)
                {
                    return otherLanes;
                }
                sprayLines = sprayLines.SelectMany(x => polyline.Trim(x).Cast<Entity>().ToList().Where(c => c is Curve).Cast<Curve>().ToList()).ToList();
                //sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();

                //return sprayLines;
                //处理车道线
                var handleLines = ThMEPLineExtension.LineSimplifier(sprayLines.ToCollection(), 500, 100.0, 2.0, Math.PI / 180.0);
                var parkingLinesService = new ParkingLinesService();
                var parkingLines = parkingLinesService.CreateNodedParkingLines(polyline, handleLines, out List<List<Line>> otherPLines);
                foreach (var item in parkingLines)
                {
                    if (null == item || item.Count < 1)
                        continue;
                    otherLanes.AddRange(item);
                }
                foreach (var item in otherPLines)
                {
                    if (null == item || item.Count < 1)
                        continue;
                    otherLanes.AddRange(item);
                }
                return otherLanes;
            }
            

            //using (var acdb = AcadDatabase.Active())
            //{
            //    var objs = new DBObjectCollection();
            //    var laneLines = acdb.ModelSpace
            //        .OfType<Curve>()
            //        .Where(o => o.Layer == ParkingStallCommon.LANELINE_LAYER_NAME);
            //    laneLines.ForEach(x => objs.Add(x));

            //    //var bufferPoly = polyline.Buffer(1)[0] as Polyline;
            //    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            //    var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
                
            //    //return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();
            //    return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Entity>().ToList().Where(c => c is Curve).Cast<Curve>().ToList()).ToList();
            //}
        }

        public void GenerateLaneGroup()
        {
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                if (curves == null || curves.Count < 1)
                    continue;
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

                if (groupLights == null || groupLights.Count < 1)
                    continue;
                // 根据车道线信息编组
                LaneGroupCalculator.MakeLaneGroupCalculator(groupLights, extendPolys, true);
            }
        }

        public void LaneSubGroupOptimization()
        {
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                if (null == curves || curves.Count < 1)
                    continue;
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
                var laneGroups = LaneGroupCalculator.MakeLaneGroupCalculator(groupLights, extendPolys, true);

                // 子分组点位调整
                SubGroupPosOptimization.MakeSubGroupPosOptimization(laneGroups);

                var optimzeLightPlaceInfos = LightPlaceInfoExtractor.MakeLightPlaceInfoExtractor(laneGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(optimzeLightPlaceInfos, Light_Place_Type.LONG_EDGE);
                BlockInsertor.MakeBlockInsert(optimzeLightPlaceInfos);
            }
        }

        public void SideLaneConnect()
        {
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                var srcLaneLines = new List<Line>();
                curves.ForEach(e =>
                {
                    if (e is Line line)
                    {
                        srcLaneLines.Add(line);
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
                                srcLaneLines.Add(cline);
                        });
                    }
                });

                var lanePolys = new List<Polyline>();
                var parkingLineService = new ParkingLinesService();
                List<List<Line>> tempData;
                var linesLst = parkingLineService.CreateNodedParkingLines(polygonInfo.ExternalProfile, srcLaneLines, out tempData);

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
                var laneGroups = LaneGroupCalculator.MakeLaneGroupCalculator(groupLights, extendPolys, false);

                // 子分组点位调整
                SubGroupPosOptimization.MakeSubGroupPosOptimization(laneGroups);

                //// laneGroups 里面组织结构可能会有无效的灯信息
                var optimzeLightPlaceInfos = LightPlaceInfoExtractor.MakeLightPlaceInfoExtractor(laneGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(optimzeLightPlaceInfos, Light_Place_Type.LONG_EDGE);
                BlockInsertor.MakeBlockInsert(optimzeLightPlaceInfos);

                // 连管处理
                var pipeLighterPolyInfos = LightConnector.MakeLightConnector(laneGroups, srcLaneLines, polygonInfo.InnerProfiles, Light_Place_Type.LONG_EDGE);

                // print
                LightConnectViewer.MakeLightConnectViewer(pipeLighterPolyInfos);
            }
        }

        public void THLaneConnect()
        {
            var wallPolygonInfos = EntityPicker.MakeUserPickPolys();
            if (wallPolygonInfos.Count == 0)
                return;

            //var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var curves = GetLanes(polygonInfo.ExternalProfile);
                var srcLaneLines = new List<Line>();
                curves.ForEach(e =>
                {
                    if (e is Line line)
                    {
                        srcLaneLines.Add(line);
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
                                srcLaneLines.Add(cline);
                        });
                    }
                });

                var lanePolys = new List<Polyline>();
                var parkingLineService = new ParkingLinesService();
                List<List<Line>> tempData;
                var linesLst = parkingLineService.CreateNodedParkingLines(polygonInfo.ExternalProfile, srcLaneLines, out tempData);

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
                var laneGroups = LaneGroupCalculator.MakeLaneGroupCalculator(groupLights, extendPolys, false);

                // 子分组点位调整
                SubGroupPosOptimization.MakeSubGroupPosOptimization(laneGroups);

                //// laneGroups 里面组织结构可能会有无效的灯信息
                var optimzeLightPlaceInfos = LightPlaceInfoExtractor.MakeLightPlaceInfoExtractor(laneGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(optimzeLightPlaceInfos, Light_Place_Type.LONG_EDGE);
                BlockInsertor.MakeBlockInsert(optimzeLightPlaceInfos);

                // 车道线单侧部分连管处理
                var pipeLighterPolyInfos = LightConnector.MakeLightConnector(laneGroups, srcLaneLines, polygonInfo.InnerProfiles, Light_Place_Type.LONG_EDGE);

                // 区域部分连管处理
                RegionLaneConnector.MakeRegionConnector(pipeLighterPolyInfos);
            }
        }

    }
}
