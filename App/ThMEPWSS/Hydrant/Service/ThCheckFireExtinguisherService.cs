﻿#if (ACAD2016 || ACAD2018)
using CLI;
using System;
using System.Linq;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Data;
using ThMEPWSS.Hydrant.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using NFox.Cad;
using Dreambuild.AutoCAD;
#endif

namespace ThMEPWSS.Hydrant.Service
{
#if (ACAD2016 || ACAD2018)
    public class ThCheckFireExtinguisherService : ICheck
    {
        private ThFireHydrantVM FireHydrantVM { get; set; }
        public List<ThIfcRoom> Rooms { get; set; }
        public List<Tuple<Entity, Point3d, List<Entity>>> Covers { get; set; }
        public List<string> FireExtinguisherBlkNames { get; set; }

        private ThAILayerManager AiLayerManager { get; set; }

        public ThCheckFireExtinguisherService(ThFireHydrantVM fireHydrantVM)
        {
            Rooms = new List<ThIfcRoom>();
            FireHydrantVM = fireHydrantVM;
            Covers = new List<Tuple<Entity, Point3d, List<Entity>>>();
            AiLayerManager = ThHydrantExtractLayerManager.Config();
            FireExtinguisherBlkNames = new List<string>() { "手提式灭火器", "推车式灭火器" };
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

            var frame = new Point3dCollection();
            SecondExtract(db, extractors, ptsList, frame);
            ThStopWatchService.Stop();
            ThStopWatchService.Print("提取数据耗时：");

            ThStopWatchService.ReStart();
            //过滤只连接一个房间框线的门
            var doorOpeningExtractor = extractors
                .Where(o => o is ThHydrantDoorOpeningExtractor)
                .First() as ThHydrantDoorOpeningExtractor;
            doorOpeningExtractor.FilterOuterDoors(Rooms.Select(o => o.Boundary).ToList());

            //用于判断私立空间或公立空间
            IRoomPrivacy privacyCheck = new ThJudgeRoomPrivacyService();
            roomExtractor.iRoomPrivacy = privacyCheck;

            if (mode == "P")
            {
                ptsList.Add(pts);
            }

            FireExtinguisherBlkNames.ForEach(o =>
            {
                var newExtractors = new List<ThExtractorBase>();
                extractors.ForEach(e => newExtractors.Add(e));
                var extinguisherExtractor = ExtractFireExtinguisher(db, ptsList, o, frame);
                if (extinguisherExtractor.FireExtinguishers.Count > 0)
                {
                    newExtractors.Add(extinguisherExtractor);
                    string geoContent = OutPutGeojson(newExtractors);
                    var context = BuildHydrantParam(o);
                    var hydrant = new ThHydrantEngineMgd();
                    var regions = hydrant.Validate(geoContent, context);
                    Covers = ThHydrantResultParseService.Parse(regions);
                }
            });

            ThStopWatchService.Stop();
            ThStopWatchService.Print("保护区域计算耗时：");
        }

        private ThFireExtinguisherExtractor ExtractFireExtinguisher(Database db, List<Point3dCollection> ptsList, string name, Point3dCollection frame)
        {
            var visitor = new ThFireExtinguisherExtractionVisitor()
            {
                BlkNames = new List<string> { name },
            };
            var extractor = new ThFireExtinguisherExtractor(visitor);
            extractor.Extract(db, frame);
            var temp = new List<DBPoint>();
            foreach (var pts in ptsList)
            {
                Filter(extractor, temp, pts);
            }

            var container = new List<DBPoint>();
            foreach (DBPoint e in RemoveRepeatedGeometry(temp.ToCollection()))
            {
                container.Add(e);
            }
            extractor.FireExtinguishers = container;
            return extractor;
        }

        private string OutPutGeojson(List<ThExtractorBase> extractors)
        {
            //用于孤立判断
            extractors.ForEach(o => o.SetRooms(Rooms));
            //输出Geojson
            var geos = new List<ThGeometry>();
            extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));
            return ThGeoOutput.Output(geos);
        }

        private ThProtectionContextMgd BuildHydrantParam(string fireExtinguisherName)
        {
            var context = new ThProtectionContextMgd
            {
                HydrantHoseLength = FireHydrantVM.QueryMaxProtectDistance(fireExtinguisherName),
                HydrantClearanceRadius = 0
            };
            return context;
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
                    ents.CreateGroup(acadDb.Database, colorIndex++);
                });
            }
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
            return extractors;
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

        private void SecondExtract(Database db, List<ThExtractorBase> extractors, List<Point3dCollection> ptsList, Point3dCollection frame)
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
                    var container = new List<Entity>();
                    foreach (Entity e in RemoveRepeatedGeometry(temp.ToCollection()))
                    {
                        container.Add(e);
                    }
                    temp0.Walls = container;
                }
                else if (i == 1 && extractorsContainer[i] is ThHydrantShearwallExtractor temp1)
                {
                    var temp = new List<Entity>();
                    foreach (var points in ptsList)
                    {
                        Filter(temp1, temp, points);
                    }
                    var container = new List<Entity>();
                    foreach (Entity e in RemoveRepeatedGeometry(temp.ToCollection()))
                    {
                        container.Add(e);
                    }
                    temp1.Walls = container;
                }
                else if (i == 2 && extractorsContainer[i] is ThHydrantDoorOpeningExtractor temp2)
                {
                    var temp = new List<Polyline>();
                    foreach (var points in ptsList)
                    {
                        Filter(temp2, temp, points);
                    }
                    var container = new List<Polyline>();
                    foreach (Polyline e in RemoveRepeatedGeometry(temp.ToCollection()))
                    {
                        container.Add(e);
                    }
                    temp2.Doors = container;
                }
            }
            extractors.AddRange(extractorsContainer);
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

        private void Filter(ThFireExtinguisherExtractor extractor, List<DBPoint> temp, Point3dCollection pts)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(extractor.FireExtinguishers.ToCollection());
            var result = spatialIndex.SelectCrossingPolygon(pts);
            foreach (DBPoint e in result)
            {
                temp.Add(e);
            }
        }

        private DBObjectCollection RemoveRepeatedGeometry(DBObjectCollection objs)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.Geometries.Values.ToCollection();
        }

        private List<DBPoint> RemoveRepeatedPoints(List<DBPoint> dbPoints, double tolerance = 1.0)
        {
            var kdTree = new ThCADCoreNTSKdTree(tolerance);
            dbPoints.ForEach(p => kdTree.InsertPoint(p.Position));
            return kdTree.Nodes.Keys
                .Select(k => new DBPoint(k.Coordinate.ToAcGePoint3d()))
                .ToList();
        }
#endif
    }
}
