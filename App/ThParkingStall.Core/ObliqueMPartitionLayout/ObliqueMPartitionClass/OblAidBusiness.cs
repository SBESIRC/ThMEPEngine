using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public partial class ObliqueMPartition
    {
        public LineSegment TranslateReservedConnection(LineSegment line, Vector2D vector,bool allow_extend_bound=true)
        {
            var res = line.Translation(vector);
            var nonparallellines = IniLanes.Where(e => !IsParallelLine(line, e.Line)).Select(e => e.Line);
            //start
            nonparallellines = nonparallellines.OrderBy(e => e.ClosestPoint(line.P0).Distance(line.P0));
            if (nonparallellines.Any() && nonparallellines.First().ClosestPoint(line.P0).Distance(line.P0) < 10)
            {
                var start = res.P0;
                var start_connected_line = nonparallellines.First();
                var case_out2750 = false;
                //偏移直线超出了边界2750的case
                if (!Boundary.Contains(res.MidPoint))
                {
                    case_out2750 = true;
                    start_connected_line = new LineSegment(start_connected_line);
                    start_connected_line=start_connected_line.Scale(20);
                    res = res.Translation(-vector.Normalize() * DisLaneWidth / 2);
                    start = res.P0;
                }

                res.P0 = res.P0.Translation(-Vector(res).Normalize() * MaxLength);
                var intersects = res.IntersectPoint(start_connected_line);
                if (intersects.Count() > 0)
                    res.P0 = intersects.First();
                else
                    res.P0 = start;
                //偏移直线超出了边界2750的case
                if (case_out2750)
                {
                    res = res.Translation(vector.Normalize() * DisLaneWidth / 2);
                }
            }
            else if(allow_extend_bound)
            {
                var near_wall = false;
                foreach (var wall in Walls)
                    if (wall.ClosestPoint(line.P0).Distance(line.P0) < 10) near_wall = true;
                if (near_wall)
                {
                    var start = res.P0;
                    res.P0 = res.P0.Translation(-Vector(res).Normalize() * MaxLength);
                    var intersects = res.IntersectPoint(Boundary);
                    if (intersects.Count() > 0)
                    {
                        var p_bound= intersects.OrderBy(P => P.Distance(start)).First();
                        res.P0 = p_bound;
                        //var extend_seg = new LineSegment(start, p_bound);
                        //extend_seg = extend_seg.Translation(-vector);
                        //if (Boundary.ClosestPoint(extend_seg.MidPoint).Distance(extend_seg.MidPoint) < 1 )
                        //    res.P0 = p_bound;
                        //else
                        //    res.P0 = start;
                    }
                    else
                        res.P0 = start;
                }
            }
            //end
            nonparallellines = nonparallellines.OrderBy(e => e.ClosestPoint(line.P1).Distance(line.P1));
            if (nonparallellines.Any() && nonparallellines.First().ClosestPoint(line.P1).Distance(line.P1) < 10)
            {
                var end = res.P1;
                var end_connected_line = nonparallellines.First();
                var case_out2750 = false;
                //偏移直线超出了边界2750的case
                if (!Boundary.Contains(res.MidPoint))
                {
                    case_out2750 = true;
                    end_connected_line = new LineSegment(end_connected_line);
                    end_connected_line = end_connected_line.Scale(20);
                    res = res.Translation(-vector.Normalize() * DisLaneWidth / 2);
                    end = res.P1;
                }
                res.P1 = res.P1.Translation(Vector(res).Normalize() * MaxLength);
                var intersects = res.IntersectPoint(end_connected_line);
                if (intersects.Count() > 0)
                    res.P1 = intersects.First();
                else
                    res.P1=end;
                //偏移直线超出了边界2750的case
                if (case_out2750)
                {
                    res = res.Translation(vector.Normalize() * DisLaneWidth / 2);
                }
            }
            else if(allow_extend_bound)
            {
                var near_wall = false;
                foreach (var wall in Walls)
                    if (wall.ClosestPoint(line.P1).Distance(line.P1) < 10) near_wall = true;
                if (near_wall)
                {
                    var end= res.P1;
                    res.P1 = res.P1.Translation(Vector(res).Normalize() * MaxLength);
                    var intersects = res.IntersectPoint(Boundary);
                    if (intersects.Count() > 0)
                    {
                        var p_bound = intersects.OrderBy(P => P.Distance(end)).First();
                        res.P1 = p_bound;
                        //var extend_seg = new LineSegment(end, p_bound);
                        //extend_seg = extend_seg.Translation(-vector);
                        //if (Boundary.ClosestPoint(extend_seg.MidPoint).Distance(extend_seg.MidPoint) < 1 )
                        //    res.P1 = p_bound;
                        //else
                        //    res.P1 = end;
                    }
                    else
                        res.P1 = end;
                }
            }
            return res;
        }
        public Polygon BufferReservedConnection(LineSegment line, double dis)
        {
            var vec = Vector(line).GetPerpendicularVector().Normalize();
            var a = TranslateReservedConnection(line, vec * dis);
            var b = TranslateReservedConnection(line, -vec * dis);
            var poly = new Polygon(new LinearRing(new Coordinate[] {
                a.P0,a.P1,b.P1,b.P0,a.P0}));
            return poly;
        }
        private bool CloseToWall(Coordinate point, LineSegment line)
        {
            if (Walls.Count == 0) return false;
            double tol = 10;
            var dis = ClosestPointInCurvesFast(point, Walls);
            if (dis < tol) return true;
            else return false;
        }
        public static void SortLaneByDirection(List<Lane> lanes, int mode)
        {
            var comparer = new LaneComparer(mode, DisCarAndHalfLaneBackBack);
            lanes.Sort(comparer);
        }
        private class LaneComparer : IComparer<Lane>
        {
            public LaneComparer(int mode, double filterLength)
            {
                Mode = mode;
                FilterLength = filterLength;
            }
            private int Mode;
            private double FilterLength;
            public int Compare(Lane a, Lane b)
            {
                if (Mode == 0)
                {
                    return CompareLength(a.Line, b.Line);
                }
                else return 0;
            }
            private int CompareLength(LineSegment a, LineSegment b)
            {
                if (a.Length > b.Length) return -1;
                else if (a.Length < b.Length) return 1;
                return 0;
            }
        }
        private bool IsConnectedToLane(LineSegment line)
        {
            var nonParallelLanes = IniLanes.Where(e => !IsParallelLine(e.Line, line)).Select(e => e.Line);
            if (ClosestPointInLines(line.P0, line, nonParallelLanes) < 10
                || ClosestPointInLines(line.P1, line, nonParallelLanes) < 10)
                return true;
            else return false;
        }
        private bool IsConnectedToLane(LineSegment line, bool Startpoint)
        {
            var nonParallelLanes = IniLanes.Where(e => !IsParallelLine(e.Line, line)).Select(e => e.Line);
            if (Startpoint)
            {
                if (ClosestPointInLines(line.P0, line, nonParallelLanes) < 10) return true;
                else return false;
            }
            else
            {
                if (ClosestPointInLines(line.P1, line, nonParallelLanes) < 10) return true;
                else return false;
            }
        }
        private bool IsConnectedToLane(LineSegment line, bool Startpoint, List<LineSegment> lanes)
        {
            if (Startpoint)
            {
                if (ClosestPointInLines(line.P0, line, lanes) < 10) return true;
                else return false;
            }
            else
            {
                if (ClosestPointInLines(line.P1, line, lanes.Select(e => e)) < 10) return true;
                else return false;
            }
        }
        public bool IsConnectedToLaneDouble(LineSegment line)
        {
            if (IsConnectedToLane(line, true) && IsConnectedToLane(line, false)) return true;
            else return false;
        }
        private List<LineSegment> SplitBufferLineByPoly(LineSegment line, double distance, Polygon cutter)
        {
            return SplitLine(line, cutter);
            var pl = BufferReservedConnection(line,distance);
            var splits = SplitCurve(cutter, pl);
            if (splits.Count() == 1)
            {
                return new List<LineSegment> { line };
            }
            splits = splits.Where(e => pl.Contains(e.GetMidPoint())).ToArray();
            var points = new List<Coordinate>();
            foreach (var crv in splits)
                points.AddRange(crv.Coordinates);
            points = points.Select(p => line.ClosestPoint(p)).ToList();
            return SplitLine(line, points);
        }
        private List<LineSegment> SplitLineBySpacialIndexInPoly(LineSegment line, Polygon polyline, MNTSSpatialIndex spatialIndex, bool allow_on_edge = true,bool directly=false)
        {
            var crossed = spatialIndex.SelectCrossingGeometry(polyline).Cast<Polygon>();
            crossed = crossed.Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0);
            if(directly)
                return SplitLine(line,crossed.ToList()).ToList();
            var points = new List<Coordinate>();
            foreach (var c in crossed)
            {
                points.AddRange(c.Coordinates);
                points.AddRange(c.IntersectPoint(polyline));
            }
            points = points.Where(p =>
            {
                var conda = polyline.Contains(p);
                if (!allow_on_edge) conda = conda || polyline.ClosestPoint(p).Distance(p) < 1;
                return conda;
            }).Select(p => line.ClosestPoint(p)).ToList();
            return SplitLine(line, points);
        }
        private bool HasParallelLaneForwardExisted(LineSegment line, Vector2D vec, double maxlength, double minlength, ref double dis_to_move
   , ref LineSegment prepLine, ref List<LineSegment> paras_lines)
        {
            var lperp = LineSegmentSDL(line.MidPoint.Translation(vec * 100), vec, maxlength);
            var lins = IniLanes.Where(e => IsParallelLine(line, e.Line))
                .Where(e => e.Line.IntersectPoint(lperp).Count() > 0)
                .Where(e => e.Line.Length > line.Length / 3)
                .OrderBy(e => line.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint))
                .Select(e => e.Line).ToList();
            lins.AddRange(paras_lines.Where(e => IsParallelLine(line, e))
                .Where(e => e.IntersectPoint(lperp).Count() > 0)
                .Where(e => e.Length > line.Length / 3)
                .OrderBy(e => line.ClosestPoint(e.MidPoint).Distance(e.MidPoint))
                .Select(e => e)
                );
            lins = lins.Where(e => e.ClosestPoint(line.MidPoint).Distance(line.MidPoint) > minlength).ToList();
            if (lins.Count == 0) return false;
            else
            {
                var lin = lins.First();
                var dis1 = lin.ClosestPoint(line.P0)
                    .Distance(lin.ClosestPoint(line.P0));
                var dis2 = lin.ClosestPoint(line.P1)
                    .Distance(lin.ClosestPoint(line.P1));
                if (dis1 + dis2 < line.Length / 2)
                {
                    dis_to_move = lin.ClosestPoint(line.MidPoint).Distance(line.MidPoint);
                    prepLine = lperp;
                    return true;
                }
                else return false;
            }
        }
        private static void UnifyLaneDirection(ref LineSegment lane, List<Lane> iniLanes)
        {
            var line = new LineSegment(lane);
            var lanes = iniLanes.Select(e => e.Line).Where(e => !IsParallelLine(line, e)).ToList();
            var lanestrings = lanes.Select(e => e.ToLineString()).ToList();
            if (lanes.Count > 0)
            {
                if (ClosestPointInCurves(line.P1, lanes.Select(e => e.ToLineString()).ToList()) < 1 && ClosestPointInCurves(line.P0, lanes.Select(e => e.ToLineString()).ToList()) < 1)
                {
                    //if (line.P0.X - line.P1.X > 1000) line=new LineSegment(line.P1,line.P0);
                    //else if (lanes.Count == 0 && line.P0.Y - line.P1.Y > 1000) line = new LineSegment(line.P1, line.P0);
                    var startline = lanes.Where(e => e.ClosestPoint(line.P0).Distance(line.P0) < 1).OrderByDescending(e => e.Length).First();
                    var endline = lanes.Where(e => e.ClosestPoint(line.P1).Distance(line.P1) < 1).OrderByDescending(e => e.Length).First();
                    if (endline.Length > startline.Length) line = new LineSegment(line.P1, line.P0);
                    //else
                    //{
                    //    if (line.P0.X - line.P1.X > 1000) line = new LineSegment(line.P1, line.P0);
                    //}
                }
                else if (ClosestPointInCurves(line.P1, lanestrings) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.P0, lanestrings) > DisCarAndHalfLane + 1) line = new LineSegment(line.P1, line.P0);
                else if (ClosestPointInCurves(line.P1, lanestrings) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.P0, lanestrings) < DisCarAndHalfLane + 1
                    && ClosestPointInCurves(line.P1, lanestrings) < ClosestPointInCurves(line.P0, lanestrings)) line = new LineSegment(line.P1, line.P0);
            }
            else
            {
                if (line.P0.X - line.P1.X > 1000) line = new LineSegment(line.P1, line.P0);
                else if (lanes.Count == 0 && line.P0.Y - line.P1.Y > 1000) line = new LineSegment(line.P1, line.P0);
            }
            lane = line;
        }
        public void UpdateLaneBoxAndSpatialIndexForGenerateVertLanes()
        {
            LaneSpatialIndex.Update(IniLanes.Select(e => (Geometry)PolyFromLine(e.Line)).ToList(), new List<Geometry>());
            LaneBoxes.AddRange(IniLanes.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = new LineSegment(e.Line);
                var lb = new LineSegment(e.Line);
                la = la.Translation(Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                lb = lb.Translation(-Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                var py = PolyFromLines(la, lb);
                return py;
            }));
            LaneBoxes.AddRange(CarModules.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = new LineSegment(e.Line);
                var lb = new LineSegment(e.Line);
                la = la.Translation(Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                lb = lb.Translation(-Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                var py = PolyFromLines(la, lb);
                return py;
            }));
            LaneBufferSpatialIndex.Update(LaneBoxes.Cast<Geometry>().ToList(), new List<Geometry>());
        }

        private Polygon ConvertSpecialCollisionCheckRegionForLane(LineSegment line, Vector2D vec, bool isstart = true)
        {
            var collisionD = CollisionD;
            CollisionD = 300;
            var pt = line.P0;
            var v = -Vector(line).Normalize();
            if (!isstart)
            {
                pt = line.P1;
                v = -v;
            }
            var points = new List<Coordinate>();
            pt = pt.Translation(vec.Normalize() * CollisionCT);
            points.Add(pt);
            pt = pt.Translation(v * CollisionD);
            points.Add(pt);
            pt = pt.Translation(vec.Normalize() * CollisionCM);
            points.Add(pt);
            pt = pt.Translation(-v * CollisionD);
            points.Add(pt);
            var pl = PolyFromPoints(points.ToList());
            CollisionD = collisionD;
            return pl;
        }
        private Polygon ConvertVertCarToCollisionCar(LineSegment baseline, Vector2D vec)
        {
            var collisionD = CollisionD;
            CollisionD = 300;
            vec = vec.Normalize();
            List<Coordinate> points = new List<Coordinate>();
            var pt = baseline.P0;
            points.Add(pt);
            pt = pt.Translation(vec * CollisionCT);
            points.Add(pt);
            pt = pt.Translation(-Vector(baseline).Normalize() * CollisionD);
            points.Add(pt);
            pt = pt.Translation(vec * CollisionCM);
            points.Add(pt);
            pt = pt.Translation(Vector(baseline).Normalize() * CollisionD);
            points.Add(pt);
            pt = pt.Translation(vec * (DisVertCarLength - CollisionCT - CollisionCM - CollisionTOP));
            points.Add(pt);
            pt = pt.Translation(Vector(baseline).Normalize() * DisVertCarWidth);
            points.Add(pt);
            pt = pt.Translation(-vec * (DisVertCarLength - CollisionCT - CollisionCM - CollisionTOP));
            points.Add(pt);
            pt = pt.Translation(Vector(baseline).Normalize() * CollisionD);
            points.Add(pt);
            pt = pt.Translation(-vec * CollisionCM);
            points.Add(pt);
            pt = pt.Translation(-Vector(baseline).Normalize() * CollisionD);
            points.Add(pt);
            pt = pt.Translation(-vec * CollisionCT);
            points.Add(pt);
            var pl = PolyFromPoints(points.ToList());
            CollisionD = collisionD;
            return pl;
        }
        private class LocCar
        {
            public LocCar(Polygon car, Coordinate point)
            {
                Car = car;
                Point = point;
            }
            public Polygon Car;
            public Coordinate Point;
        }
        private class LocCarComparer : IEqualityComparer<LocCar>
        {
            public bool Equals(LocCar a, LocCar b)
            {
                if (a.Point.Distance(b.Point) < 1000) return true;
                return false;
            }
            public int GetHashCode(LocCar car)
            {
                return ((int)car.Point.X).GetHashCode();
            }
        }
        private void RemoveDuplicateCars()
        {
            var locCars = CarSpots.Select(e => new LocCar(e, e.Envelope.Coordinate));
            var compare = new LocCarComparer();
            locCars = locCars.Distinct(compare);
            CarSpots = locCars.Select(e => e.Car).ToList();
        }
        private void RemoveCarsIntersectedWithBoundary()
        {
            var obspls = Obstacles.Where(e => e.Area > DisVertCarLength * DisLaneWidth * 5).ToList();
            var tmps = new List<Polygon>();
            foreach (var e in CarSpots)
            {
                var k = e.Clone();
                k = k.Scale(ScareFactorForCollisionCheck);
                var conda = Boundary.Contains(k.Envelope.Centroid.Coordinate);
                //var condb = !IsInAnyPolys(k.Envelope.Centroid.Coordinate, obspls);
                var _tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(k.Envelope.Centroid).Cast<Polygon>().ToList();
                _tmpobs = _tmpobs.Where(t => t.Area > DisVertCarLength * DisLaneWidth * 5).ToList();
                var condb = !IsInAnyPolys(k.Envelope.Centroid.Coordinate, _tmpobs);
                //var condc = Boundary.Intersect(k, Intersect.OnBothOperands).Count == 0;
                //if (conda && condb && condc) tmps.Add(e);
                if (conda && condb)
                {
                    if (Boundary.ClosestPoint(k.Envelope.Centroid.Coordinate).Distance(k.Envelope.Centroid.Coordinate) > DisVertCarLength)
                        tmps.Add(e);
                    else if (Boundary.IntersectPoint(k).Count() == 0)
                        tmps.Add(e);
                }
            }
            CarSpots = tmps;
            var tps = new List<InfoCar>();
            foreach (var e in Cars)
            {
                var k = e.Polyline.Clone();
                k = k.Scale(ScareFactorForCollisionCheck);
                var conda = Boundary.Contains(k.Envelope.Centroid);
                //var condb = !IsInAnyPolys(k.Envelope.Centroid.Coordinate, obspls);
                var _tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(k.Envelope.Centroid).Cast<Polygon>().ToList();
                _tmpobs = _tmpobs.Where(t => t.Area > DisVertCarLength * DisLaneWidth * 5).ToList();
                var condb = !IsInAnyPolys(k.Envelope.Centroid.Coordinate, _tmpobs);
                //var condc = Boundary.Intersect(k, Intersect.OnBothOperands).Count == 0;
                //if (conda && condb && condc) tmps.Add(e);
                if (conda && condb)
                {
                    if (Boundary.ClosestPoint(k.Envelope.Centroid.Coordinate).Distance(k.Envelope.Centroid.Coordinate) > DisVertCarLength)
                        tps.Add(e);
                    else if (Boundary.IntersectPoint(k).Count() == 0)
                        tps.Add(e);
                }
            }
            Cars = tps;
        }
        public void ReDefinePillarDimensions()
        {
            IniPillar.AddRange(Pillars.Select(e => e.Clone()));
            if (HasImpactOnDepthForPillarConstruct)
            {
                Pillars = Pillars.Select(e =>
                {
                    var segobjs = e.GetEdges();
                    if (DisPillarLength < DisPillarDepth)
                    {
                        double t = DisPillarDepth;
                        DisPillarDepth = DisPillarLength;
                        DisPillarLength = t;
                        t = PillarNetLength;
                        PillarNetLength = PillarNetDepth;
                        PillarNetDepth = t;
                    }
                    var segs = segobjs.OrderByDescending(t => t.Length).ToList();
                    if (DisPillarLength < DisPillarDepth)
                        segs = segobjs.OrderBy(t => t.Length).ToList();
                    LineSegment a = new LineSegment();
                    LineSegment b = new LineSegment();
                    if (DisPillarLength != DisPillarDepth)
                    {
                        a = segs[0];
                        b = segs[1];
                    }
                    else
                    {
                        a = segs[0];
                        segs.RemoveAt(0);
                        b = segs.Where(t => IsParallelLine(t, a)).First();
                    }
                    b = new LineSegment(b.P1, b.P0);
                    a = a.Scale(PillarNetLength / a.Length);
                    b = b.Scale(PillarNetLength / b.Length);
                    a = a.Translation(new Vector2D(a.MidPoint, b.MidPoint).Normalize() * ThicknessOfPillarConstruct);
                    b = b.Translation(-new Vector2D(a.MidPoint, b.MidPoint).Normalize() * ThicknessOfPillarConstruct);
                    var pl = PolyFromLines(a, b);
                    return pl;
                }).ToList();
            }
        }
        private void InsuredForTheCaseOfoncaveBoundary()
        {
            var lanebox = IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2 - 1).Scale(ScareFactorForCollisionCheck));
            var carspacialindex = new MNTSSpatialIndex(CarSpots);
            var pillarspacialindex = new MNTSSpatialIndex(Pillars);
            var cars_to_remove = new List<Polygon>();
            var pillars_to_remove = new List<Polygon>();
            foreach (var lane in lanebox)
            {
                var cars = carspacialindex.SelectCrossingGeometry(lane).Cast<Polygon>();
                var pillars = pillarspacialindex.SelectCrossingGeometry(lane).Cast<Polygon>();
                cars_to_remove.AddRange(cars);
                pillars_to_remove.AddRange(pillars);
            }
            cars_to_remove = cars_to_remove.Distinct().ToList();
            pillars_to_remove = pillars_to_remove.Distinct().ToList();
            CarSpots = CarSpots.Except(cars_to_remove).ToList();
            for (int i = 0; i < Cars.Count; i++)
            {
                foreach (var remove in cars_to_remove)
                {
                    if (Cars[i].Polyline.Centroid.Coordinate.Distance(remove.Centroid.Coordinate) < 1)
                    {
                        Cars.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            Pillars = Pillars.Except(pillars_to_remove).ToList();
        }
    }
}
