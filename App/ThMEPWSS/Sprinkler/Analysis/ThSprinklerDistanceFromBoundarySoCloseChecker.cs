using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Catel.Collections;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceFromBoundarySoCloseChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var results = DistanceCheck(sprinklers, geometries, pline);
            Present(results);
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var geometriesFilter = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if ((g.Properties["Category"] as string).Contains("Room")
                 || (g.Properties["Category"] as string).Contains("Beam"))
                {
                    return;
                }
                geometriesFilter.Add(g.Boundary);
            });

            var result = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            sprinklers
                .Cast<ThSprinkler>()
                .Where(o => o.Category == Category)
                .Where(o => pline.Contains(o.Position))
                .ForEach(o =>
                {
                    var circle = new Circle(o.Position, Vector3d.ZAxis, 100.0);
                    var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(10.0 * Math.PI));
                    if (filter.Count > 0)
                    {
                        var points = new List<Point3d>();
                        filter.Cast<Entity>().ForEach(e =>
                        {
                            var objs = new DBObjectCollection();
                            e.Explode(objs);
                            objs.Cast<Curve>().ForEach(curve =>
                            {
                                var closePoint = curve.GetClosestPointTo(o.Position, false);
                                if (closePoint.DistanceTo(o.Position) < 100.0)
                                {
                                    points.Add(closePoint);
                                }
                            });
                        });

                        var indexs = new List<int>();
                        for (int i = 0; i < points.Count(); i++)
                        {
                            for (int j = i + 1; j < points.Count(); j++)
                            {
                                if (points[i].DistanceTo(points[j]) < 1.0)
                                {
                                    indexs.Add(j);
                                }
                            }
                            if (!indexs.Contains(i))
                            {
                                result.Add(new Line(o.Position, points[i]));
                            }
                        }
                    }

                });
            return result;
        }

        private void Present(HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerDistanceFormBoundarySoCloseCheckerLayer();
                Present(result, layerId);
            }
        }
    }
}
