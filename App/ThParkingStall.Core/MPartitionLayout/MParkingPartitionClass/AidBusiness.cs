using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        public static void SortLaneByDirection(List<Lane> lanes, int mode)
        {
            var comparer = new LaneComparer(mode,DisCarAndHalfLaneBackBack);
            lanes.Sort(comparer);
        }
        private class LaneComparer : IComparer<Lane>
        {
            public LaneComparer(int mode,double filterLength)
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
                else if (Mode == 1)
                {
                    if (IsHorizontalLine(a.Line) && a.Line.Length > FilterLength && !IsHorizontalLine(b.Line)) return -1;
                    else if (!IsHorizontalLine(a.Line) && IsHorizontalLine(b.Line) && b.Line.Length > FilterLength) return 1;
                    else
                    {
                        return CompareLength(a.Line, b.Line);
                    }
                }
                else if (Mode == 2)
                {
                    if (IsVerticalLine(a.Line) && a.Line.Length > FilterLength && !IsVerticalLine(b.Line)) return -1;
                    else if (!IsVerticalLine(a.Line) && IsVerticalLine(b.Line) && b.Line.Length > FilterLength) return 1;
                    else
                    {
                        return CompareLength(a.Line, b.Line);
                    }
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
        private List<LineSegment> SplitBufferLineByPoly(LineSegment line, double distance, Polygon cutter)
        {
            var pl = line.Buffer(distance);
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
        private List<LineSegment> SplitLineBySpacialIndexInPoly(LineSegment line, Polygon polyline, MNTSSpatialIndex spatialIndex, bool allow_on_edge = true)
        {
            var crossed = spatialIndex.SelectCrossingGeometry(polyline).Cast<Polygon>();
            crossed = crossed.Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0);
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
        private bool IsConnectedToLane(LineSegment line)
        {
            if (ClosestPointInVertLines(line.P0, line, IniLanes.Select(e => e.Line)) < 10
                || ClosestPointInVertLines(line.P1, line, IniLanes.Select(e => e.Line)) < 10)
                return true;
            else return false;
        }
        private bool IsConnectedToLane(LineSegment line, bool Startpoint)
        {
            if (Startpoint)
            {
                if (ClosestPointInVertLines(line.P0, line, IniLanes.Select(e => e.Line)) < 10) return true;
                else return false;
            }
            else
            {
                if (ClosestPointInVertLines(line.P1, line, IniLanes.Select(e => e.Line)) < 10) return true;
                else return false;
            }
        }
        private bool IsConnectedToLane(LineSegment line, bool Startpoint,List<LineSegment> lanes)
        {
            if (Startpoint)
            {
                if (ClosestPointInVertLines(line.P0, line, lanes) < 10) return true;
                else return false;
            }
            else
            {
                if (ClosestPointInVertLines(line.P1, line, lanes.Select(e => e)) < 10) return true;
                else return false;
            }
        }
        private bool HasParallelLaneForwardExisted(LineSegment line, Vector2D vec, double maxlength, double minlength, ref double dis_to_move
           , ref LineSegment prepLine,ref List<LineSegment> paras_lines)
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
        private double IsEssentialToCloseToBuilding(LineSegment lane, Vector2D vec)
        {
            return -1;
            var line = lane.Scale(ScareFactorForCollisionCheck);
            if (!GenerateLaneForLayoutingCarsInShearWall) return -1;
            if (!IsPerpVector(vec, Vector2D.Create(1, 0))) return -1;
            if (vec.Y < 0) return -1;
            var bf = line.Buffer(DisLaneWidth / 2);
            if (ObstaclesSpatialIndex.SelectCrossingGeometry(bf).Count > 0)
            {
                return -1;
            }
            var linetest = new LineSegment(line);
            linetest=linetest.Translation(vec * MaxLength);
            var pl = PolyFromLines(line, linetest);
            var points = new List<Coordinate>();
            points = ObstacleVertexes.Where(e => pl.IsPointInFast(e)).OrderBy(e => line.ClosestPoint(e).Distance(e)).ToList();
            if (points.Count() < 1) return -1;
            var crossedplys = ObstaclesSpatialIndex.SelectCrossingGeometry(pl).Cast<Polygon>();
            double crossedlength = 0;
            foreach (var c in crossedplys)
            {
                var crossededges = SplitCurve(c, pl).Where(e => pl.IsPointInFast(e.GetMidPoint()));
                foreach (var ed in crossededges) crossedlength += ed.Length;
            }
            if (crossedlength < 10000) return -1;
            var ltest_ob_near_boundary = LineSegmentSDL(points.First(), vec, DisVertCarLength);
            if (ltest_ob_near_boundary.IntersectPoint(Boundary).Count() > 0) return -1;
            var dist = line.ClosestPoint(points.First()).Distance(points.First());
            var lperp = LineSegmentSDL(line.MidPoint.Translation(vec * 100), vec, dist + 1);
            var lanes = IniLanes.Where(e => IsParallelLine(e.Line, line))
                .Where(e => e.Line.IntersectPoint(lperp).Count() > 0);
            if (lanes.Count() > 0) return -1;
            var lt = new LineSegment(line);
            lt=lt.Translation(vec * dist);
            var ltbf = lt.Buffer(DisLaneWidth / 2);
            points = points.Where(e => ltbf.IsPointInFast(e)).OrderBy(e => e.X).ToList();
            if (points.Count() < 1) return -1;
            var length = points.Last().X - points.First().X;
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            int offsetcount = 0;
            bool isvalid = false;
            lt=lt.Translation(-vec * DisLaneWidth / 2);
            int cyclecount = 5;
            var moduledist = (dist - DisLaneWidth / 2) % DisModulus;
            if (moduledist > 0 && moduledist <= DisVertCarLength)
            {
                dist = dist - DisLaneWidth / 2;
                dist = dist - moduledist;
                return dist;
            }
            for (int i = 0; i < cyclecount; i++)
            {
                var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, new Lane(lt, vec.Normalize()),true);
                double validlength = 0;
                vertlanes.ForEach(e => validlength += e.Line.Length);
                if (validlength >= length / 2)
                {
                    isvalid = true;
                    break;
                }
                offsetcount++;
                lt=lt.Translation(-vec.Normalize() * (DisVertCarLength / cyclecount));
            }
            if (isvalid)
            {
                dist = dist - DisLaneWidth / 2;
                dist = dist - offsetcount * (DisVertCarLength / cyclecount);
                return dist;
            }
            return -1;
        }
        public void UpdateLaneBoxAndSpatialIndexForGenerateVertLanes()
        {
            LaneSpatialIndex.Update(IniLanes.Select(e => (Geometry)PolyFromLine(e.Line)).ToList(), new List<Geometry>());
            LaneBoxes.AddRange(IniLanes.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = new LineSegment(e.Line);
                var lb = new LineSegment(e.Line);
                la=la.Translation(Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                lb=lb.Translation(-Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                var py = PolyFromLines(la, lb);
                return py;
            }));
            LaneBoxes.AddRange(CarModules.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = new LineSegment(e.Line);
                var lb = new LineSegment(e.Line);
                la=la.Translation(Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
                lb=lb.Translation(-Vector(la).GetPerpendicularVector().Normalize() * (DisLaneWidth / 2 - 10));
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
        private bool CloseToWall(Coordinate point,LineSegment line)
        {
            if (Walls.Count == 0) return false;
            double tol = 10;
            if (/*Walls.Count == 0*/false)
            {
                if (Boundary.ClosestPoint(point).Distance(point) < tol && ClosestPointInVertLines(point, line, IniLanes.Select(e => e.Line)) > tol)
                    return true;
                return false;
            }
            var dis = ClosestPointInCurvesFast(point, Walls);
            if (dis < tol) return true;
            else return false;
        }
        public bool IsConnectedToLaneDouble(LineSegment line)
        {
            if (IsConnectedToLane(line, true) && IsConnectedToLane(line, false)) return true;
            else return false;
        }
        private static void UnifyLaneDirection(ref LineSegment lane, List<Lane> iniLanes)
        {
            var line = new LineSegment(lane);
            var lanes = iniLanes.Select(e => e.Line).Where(e => IsPerpLine(line, e)).ToList();
            var lanestrings = lanes.Select(e => e.ToLineString()).ToList();
            if (lanes.Count > 0)
            {
                if (ClosestPointInCurves(line.P1, lanes.Select(e =>e.ToLineString()).ToList()) < 1 && ClosestPointInCurves(line.P0, lanes.Select(e => e.ToLineString()).ToList()) < 1)
                {
                    //if (line.P0.X - line.P1.X > 1000) line=new LineSegment(line.P1,line.P0);
                    //else if (lanes.Count == 0 && line.P0.Y - line.P1.Y > 1000) line = new LineSegment(line.P1, line.P0);
                    var startline = lanes.Where(e => e.ClosestPoint(line.P0).Distance(line.P0) < 1).OrderByDescending(e => e.Length).First();
                    var endline = lanes.Where(e => e.ClosestPoint(line.P1).Distance(line.P1) < 1).OrderByDescending(e => e.Length).First();
                    if(endline.Length>startline.Length) line = new LineSegment(line.P1, line.P0);
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
                k=k.Scale( ScareFactorForCollisionCheck);
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
                k=k.Scale( ScareFactorForCollisionCheck);
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
        private void RemoveInvalidPillars()
        {
            List<Polygon> tmps = new List<Polygon>();
            foreach (var t in Pillars)
            {
                var clone = t.Clone();
                clone=clone.Scale( 0.5);
                if (ClosestPointInCurveInAllowDistance(clone.Envelope.Centroid.Coordinate, CarSpots, DisPillarLength + DisHalfCarToPillar))
                {
                    tmps.Add(t);
                }
            }
            Pillars = tmps;
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
                    a=a.Scale( PillarNetLength / a.Length);
                    b=b.Scale( PillarNetLength / b.Length);
                    a=a.Translation(new Vector2D(a.MidPoint, b.MidPoint).Normalize() * ThicknessOfPillarConstruct);
                    b=b.Translation(-new Vector2D(a.MidPoint, b.MidPoint).Normalize() * ThicknessOfPillarConstruct);
                    var pl = PolyFromLines(a, b);
                    return pl;
                }).ToList();
            }
        }
        private void ClassifyLanesForLayoutFurther()
        {
            ProcessLanes(ref IniLanes);
            OutEnsuredLanes.AddRange(OriginalLanes);

            var lanes=IniLanes.Select(e => e).ToList();
            var found = false;
            while (true)
            {
                found = false;
                //拿双边连接的车道线
                bool found_connected_double = false;
                while (true)
                {
                    found_connected_double = false;
                    for (int i = 0; i < lanes.Count; i++)
                    {
                        //筛重合
                        var overlap = false;
                        foreach (var lane in OutEnsuredLanes)
                        {
                            var cond_a = lane.ClosestPoint(lanes[i].Line.P0,false).Distance(lanes[i].Line.P0) < 1;
                            var cond_b = lane.ClosestPoint(lanes[i].Line.P1,false).Distance(lanes[i].Line.P1) < 1;
                            if (cond_a && cond_b)
                            {
                                overlap = true;
                                break;
                            }
                        }
                        if (overlap) continue;
                        if (!IsConnectedToLaneDouble(lanes[i].Line)) continue;
                        if (!IsConnectedToLane(lanes[i].Line, true, OutEnsuredLanes)) continue;
                        if (!IsConnectedToLane(lanes[i].Line, false, OutEnsuredLanes)) continue;
                        found_connected_double = true;
                        found = true;
                        OutEnsuredLanes.Add(lanes[i].Line);
                        lanes.RemoveAt(i);
                        break;
                    }
                    if (!found_connected_double) break;
                }
                //拿一端连接墙的车道线
                for (int i = 0; i < lanes.Count; i++)
                {
                    //筛重合
                    var overlap = false;
                    foreach (var lane in OutEnsuredLanes)
                    {
                        var cond_a = lane.ClosestPoint(lanes[i].Line.P0,false).Distance(lanes[i].Line.P0) < 1;
                        var cond_b = lane.ClosestPoint(lanes[i].Line.P1,false).Distance(lanes[i].Line.P1) < 1;
                        if (cond_a && cond_b)
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (overlap) continue;
                    if (IsConnectedToLane(lanes[i].Line, false, OutEnsuredLanes))
                        lanes[i].Line = new LineSegment(lanes[i].Line.P1, lanes[i].Line.P0);
                    if (!IsConnectedToLane(lanes[i].Line, true, OutEnsuredLanes)) continue;
                    foreach (var wall in Walls)
                    {
                        if (wall.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1)
                        {
                            OutEnsuredLanes.Add(lanes[i].Line);
                            found = true;
                            lanes.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                    if (!found)
                    {
                        if (OutBoundary.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1 || !OutBoundary.Contains(lanes[i].Line.P1))
                        {
                            OutEnsuredLanes.Add(lanes[i].Line);
                            found = true;
                            lanes.RemoveAt(i);
                            i--;
                        }
                    }

                }
                if (!found) break;
            }
            OriginalLanes.ForEach(e => OutEnsuredLanes.Remove(e));
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
                var cars= carspacialindex.SelectCrossingGeometry(lane).Cast<Polygon>();
                var pillars= pillarspacialindex.SelectCrossingGeometry(lane).Cast<Polygon>();
                cars_to_remove.AddRange(cars);
                pillars_to_remove.AddRange(pillars);
            }
            cars_to_remove=cars_to_remove.Distinct().ToList();
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
            pt = pt.Translation(vec * ( DisVertCarLength - CollisionCT - CollisionCM - CollisionTOP));
            points.Add(pt);
            pt = pt.Translation(Vector(baseline).Normalize() * DisVertCarWidth);
            points.Add(pt);
            pt = pt.Translation(-vec * ( DisVertCarLength - CollisionCT - CollisionCM - CollisionTOP));
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
    }
}
