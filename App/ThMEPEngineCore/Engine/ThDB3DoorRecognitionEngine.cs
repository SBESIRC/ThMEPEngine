using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3DoorExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var doorMarkVisitor = new ThDB3DoorMarkExtractionVisitor()
            {
                LayerFilter = ThDoorMarkLayerManager.XrefLayers(database),
            };
            var doorStoneVisitor = new ThDB3DoorStoneExtractionVisitor()
            {
                LayerFilter = ThDoorStoneLayerManager.XrefLayers(database),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(doorMarkVisitor);
            extractor.Accept(doorStoneVisitor);
            extractor.Extract(database);
            Results.AddRange(doorMarkVisitor.Results);
            Results.AddRange(doorStoneVisitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ThDB3DoorRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            // 提取
            var engine = new ThDB3DoorExtractionEngine();
            engine.Extract(database);

            // 创建偏移矩阵
            var transformer = new ThMEPOriginTransformer(
                engine.Results
                .Where(o=>o is ThRawDoorStone)
                .Select(o=>o.Geometry).ToCollection());

            // 构件索引服务
            ThSpatialIndexCacheService.Instance.Add(new List<BuiltInCategory>
            {
                BuiltInCategory.ArchitectureWall,
                BuiltInCategory.Column,
                BuiltInCategory.CurtainWall,
                BuiltInCategory.ShearWall,
                BuiltInCategory.Window
            });
            ThSpatialIndexCacheService.Instance.Transformer = transformer;
            ThSpatialIndexCacheService.Instance.Build(database, polygon);

            // 移动
            var newPts = transformer.Transform(polygon);
            engine.Results.ForEach(e =>
            {
                if(e is ThRawDoorStone doorStone)
                {
                    transformer.Transform(doorStone.Geometry);
                }
                else if(e is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Geometry as Entity);
                    transformer.Transform(doorMark.Data as Entity);
                }
            });

            // 识别
            Recognize(engine.Results, newPts);

            // 还原
            Elements.ForEach(e => transformer.Reset(e.Outline));
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            // 搜集门垛
            var doorStones = GetDoorStones(datas.Where(o => o is ThRawDoorStone).ToList(), polygon);
            // 搜集门标注
            var doorMarks = GetDoorMarks(datas.Where(o => o is ThRawDoorMark).ToList(), polygon);
            // 搜集门标注范围内的门垛
            var buildService = new ThCollectDoorStoneService(
                doorStones.Select(o => o.Geometry).ToCollection(),
                doorMarks.Select(o => o.Data as Entity).ToCollection());
            buildService.Build();

            // 创建门
            var buildDoor = new ThBuildDoorService();
            buildDoor.Build(buildService.Results);
            buildDoor.Outlines.ForEach(o => Elements.Add(new ThIfcDoor { Outline = o.Item1, Spec=o.Item2 })) ;
        }
        private List<ThRawIfcBuildingElementData> GetDoorStones(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var dbObjs = datas.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(polygon);
                filterObjs = filterObjs.UnionPolygons();
                //过滤面积很小的Polygon                
                return filterObjs.Cast<Polyline>().Where(o=>o.Area>=1.0).Select(o => new ThRawIfcBuildingElementData { Geometry = o.OBB() }).ToList();                
            }
            else
            {
                var dbObjs = datas.Select(o=>o.Geometry).ToCollection();
                dbObjs = dbObjs.UnionPolygons();
                return dbObjs.Cast<Polyline>().Where(o => o.Area >= 1.0).Select(o => new ThRawIfcBuildingElementData { Geometry = o.OBB() }).ToList();
            }
        }
        private List<ThRawIfcBuildingElementData> GetDoorMarks(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                datas.ForEach(o => dbObjs.Add(o.Geometry));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(polygon);
                return datas.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                return datas;
            }
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}