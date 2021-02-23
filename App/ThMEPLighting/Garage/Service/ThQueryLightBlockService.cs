using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryLightBlockService
    {
        private string Layer { get; set; }
        private Polyline Region { get; set; }
        public ThCADCoreNTSSpatialIndex PrimarySpatialIndex { get; set; }
        public ThCADCoreNTSSpatialIndex SecondarySpatialIndex { get; set; }
        private ThQueryLightBlockService(Polyline region, string layer)
        {
            Layer = layer;
            Region = region;
        }
        public static ThQueryLightBlockService Create(Polyline region, string layer)
        {
            var instance = new ThQueryLightBlockService(region, layer);
            instance.Create();
            return instance;
        }
        private void Create()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var blks = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => b.Layer == Layer);
                PrimarySpatialIndex = new ThCADCoreNTSSpatialIndex(blks.ToCollection());
                SecondarySpatialIndex = new ThCADCoreNTSSpatialIndex(PrimarySpatialIndex.SelectCrossingPolygon(Region));
            }
        }
        public List<Point3d> Query(Line edge, double width = 1.0)
        {
            var outline = ThDrawTool.ToOutline(edge.StartPoint, edge.EndPoint, width);
            var blocks = SecondarySpatialIndex
                .SelectCrossingPolygon(outline)
                .Cast<BlockReference>().ToList();
            return Filter(blocks, edge, width);
        }
        private List<Point3d> Filter(List<BlockReference> blocks, Line edge, double tolerance = 1.0)
        {
            var results = new List<Point3d>();
            blocks.ForEach(o =>
                {
                    var projectionPt = ThGeometryTool.GetProjectPtOnLine(o.Position, edge.StartPoint, edge.EndPoint);
                    if (projectionPt.DistanceTo(o.Position) <= tolerance)
                    {
                        if (ThGeometryTool.IsPointOnLine(edge.StartPoint, edge.EndPoint, projectionPt))
                        {
                            results.Add(projectionPt);
                        }
                    }
                });
            return results;
        }
    }
}
