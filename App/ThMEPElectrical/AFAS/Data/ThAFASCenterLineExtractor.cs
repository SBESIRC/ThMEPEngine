using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASCenterLineExtractor : ThExtractorBase,ITransformer
    {
        public DBObjectCollection CenterLines { get; private set; }
        private List<ThGeometry> Geos { get; set; } // 返回制造的Geometry数据
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        public ThAFASCenterLineExtractor()
        {
            TesslateLength = 1000.0;
            Geos = new List<ThGeometry>();
            CenterLines = new DBObjectCollection();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            return Geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            // 获取中心线
            var polys = GetPolylines(database, pts);
            polys = Tesslate(polys,TesslateLength);
            var lines = GetLines(database, pts);

            // 过滤很短的线
            polys = polys.Where(p => p.Length >= 1.0).ToList();
            lines = lines.Where(l => l.Length >= 1.0).ToList();

            // 移到原点
            polys.ForEach(o => Transformer.Transform(o));
            lines.ForEach(o => Transformer.Transform(o));

            CenterLines = CenterLines.Union(polys.ToCollection());
            CenterLines = CenterLines.Union(lines.ToCollection());
        }

        private List<Polyline> GetPolylines(Database database, Point3dCollection pts)
        {
            var polyService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            polyService.Extract(database, pts);
            return polyService.Polys;
        }

        private List<Line> GetLines(Database database, Point3dCollection pts)
        {
            var lineService = new ThExtractLineService()
            {
                ElementLayer = this.ElementLayer,
            };
            lineService.Extract(database, pts);
            return lineService.Lines;
        }

        private List<Polyline> Tesslate(List<Polyline> polys,double length)
        {
            return polys.Select(p => p.TessellatePolylineWithArc(length)).ToList();
        }

        private DBObjectCollection Clip(Entity polygon, DBObjectCollection objs, bool inverted = false)
        {
            // Clip的结果中可能有点（DBPoint)，这里可以忽略点
            var results = new DBObjectCollection();
            if (polygon is Polyline polyline)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(polyline, objs, inverted);
            }
            else if (polygon is MPolygon mPolygon)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(mPolygon, objs, inverted);
            }
            return results.OfType<Curve>().ToCollection();
        }

        public void Transform()
        {
            Transformer.Transform(CenterLines);
        }

        public void Reset()
        {
            Transformer.Reset(CenterLines);
        }
        public void Set(List<ThGeometry> geos)
        {
            this.Geos= geos;
        }
    }

    public class ThAFASCenterLineGeoFactory
    {
        public List<ThGeometry> Geos { get; private set; }
        private string Category { get; set; }  = ""; 
        public DBObjectCollection Centerlines { get; set; }
        public Dictionary<Entity, string> FireApartIds { get; set; } //外部传入

        public ThAFASCenterLineGeoFactory(DBObjectCollection centerlines)
        {
            Centerlines = centerlines;
            Category = BuiltInCategory.CenterLine.ToString();
        }

        public void Produce()
        {
            Geos = new List<ThGeometry>();
            var dict = Cut();
            dict.ForEach(o =>
            {
                var parentId = FireApartIds[o.Key];
                o.Value.OfType<Entity>().ForEach(e =>
                {
                    var geometry = new ThGeometry();
                    geometry.Boundary = e;
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                    geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "中心线");
                    geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                    Geos.Add(geometry);
                });
            });
        }

        private Dictionary<Entity,DBObjectCollection> Cut()
        {
            var results = new Dictionary<Entity,DBObjectCollection>();
            FireApartIds.ForEach(o =>
            {
                var objs = Clip(o.Key, Centerlines);
                results.Add(o.Key, objs);
            });
            return results;
        }

        private DBObjectCollection Clip(Entity polygon, DBObjectCollection objs, bool inverted = false)
        {
            // Clip的结果中可能有点（DBPoint)，这里可以忽略点
            var results = new DBObjectCollection();
            if (polygon is Polyline polyline)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(polyline, objs, inverted);
            }
            else if (polygon is MPolygon mPolygon)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(mPolygon, objs, inverted);
            }
            return results.OfType<Curve>().ToCollection();
        }
    }
}
