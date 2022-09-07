using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        public static Polygon CalBoundary(Polygon Boundary,List<Polygon> Pillars,List<Lane> IniLanes,List<Polygon> Obstacles,
            List<InfoCar> Cars)
        {
            double buffer_tol = 10000;
            var caledBound = Boundary.Clone();

            var polys = new List<Polygon>();
            polys.AddRange(Pillars);
            polys.AddRange(IniLanes.Select(e =>
            {
                if (Boundary.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) < DisLaneWidth / 2)
                    return PolyFromLines(e.Line, e.Line.Translation(e.Vec.Normalize() * DisLaneWidth / 2));
                else
                    return e.Line.Buffer(DisLaneWidth / 2);
            }));
            polys.AddRange(Obstacles);
            polys.AddRange(Cars.Where(e => e.CarLayoutMode != 1).Select(e =>
                  ConvertVertCarToCollisionCar(e.Polyline.GetEdges().OrderBy(edge => edge.ClosestPoint(e.Point).Distance(e.Point)).First(), e.Vector)));
            polys.AddRange(Cars.Where(e => e.CarLayoutMode == 1).Select(e => e.Polyline));
            polys = polys.Select(e =>
             {
                 var g = e.BufferPL(buffer_tol);
                 if (g is Polygon)
                 {
                     var shell = ((Polygon)g).Shell;
                     return new Polygon(shell);
                 }
                 return new Polygon(new LinearRing(new Coordinate[0]));
             }).Where(e => e.Length > 0).ToList();
            var unions = OverlayNGRobust.Union(polys);
            caledBound = CalBoundFromUnioningGeos(unions, buffer_tol);
            return caledBound;
        }

        public static Polygon CalIntegralBound(List<Polygon> pillars, List<LineSegment> lanes, List<Polygon> obstacles, List<InfoCar> cars)
        {
            double buffer_tol = 10000;
            var bound = new Polygon(new LinearRing(new Coordinate[0]));

            var polys = new List<Polygon>();
            polys.AddRange(pillars);
            ModifyLanes(ref lanes, cars.Select(e => e.Polyline).ToList());
            polys.AddRange(lanes.Select(e =>
            {
                //if (Boundary.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) < DisLaneWidth / 2)
                //    return PolyFromLines(e.Line, e.Line.Translation(e.Vec.Normalize() * DisLaneWidth / 2));
                //else
                return e.Buffer(DisLaneWidth / 2);
            }));
            polys.AddRange(obstacles);
            polys.AddRange(cars.Where(e => e.CarLayoutMode != 1).Select(e =>
                  ConvertVertCarToCollisionCar(e.Polyline.GetEdges().OrderBy(edge => edge.ClosestPoint(e.Point).Distance(e.Point)).First(), e.Vector)));
            polys.AddRange(cars.Where(e => e.CarLayoutMode == 1).Select(e => e.Polyline));
            polys = polys.Select(e =>
            {
                var g = e.BufferPL(buffer_tol);
                if (g is Polygon)
                {
                    var shell = ((Polygon)g).Shell;
                    return new Polygon(shell);
                }
                return new Polygon(new LinearRing(new Coordinate[0]));
            }).Where(e => e.Length > 0).ToList();
            var unions = OverlayNGRobust.Union(polys);
            return CalBoundFromUnioningGeos(unions, buffer_tol);
        }
        static void ModifyLanes(ref List<LineSegment> lanes, List<Polygon> cars)
        {
            var carSpacialIndex=new MNTSSpatialIndex(cars);
            var pro = new MParkingPartitionPro();
            for (int i = 0; i < lanes.Count; i++)
            {
                var lane=lanes[i];
                if (pro.IsConnectedToLane(lane, false, lanes) && !pro.IsConnectedToLane(lane, true, lanes))
                    lanes[i] = new LineSegment(lane.P1, lane.P0);
                if (pro.IsConnectedToLane(lane, true, lanes) && !pro.IsConnectedToLane(lane, false, lanes))
                {
                    var buffer = lane.Buffer(DisLaneWidth / 2 + 500);
                    var car_crossed=carSpacialIndex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
                    var end_cars = car_crossed.OrderBy(e => e.ClosestPoint(lane.P1).Distance(lane.P1));
                    if (end_cars.Any())
                    {
                        var end_car=end_cars.First();
                        var p = end_car.Coordinates.OrderBy(t => t.Distance(lane.P1)).First();
                        var p_on_lane=lane.ClosestPoint(p);
                        if (p_on_lane.Distance(lane.P1) > 1)
                        {
                            var splits = SplitLine(lane, new List<Coordinate>() { p_on_lane });
                            lanes[i]=splits.FirstOrDefault();
                        }
                    }
                }
            }
        }
        static Polygon CalBoundFromUnioningGeos(Geometry unions, double buffer_tol)
        {
            var bound = new Polygon(new LinearRing(new Coordinate[0]));
            Polygon union;
            Polygon hole;
            if (unions is Polygon)
                union = (Polygon)unions;
            else if (unions is MultiPolygon)
                union = (Polygon)((MultiPolygon)unions).Geometries.Where(e => e is Polygon polygon).OrderByDescending(polygon => polygon.Area).First();
            else
                return bound;

            var shrinked_pl = ((Polygon)union).BufferPL(buffer_tol);
            if (shrinked_pl is Polygon)
            {
                var holes = ((Polygon)shrinked_pl).Holes.OrderByDescending(e => e.Area);
                if (holes.Any())
                {
                    hole = new Polygon(holes.First());
                    bound = hole;
                }
            }
            return bound;
        }
    }
}
