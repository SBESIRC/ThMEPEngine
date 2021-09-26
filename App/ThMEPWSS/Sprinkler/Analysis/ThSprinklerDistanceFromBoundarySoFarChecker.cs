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
    public class ThSprinklerDistanceFromBoundarySoFarChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
        {
            var results = DistanceCheck(sprinklers, geometries);
            Present(results);
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
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
                        filter.Cast<Entity>().ForEach(e =>
                        {
                            var objs = new DBObjectCollection();
                            e.Explode(objs);
                            objs.Cast<Curve>().ForEach(curve =>
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
                            result.Add(new Line(o.Position, closestPoint));
                        }
                    }
                });
            return result;
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
