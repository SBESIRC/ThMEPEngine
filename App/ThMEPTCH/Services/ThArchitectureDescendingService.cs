using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPTCH.CAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPTCH.Services
{
    public class ThArchitectureDescendingService
    {
        public static void Handle(ThTCHSlabData architectureSlab)
        {
            var dictionary = new Dictionary<Polyline, ThTCHDescendingData>();
            architectureSlab.Descendings.ForEach(o =>
            {
                var outlineGeometry = GetPolyline(o.OutlineBuffer.ToPolyline().Buffer(10.0));
                dictionary.Add(outlineGeometry, o);
            });

            var geometries = dictionary.Select(o => o.Key).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometries);

            dictionary.ForEach(o =>
            {
                var filter = spatialIndex.SelectCrossingPolygon(o.Key);
                if (filter.Count == 2)
                {
                    // 默认为深度较浅的降板
                    var first = architectureSlab.Descendings.Where(d => d.Equals(dictionary[filter[0] as Polyline])).First();
                    var second = architectureSlab.Descendings.Where(d => d.Equals(dictionary[filter[1] as Polyline])).First();
                    if (first.DescendingHeight > second.DescendingHeight)
                    {
                        var temp = first.Clone();
                        first = second;
                        second = temp;
                    }

                    var firstOutline = first.Outline.ToPolyline();
                    var secondOutline = second.Outline.ToPolyline();
                    var vertices = firstOutline.Vertices();
                    var newVertices = new Point3dCollection();
                    vertices.OfType<Point3d>().ForEach(pt =>
                    {
                        if (secondOutline.Distance(pt) < first.DescendingWrapThickness + second.DescendingWrapThickness + 10.0)
                        {
                            newVertices.Add(secondOutline.GetClosePoint(pt));
                        }
                        else
                        {
                            newVertices.Add(pt);
                        }
                    });
                    first.Outline = newVertices.CreatePolyline().ToTCHPolyline();

                    spatialIndex.Update(new DBObjectCollection(), filter);
                }
                else if (filter.Count > 2)
                {
                    //暂不处理
                }
            });
        }

        private static Polyline GetPolyline(DBObjectCollection coll)
        {
            return coll.OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
        }
    }
}
