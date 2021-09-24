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
        public void Dispose()
        {
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            throw new NotSupportedException();
        }

        public override List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            throw new NotSupportedException();           
        }
        public override void Build(Database db, Point3dCollection pts)
        {
            var rawelement = new List<ThRawIfcBuildingElementData>();
            var doorExtractor = new ThDB3DoorExtractionEngine();
            doorExtractor.Extract(db);
            doorExtractor.Results.ForEach(e => rawelement.Add(new ThRawIfcBuildingElementData()
            {
                Geometry = e.Geometry,
                Source = DataSource.DB3
            }));

            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            rawelement.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts.OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();
            CreateDoorNeibourSpatialIndex(db, pts, transformer);

            var doorRecognize = new ThDB3DoorRecognitionEngine();
            doorRecognize.Recognize(rawelement, newPts);
            Elements = doorRecognize.Elements;
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
