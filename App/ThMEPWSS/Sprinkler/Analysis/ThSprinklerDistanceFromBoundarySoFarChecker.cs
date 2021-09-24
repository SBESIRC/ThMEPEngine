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
    public class ThSprinklerDistanceFromBoundarySoFarChecker
    {
        public List<List<Point3d>> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, int radiusA, int radiusB)
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

            var result = new List<List<Point3d>>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            sprinklers
                .Cast<ThSprinkler>()
                .Where(o => o.Category != "侧喷")
                .ForEach(o =>
                {
                    var circle = new Circle(o.Position, Vector3d.ZAxis, radiusA);
                    var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(20.0 * Math.PI));
                    if (filter.Count == 0) 
                    {
                        return;
                    }
                    else if (filter.Count > 0)
                    {
                        var points = new List<Point3d>();
                        double minDistance = radiusA;
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

                        if (minDistance > radiusB) 
                        {
                            result.Add(new List<Point3d> { o.Position, closestPoint });
                        }
                    }
                });
            return result;
        }

        public void Present(Database database, List<List<Point3d>> result)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.CreateAISprinklerDistanceFormBoundarySoFarCheckerLayer();
                var style = "TH-DIM100-W";
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, acadDatabase.Database);
                result.ForEach(o =>
                {
                    var alignedDimension = new AlignedDimension
                    {
                        XLine1Point = o[0],
                        XLine2Point = o[1],
                        DimensionText = "",
                        DimLinePoint = ThSprinklerUtils.VerticalPoint(o[0], o[1], 2000.0),
                        ColorIndex = 256,
                        DimensionStyle = id,
                        LayerId = layerId,
                        Linetype = "ByLayer"
                    };

                    acadDatabase.ModelSpace.Add(alignedDimension);
                });
            }
        }
    }
}
