using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThDoorBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThDoorBuilderEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var doorExtractor = new ThDB3DoorExtractionEngine();
            doorExtractor.Extract(db);            
            return doorExtractor.Results;
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            var doorRecognition = new ThDB3DoorRecognitionEngine();
            doorRecognition.Recognize(datas, pts);
            Elements.AddRange(doorRecognition.Elements);
        }
        public override void Build(Database db, Point3dCollection pts)
        {
            // 提取
            var rawElements = Extract(db);

            // 移动原点
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            rawElements.ForEach(o =>
            {
                transformer.Transform(o.Geometry);
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
            });
            var newPts = pts.OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();
            CreateDoorNeibourSpatialIndex(db, pts, transformer);

            // 识别
            Recognize(rawElements, newPts);

            // 还原
            Elements.ForEach(o=>transformer.Reset(o.Outline));
        }
        private void CreateDoorNeibourSpatialIndex(Database db, Point3dCollection pts, ThMEPOriginTransformer transformer)
        {
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
            ThSpatialIndexCacheService.Instance.Build(db, pts);
        }
    }
}
