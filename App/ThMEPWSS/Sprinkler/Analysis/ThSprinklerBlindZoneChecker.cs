using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Catel.Linq;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Sprinkler.Service;
using ThMEPWSS.Uitl.ShadowIn2D;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBlindZoneChecker : ThSprinklerChecker
    {
        public override void Clean(Polyline pline)
        {
            CleanPline(ThWSSCommon.Blind_Zone_LayerName, pline, true);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            if (entity is Polyline pline)
            {
                var holes = new List<Polyline>();
                var polygon = pline.ToNTSPolygon();
                var geometriesFilter = geometries.Where(g => !((g.Properties.ContainsKey("BottomDistanceToFloor")
                                                             && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < BeamHeight)
                                                             || (g.Properties["Category"] as string).Contains("Room")))
                                                 .Select(g => g.Boundary)
                                                 .Where(g => polygon.Intersects(g.ToNTSGeometry()))
                                                 .OfType<Polyline>()
                                                 .ToCollection();
                holes = geometriesFilter.UnionPolygons().OfType<Polyline>().ToList();
                var sprinklersData = sprinklers
                    .OfType<ThSprinkler>()
                    .Where(o => o.Category == Category)
                    .Where(o => pline.Contains(o.Position))
                    .Select(o => o.Position)
                    .ToList();
                var blindZone = BlindZoneCheck(sprinklersData, pline, holes, RadiusA);
                Present(blindZone);
            }
        }

        private DBObjectCollection BlindZoneCheck(List<Point3d> sprinklersData, Polyline pline, List<Polyline> holes, double protectRange)
        {
            var protectAreas = new DBObjectCollection();
            sprinklersData.ForEach(o =>
            {
                var protectCircle = new Circle(o, Vector3d.ZAxis, protectRange)
                    .TessellateCircleWithArc(ThCADCoreNTSService.Instance.ArcTessellationLength);
                var intersectsPolys = holes.Where(x => protectCircle.LineIntersects(x)).ToList();
                var containsPolys = holes.Where(x => protectCircle.Contains(x)).ToList();

                if (intersectsPolys.Count + containsPolys.Count == 0)
                {
                    protectAreas.Add(protectCircle);
                    return;
                }
                if (intersectsPolys.Count > 0)
                {
                    protectCircle = protectCircle.Difference(intersectsPolys.ToCollection().Buffer(0.01))
                        .OfType<Polyline>()
                        .Where(y => y.Contains(o))
                        .FirstOrDefault();
                }
                if (protectCircle != null && protectCircle.Area > 10)
                {
                    var verticePolys = new List<Polyline>(containsPolys);
                    verticePolys.Add(protectCircle);

                    var vertices = new List<Point3d>();
                    verticePolys.ForEach(o =>
                    {
                        o.Vertices().OfType<Point3d>().ForEach(pt => vertices.Add(pt));
                    });
                    var shadowService = new ShadowService();
                    var usefulLines = shadowService.GetLine(o, vertices, verticePolys);
                    var allTriangle = shadowService.CalLightTriangle(usefulLines, verticePolys).ToCollection();
                    var protectArea = allTriangle.Buffer(0.01).UnionPolygons()
                        .OfType<Polyline>()
                        .OrderByDescending(pline => pline.Area)
                        .First();
                    protectAreas.Add(protectArea);
                }
            });
            return pline.DifferenceMP(protectAreas.Union(holes.ToCollection()));
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, Polyline pline)
        {
            var nodeCapacity = 5;
            var objs = sprinklers
                .OfType<ThSprinkler>()
                .Where(o => o.Category == Category)
                .Where(o => pline.Contains(o.Position))
                .Select(o => new DBPoint(o.Position))
                .ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var results = new HashSet<Line>();
            var num = spatialIndex.SelectAll().Count;
            if (num <= 1)
            {
                return results;
            }
            else if (num > 1 && num < 5)
            {
                nodeCapacity = num;
            }
            objs.OfType<DBPoint>().ForEach(o =>
            {
                var points = spatialIndex.NearestNeighbours(o.Position, nodeCapacity)
                                         .OfType<DBPoint>()
                                         .Select(o => o.Position)
                                         .ToList();
                points.ForEach(p =>
                {
                    if (p.DistanceTo(o.Position) > 2 * RadiusB)
                    {
                        results.Add(new Line(o.Position, p));
                    }
                });
            });
            return results;
        }

        private HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines, Polyline pline)
        {
            var polygon = pline.ToNTSPolygon();
            var geometriesFilter = geometries.Where(g => !((g.Properties.ContainsKey("BottomDistanceToFloor")
                                                         && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < BeamHeight)
                                                         || (g.Properties["Category"] as string).Contains("Room")))
                                             .Select(g => g.Boundary)
                                             .Where(g => polygon.Intersects(g.ToNTSGeometry()))
                                             .ToCollection();
            var result = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            lines.ForEach(o =>
            {
                var filter = spatialIndex.SelectCrossingPolygon(o.Buffer(1.0));
                if (filter.Count == 0)
                {
                    result.Add(o);
                }
            });
            return result;
        }

        private void Present(DBObjectCollection blindZone)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerBlindZoneCheckerLayer();
                blindZone.OfType<Entity>().ForEach(o =>
                {
                    if(o is Polyline polyline)
                    {
                        if (polyline.Area < 100 || polyline.Area < 5 * polyline.Length) 
                        {
                            return;
                        }
                    }
                    o.ColorIndex = 1;
                    o.LayerId = layerId;
                    acadDatabase.ModelSpace.Add(o);
                });
            }
        }

        public override void Extract(Database database, Polyline pline)
        {
            //
        }
    }
}
