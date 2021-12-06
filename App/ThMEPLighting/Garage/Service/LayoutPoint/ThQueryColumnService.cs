using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThQueryColumnService
    {
        private List<ThIfcColumn> Columns { get; set; }
        private ThMEPOriginTransformer Transformer { get; set; }
        public ThQueryColumnService(Database database)
        {
            Extract(database);
            GetTransformer();
        }
        private void Extract(Database database)
        {
            var columnBuilder = new ThColumnBuilderEngine();
            columnBuilder.Build(database, new Point3dCollection());
            Columns = columnBuilder.Elements.Cast<ThIfcColumn>().ToList();
        }
        private void GetTransformer()
        {
            Transformer = new ThMEPOriginTransformer(Columns.Select(o => o.Outline).ToCollection());
        }
        public List<ThIfcColumn> SelectCrossPolygon(Entity polygon)
        {
            var results = new List<ThIfcColumn>();
            var clone = polygon.Clone() as Entity;
            Transformer.Transform(clone);
            Columns.ForEach(b => Transformer.Transform(b.Outline));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Columns.Select(o=>o.Outline).ToCollection());
            var objs = spatialIndex.SelectCrossingPolygon(clone);
            results = Columns.Where(o => objs.Contains(o.Outline)).ToList();
            Columns.ForEach(b => Transformer.Reset(b.Outline));
            return results;
        }
    }
}
