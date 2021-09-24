using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceFromBoundaryChecker
    {
        public List<List<Point3d>> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
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
                    var circle = new Circle(o.Position, Vector3d.ZAxis, 100.0);
                    var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(20.0 * Math.PI));
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
                                if(closePoint.DistanceTo(o.Position) < 100.0)
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
                                if(points[i].DistanceTo(points[j]) < 1.0)
                                {
                                    indexs.Add(j);
                                }
                            }
                            if(!indexs.Contains(i))
                            {
                                result.Add(new List<Point3d> { o.Position, points[i] });
                            }
                        }
                    }

                });
            return result;
        }

        public void Present(Database database, List<List<Point3d>> result)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.CreateAISprinklerDistanceFormBoundarySoCloseCheckerLayer();
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
