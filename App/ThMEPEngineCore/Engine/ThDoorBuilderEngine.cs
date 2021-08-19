using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

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
        public override List<ThIfcBuildingElement> Build(Database db, Point3dCollection pts)
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
            return doorRecognize.Elements;
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
