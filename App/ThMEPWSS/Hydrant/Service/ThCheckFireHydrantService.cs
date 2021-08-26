#if (ACAD2016 || ACAD2018)
using CLI;
using System;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Data;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThCADExtension;
using ThCADCore.NTS;
using NFox.Cad;
using Catel.Collections;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThCheckFireHydrantService : ICheck
    {
        public List<ThIfcRoom> Rooms { get; set; }
        public List<Tuple<Entity, Point3d, List<Entity>>> Covers { get; set; }
        private ThFireHydrantVM FireHydrantVM { get; set; }
        private ThAILayerManager AiLayerManager { get; set; }

        public ThCheckFireHydrantService(ThFireHydrantVM fireHydrantVM)
        {
            FireHydrantVM = fireHydrantVM;
            Rooms = new List<ThIfcRoom>();
            Covers = new List<Tuple<Entity, Point3d, List<Entity>>>();
            AiLayerManager = ThHydrantExtractLayerManager.Config();
        }

        public void Check(Database db, Point3dCollection pts, string mode)
        {
            ThStopWatchService.Start();
            var extractors = FirstExtract(db, pts); //获取数据
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            Rooms = roomExtractor.Rooms; //获取房间

            var ptsList = new List<Point3dCollection> { };
            if (mode == "P")
            {
                Rooms.ForEach(r => ptsList.Add(GetRoomBounds(r)));
            }
            else
            {
                ptsList.Add(pts);
            }

            var frame = new Point3dCollection
            {
                new Point3d(double.MinValue, double.MinValue, 0),
                new Point3d(double.MaxValue, double.MinValue, 0),
                new Point3d(double.MaxValue, double.MaxValue, 0),
                new Point3d(double.MinValue, double.MaxValue, 0),
            };
            SecondExtract(db, extractors, ptsList, pts, frame);
            //Clear(extractors, frame);

            ThStopWatchService.Stop();
            ThStopWatchService.Print("提取数据耗时：");

            ThStopWatchService.ReStart();
            //过滤只连接一个房间框线的门
            var doorOpeningExtractor = extractors
                .Where(o => o is ThHydrantDoorOpeningExtractor)
                .First() as ThHydrantDoorOpeningExtractor;
            doorOpeningExtractor.FilterOuterDoors(Rooms.Select(o => o.Boundary).ToList());

            var fireHydrantExtractor = extractors.Where(o => o is ThFireHydrantExtractor).First() as ThFireHydrantExtractor;
            if (fireHydrantExtractor.FireHydrants.Count == 0)
            {
                return;
            }
            string geoContent = OutPutGeojson(extractors);
            var context = BuildHydrantParam();
            var hydrant = new ThHydrantEngineMgd();
            var regions = hydrant.Validate(geoContent, context);
            Covers = ThHydrantResultParseService.Parse(regions);
            ThStopWatchService.Stop();
            ThStopWatchService.Print("保护区域计算耗时：");
        }

        private List<ThExtractorBase> FirstExtract(Database db, Point3dCollection pts)
        {
            //提取房间和外部空间
            var extractors = new List<ThExtractorBase>()
                {
                    new ThExternalSpaceExtractor()
                    {
                        UseDb3Engine=false,
                        FilterMode = FilterMode.Cross,
                        ElementLayer=AiLayerManager.OuterBoundaryLayer,
                    },
                    new ThRoomExtractor()
                    {
                        UseDb3Engine=true,
                        FilterMode = FilterMode.Cross,
                    }
                };
            extractors.ForEach(o => o.Extract(db, pts));
            //调整不在房间内的消火栓的点位
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            return extractors;
        }

        private void SecondExtract(Database db, List<ThExtractorBase> extractors, List<Point3dCollection> ptsList, Point3dCollection pts, Point3dCollection frame)
        {
            //提取其余建筑元素
            var extractorsContainer = new List<ThExtractorBase>()
                {
                    new ThHydrantArchitectureWallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        FilterMode = FilterMode.Cross,
                        ElementLayer=AiLayerManager.ArchitectureWallLayer,
                    },
                    new ThHydrantShearwallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        FilterMode = FilterMode.Cross,
                        ElementLayer=AiLayerManager.ShearWallLayer,
                    },
                    new ThHydrantDoorOpeningExtractor()
                    {
                        UseDb3Engine=false,
                        FilterMode = FilterMode.Cross,
                        ElementLayer = "AI-Door,AI-门,门",
                    },
                    new ThFireHydrantExtractor()
                    {
                        FilterMode = FilterMode.Cross,
                    }
                };
            if (FireHydrantVM.Parameter.IsThinkIsolatedColumn)
            {
                extractorsContainer.Add(new ThColumnExtractor()
                {
                    UseDb3Engine = true,
                    IsolateSwitch = true,
                    FilterMode = FilterMode.Cross,
                    ElementLayer = AiLayerManager.ColumnLayer,
                });
            }
            extractorsContainer.ForEach(e => e.Extract(db, frame));

            for (int i = 0; i < extractorsContainer.Count; ++i)
            {
                if (i == 0 && extractorsContainer[i] is ThHydrantArchitectureWallExtractor temp0)
                {
                    var temp = new List<Entity>();
                    foreach (var points in ptsList)
                    {
                        Filter(temp0, temp, points);
                    }
                    temp0.Walls = temp;
                }
                else if (i == 1 && extractorsContainer[i] is ThHydrantShearwallExtractor temp1)
                {
                    var temp = new List<Entity>();
                    foreach (var points in ptsList)
                    {
                        Filter(temp1, temp, points);
                    }
                    temp1.Walls = temp;
                }
                else if (i == 2 && extractorsContainer[i] is ThHydrantDoorOpeningExtractor temp2)
                {
                    var temp = new List<Polyline>();
                    foreach (var points in ptsList)
                    {
                        Filter(temp2, temp, points);
                    }
                    temp2.Doors = temp;
                }
                else if (i == 3 && extractorsContainer[i] is ThFireHydrantExtractor temp3)
                {
                    var temp = new List<DBPoint>();
                    foreach (var points in ptsList)
                    {
                        Filter(temp3, temp, points);
                    }
                    temp3.FireHydrants = temp;

                    var container = new List<DBPoint>();
                    temp3.Extract(db, pts);
                    Filter(temp3, container, pts);
                    temp3.FireHydrants = temp;
                }
            }

            //调整不在房间内的消火栓的点位
            extractors.AddRange(extractorsContainer);
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            var hydrantExtractor = extractors.Where(o => o is ThFireHydrantExtractor).First() as ThFireHydrantExtractor;
            hydrantExtractor.AdjustFireHydrantPosition(roomExtractor.Rooms);
        }

        private string OutPutGeojson(List<ThExtractorBase> extractors)
        {
            //用于孤立判断
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            Rooms = roomExtractor.Rooms; //获取房间

            extractors.ForEach(o => o.SetRooms(Rooms));

            //用于判断私立空间或公立空间
            IRoomPrivacy privacyCheck = new ThJudgeRoomPrivacyService();
            roomExtractor.iRoomPrivacy = privacyCheck;

            //输出Geojson
            var geos = new List<ThGeometry>();
            extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));
            return ThGeoOutput.Output(geos);
        }

        private ThProtectionContextMgd BuildHydrantParam()
        {
            return new ThProtectionContextMgd()
            {
                HydrantClearanceSampleLength = 1000.0,
                HydrantHoseLength = FireHydrantVM.Parameter.FireHoseWalkRange,
                HydrantClearanceRadius = FireHydrantVM.Parameter.SprayWaterColumnRange
            };
        }

        private List<Point3d> ContainsPts(Entity polygon, List<Point3d> pts)
        {
            return pts.Where(p => polygon.IsContains(p)).ToList();
        }

        public void Print(Database db)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(db))
            {
                int colorIndex = 1;
                Covers.ForEach(o =>
                {
                    var ents = new List<Entity>();
                    var cover = o.Item1.Clone() as Entity;
                    cover.Layer = ThCheckExpressionControlService.CheckExpressionLayer;
                    ents.Add(cover);
                    var circle = new Circle(o.Item2, Vector3d.ZAxis, 200.0)
                    {
                        Layer = ThCheckExpressionControlService.CheckExpressionLayer
                    };
                    ents.Add(circle);
                    ents.CreateGroup(acadDb.Database, (colorIndex++) % 256);
                });
            }
        }

        private Point3dCollection GetRoomBounds(ThIfcRoom room)
        {
            if (room.Boundary is Polyline polyline)
            {
                return ((Entity)polyline.Buffer(10)[0]).EntityVertices();
            }
            else if (room.Boundary is MPolygon mPolygon)
            {
                return mPolygon.Shell().EntityVertices();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void Filter(ThHydrantArchitectureWallExtractor extractor, List<Entity> temp, Point3dCollection pts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(extractor.Walls.ToCollection());
            var result = spatialIndex.SelectCrossingPolygon(pts);
            foreach (Entity e in result)
            {
                temp.Add(e);
            }
        }

        private void Filter(ThHydrantShearwallExtractor extractor, List<Entity> temp, Point3dCollection pts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(extractor.Walls.ToCollection());
            var result = spatialIndex.SelectCrossingPolygon(pts);
            foreach (Entity e in result)
            {
                temp.Add(e);
            }
        }

        private void Filter(ThDoorOpeningExtractor extractor, List<Polyline> temp, Point3dCollection pts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(extractor.Doors.ToCollection());
            var result = spatialIndex.SelectCrossingPolygon(pts);
            foreach (Polyline e in result)
            {
                temp.Add(e);
            }
        }

        private void Filter(ThFireHydrantExtractor extractor, List<DBPoint> temp, Point3dCollection pts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(extractor.FireHydrants.ToCollection());
            var result = spatialIndex.SelectCrossingPolygon(pts);
            foreach (DBPoint e in result)
            {
                temp.Add(e);
            }
        }
    }
}
#endif
