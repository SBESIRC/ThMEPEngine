using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Catel.Collections;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceFromBoundarySoFarChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var results = DistanceCheck(sprinklers, geometries, pline);
            if (results.Count > 0)
            {
                Present(results);
            }
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var polygon = pline.ToNTSPolygon();
            var geometriesFilter = geometries.Where(g =>
            {
                var category = g.Properties["Category"] as string;
                if (category.Contains("Beam"))
                {
                    return false;
                }
                else if (category.Contains("Column"))
                {
                    if(pline.IsFullContains(g.Boundary))
                        return false;
                }
                if (!polygon.Intersects(g.Boundary.ToNTSGeometry()))
                {
                    return false;
                }
                return true;
            }).Select(g => g.Boundary).ToCollection();
            var results = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            sprinklers
                .OfType<ThSprinkler>()
                .Where(o => o.Category == Category)
                .Where(o => pline.Contains(o.Position))
                .ForEach(o =>
                {
                    var circle = new Circle(o.Position, Vector3d.ZAxis, RadiusA);
                    var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(10.0 * Math.PI));
                    if (filter.Count == 0)
                    {
                        return;
                    }
                    else if (filter.Count > 0)
                    {
                        var points = new List<Point3d>();
                        double minDistance = RadiusA;
                        var closestPoint = new Point3d();
                        filter.OfType<Entity>().ForEach(e =>
                        {
                            var objs = new DBObjectCollection();
                            e.Explode(objs);
                            objs.OfType<Curve>().ForEach(curve =>
                            {
                                var closePoint = curve.GetClosestPointTo(o.Position, false);
                                var distance = closePoint.DistanceTo(o.Position);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    closestPoint = closePoint;
                                }
                            });
                        });

                        if (minDistance > RadiusB)
                        {
                            results.Add(new Line(o.Position, closestPoint));
                        }
                    }
                });
            return results;
        }

        private void Present(HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerDistanceFormBoundarySoFarCheckerLayer();
                Present(result, layerId);
            }
        }
    }
}
