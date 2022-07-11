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

namespace ThParkingStall.Core.ObliqueMPartitionLayout.Services
{
    public partial class ObliqueMPartition
    {
        public LineSegment TranslateReservedConnection(LineSegment line, Vector2D vector)
        {
            var res = line.Translation(vector);
            var nonparallellines = IniLanes.Where(e => !IsParallelLine(line, e.Line)).Select(e => e.Line);
            //start
            nonparallellines = nonparallellines.OrderBy(e => e.ClosestPoint(line.P0).Distance(line.P0));
            if (nonparallellines.Any() && nonparallellines.First().ClosestPoint(line.P0).Distance(line.P0) < 10)
            {
                var start = res.P0;
                var start_connected_line = nonparallellines.First();
                //偏移直线超出了边界2750的case
                if (!Boundary.Contains(res.MidPoint))
                {
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
                if (!Boundary.Contains(res.MidPoint))
                {
                    res = res.Translation(vector.Normalize() * DisLaneWidth / 2);
                }
            }
            else
            {
                if (Boundary.ClosestPoint(line.P0).Distance(line.P0) < 10)
                {
                    var start = res.P0;
                    res.P0 = res.P0.Translation(-Vector(res).Normalize() * MaxLength);
                    var intersects = res.IntersectPoint(Boundary);
                    if (intersects.Count() > 0)
                        res.P0 = intersects.OrderBy( P=> P.Distance(start)).First();
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
                //偏移直线超出了边界2750的case
                if (!Boundary.Contains(res.MidPoint))
                {
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
                if (!Boundary.Contains(res.MidPoint))
                {
                    res = res.Translation(vector.Normalize() * DisLaneWidth / 2);
                }
            }
            else
            {
                if (Boundary.ClosestPoint(line.P1).Distance(line.P1) < 10)
                {
                    var end= res.P1;
                    res.P1 = res.P1.Translation(Vector(res).Normalize() * MaxLength);
                    var intersects = res.IntersectPoint(Boundary);
                    if (intersects.Count() > 0)
                        res.P1 = intersects.OrderBy(p=> p.Distance(end)).First();
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
        public List<Lane> GeneratePerpModuleLanes(double mindistance, double minlength, bool judge_cross_carbox = true
, Lane specialLane = null, bool check_adj_collision = false, List<Lane> rlanes = null, bool transform_start_edge_for_perp_module = false)
        {
            var pillarSpatialIndex = new MNTSSpatialIndex(Pillars);
            var lanes = new List<Lane>();
            var Lanes = IniLanes;
            if (rlanes != null)
                Lanes = rlanes;
            if (specialLane != null)
                Lanes = new List<Lane>() { specialLane };
            foreach (var lane in Lanes)
            {
                var line = new LineSegment(lane.Line);
                var linetest = new LineSegment(line);
                linetest = TranslateReservedConnection(linetest,lane.Vec.Normalize() * mindistance);
                var bdpl = PolyFromLines(line, linetest);
                bdpl = bdpl.Scale(ScareFactorForCollisionCheck);
                var bdpoints = Boundary.Coordinates.ToList();
                bdpoints.AddRange(Boundary.IntersectPoint(bdpl));
                bdpl = PolyFromLines(line, linetest);
                bdpoints = bdpoints.Where(p => bdpl.IsPointInFast(p)).Select(p => linetest.ClosestPoint(p)).ToList();
                //20220609
                //var on_points = bdpoints.Where(p => bdpl.ClosestPoint(p).Distance(p) < 1).ToList();
                //on_points = on_points.OrderByDescending(p => line.ClosestPoint(p).Distance(p)).ToList();
                //if (on_points.Count() > 1)
                //{
                //    on_points.RemoveAt(0);
                //    bdpoints = bdpoints.Except(on_points).ToList();
                //}
                //20220609测试性修改
                var bdsplits = SplitLine(linetest, bdpoints).Where(e => Boundary.Contains(e.MidPoint) || Boundary.ClosestPoint(e.MidPoint).Distance(e.MidPoint) < 1).Where(e => e.Length >= minlength);
                foreach (var bsplit in bdsplits)
                {
                    var bdsplit = bsplit;
                    bdsplit = TranslateReservedConnection(bdsplit ,- lane.Vec.Normalize() * mindistance);
                    var bdsplittest = new LineSegment(bdsplit);
                    bdsplittest = TranslateReservedConnection(bdsplittest,lane.Vec.Normalize() * mindistance);
                    var obpl = PolyFromLines(bdsplit, bdsplittest);
                    obpl = obpl.Scale(ScareFactorForCollisionCheck);
                    var obcrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(obpl).Cast<Polygon>().ToList();
                    var obpoints = new List<Coordinate>();
                    foreach (var cross in obcrossed)
                    {
                        obpoints.AddRange(cross.Coordinates);
                        obpoints.AddRange(cross.IntersectPoint(obpl));
                    }
                    var pillarcrossed = pillarSpatialIndex.SelectCrossingGeometry(obpl).Cast<Polygon>().ToList();
                    foreach (var cross in pillarcrossed)
                    {
                        obpoints.AddRange(cross.Coordinates);
                        obpoints.AddRange(cross.IntersectPoint(obpl));
                    }
                    obpl = obpl.Scale(1 / (ScareFactorForCollisionCheck - 0.01));
                    obpoints = obpoints.Where(p => obpl.IsPointInFast(p)).Select(p => bdsplittest.ClosestPoint(p)).ToList();
                    var boxsplits = SplitLine(bdsplittest, obpoints).Where(e =>
                    {
                        var box = PolyFromLines(e.Scale(ScareFactorForCollisionCheck), e.Scale(ScareFactorForCollisionCheck).Translation(-lane.Vec.Normalize() * mindistance));
                        foreach (var cross in obcrossed)
                        {
                            if (cross.IntersectPoint(box).Count() > 0) return false;
                        }
                        return !IsInAnyPolys(e.MidPoint, obcrossed);
                    }).Where(e => e.Length >= minlength);
                    foreach (var bxsplit in boxsplits)
                    {
                        var boxsplit = bxsplit;
                        boxsplit = TranslateReservedConnection(boxsplit ,- lane.Vec.Normalize() * mindistance);
                        var boxsplittest = new LineSegment(boxsplit);
                        boxsplittest = TranslateReservedConnection(boxsplittest,lane.Vec.Normalize() * mindistance);
                        var boxpl = PolyFromLines(boxsplit, boxsplittest);
                        boxpl = boxpl.Scale(ScareFactorForCollisionCheck);
                        var boxcrossed = new List<Polygon>();
                        if (judge_cross_carbox)
                            boxcrossed = CarBoxesSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                        else
                        {
                            //boxcrossed = CarSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                            //修改，有的背靠背模块第二模块没有生成carmoudle只生成车道线，对这种背靠背车位的过滤
                            var carcrossed = CarSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                            if (mindistance == DisCarAndHalfLaneBackBack && ScareEnabledForBackBackModule)
                            {
                                var iniboxpl = PolyFromLines(boxsplit, boxsplittest);
                                foreach (var car_cross in carcrossed)
                                {
                                    var g = NetTopologySuite.Operation.OverlayNG.OverlayNGRobust.Overlay(car_cross, iniboxpl, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
                                    if (g is Polygon)
                                    {
                                        var cond_area = Math.Round(g.Area) <= (DisVertCarLength - DisVertCarLengthBackBack) * DisVertCarWidth;
                                        var cond_type = true;
                                        var existed_index = Cars.Select(e => e.Polyline).ToList().IndexOf(car_cross);
                                        if (existed_index >= 0)
                                        {
                                            cond_type = Cars[existed_index].CarLayoutMode != 1;
                                        }
                                        else cond_type = false;
                                        //var cond_area = Math.Abs((DisVertCarLength - DisVertCarLengthBackBack) * DisVertCarWidth - g.Area) > 1;
                                        if (!cond_area)
                                        {
                                            //0628该情况应为是加感叹号相反条件，之前判断条件相反，在一个很长的有点凹的两根车道线相错很少的case发现。
                                            boxcrossed.Add(car_cross);
                                        }
                                    }
                                    else boxcrossed.Add(car_cross);
                                }
                            }
                            else
                                boxcrossed = carcrossed;
                        }
                        boxpl = boxpl.Translation(lane.Vec.Normalize() * DisLaneWidth / 2);
                        //boxsplittest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * DisLaneWidth / 2));
                        //boxpl = CreatPolyFromLines(boxsplit, boxsplittest);
                        //boxpl.Scale(boxpl.GetRecCentroid(), ScareFactorForCollisionCheck);
                        boxcrossed.AddRange(LaneSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>());
                        //boxcrossed.AddRange(LaneBufferSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>());
                        foreach (var bx in LaneBufferSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>())
                        {
                            var pts = bx.Coordinates.ToList();
                            var pta = AveragePoint(pts[0], pts[3]);
                            var ptb = AveragePoint(pts[1], pts[2]);
                            var lin = new LineSegment(pta, ptb);
                            boxcrossed.Add(BufferReservedConnection(lin,DisLaneWidth / 2));
                        }
                        boxsplittest = TranslateReservedConnection(boxsplittest,lane.Vec.Normalize() * DisLaneWidth / 2);
                        var boxpoints = new List<Coordinate>();
                        foreach (var cross in boxcrossed)
                        {
                            boxpoints.AddRange(cross.Coordinates);
                            boxpoints.AddRange(cross.IntersectPoint(boxpl));
                        }
                        boxpl = boxpl.Scale(1 / (ScareFactorForCollisionCheck - 0.01));
                        boxpoints = boxpoints.Where(p => boxpl.IsPointInFast(p)).Select(p => boxsplittest.ClosestPoint(p)).ToList();
                        var splits = SplitLine(boxsplittest, boxpoints).Where(e => e.Length >= minlength)
                            .Where(e =>
                            {
                                var k = new LineSegment(e);
                                k = TranslateReservedConnection(k,-lane.Vec.Normalize() * DisLaneWidth / 2/*(minlength < DisLaneWidth / 2? minlength : DisLaneWidth / 2)*/);
                                var con = !IsInAnyBoxes(k.MidPoint, LaneBoxes, true);
                                if (con) return true;
                                else return false;
                            });
                        if (judge_cross_carbox)
                        {
                            splits = splits.Where(e => !IsInAnyBoxes(e.MidPoint, CarBoxes, true));
                        }
                        splits = splits.Where(e =>
                        {
                            var tlt = new LineSegment(boxsplit.ClosestPoint(e.P0), boxsplit.ClosestPoint(e.P1));
                            var plt = PolyFromLines(tlt, e);
                            foreach (var il in boxcrossed)
                            {
                                if (plt.Contains(il.Centroid)) return false;
                            }
                            return true;
                        });
                        foreach (var slit in splits)
                        {
                            var split = slit;
                            split = TranslateReservedConnection(split ,- lane.Vec.Normalize() * DisLaneWidth / 2);
                            split =TranslateReservedConnection(split ,- lane.Vec.Normalize() * mindistance);
                            if (transform_start_edge_for_perp_module && split.Length > DisLaneWidth / 2 + DisVertCarLength)
                            {
                                if (ClosestPointInLines(split.P0, split, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e,split))) < 10)
                                    split.P0 = split.P0.Translation(Vector(split).Normalize() * (DisLaneWidth / 2 + DisVertCarLength));
                            }
                            if (ClosestPointInLines(split.P0, split, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, split))) < 10)
                                split.P0 = split.P0.Translation(Vector(split).Normalize() * DisLaneWidth / 2);
                            if (ClosestPointInLines(split.P1, split, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, split))) < 10)
                                split.P1 = split.P1.Translation(-Vector(split).Normalize() * DisLaneWidth / 2);
                            //
                            var splitnw = new LineSegment(split);
                            splitnw = TranslateReservedConnection(splitnw,lane.Vec.Normalize() * DisLaneWidth / 2);
                            if (check_adj_collision)
                            {
                                var pls = ConvertSpecialCollisionCheckRegionForLane(splitnw, lane.Vec.Normalize());
                                var plsc = pls.Clone();
                                plsc = plsc.Scale(ScareFactorForCollisionCheck);
                                var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(plsc).Cast<Polygon>().ToList();
                                var crossedstring = new List<LineString>();
                                crossed.ForEach(e => crossedstring.Add(new LineString(e.Coordinates)));
                                Obstacles.ForEach(o =>
                                {
                                    if (o.Contains(plsc.Envelope.Centroid)) crossedstring.Add(new LineString(o.Coordinates));
                                });
                                Walls?.ForEach(wall =>
                                {
                                    try
                                    {
                                        if (plsc.IntersectPoint(wall).Count() > 0)
                                            crossedstring.Add(wall);
                                    }
                                    catch (Exception ex)
                                    {
                                        ;
                                    }
                                });
                                var points = new List<Coordinate>();
                                foreach (var cross in crossedstring)
                                {
                                    points.AddRange(cross.Coordinates.Where(pt => pls.Contains(pt)));
                                    points.AddRange(cross.IntersectPoint(pls));
                                }
                                var diss = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P0));
                                if (diss.Count() > 0)
                                {
                                    var collisionD = CollisionD;
                                    CollisionD = 300;
                                    var dis = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P0)).First().Distance(splitnw.P0);
                                    var disc = CollisionD - dis >= 0 ? CollisionD - dis : 0;
                                    splitnw = new LineSegment(splitnw.P0.Translation(Vector(splitnw).Normalize() * disc), splitnw.P1);
                                    CollisionD = collisionD;
                                }

                                pls = ConvertSpecialCollisionCheckRegionForLane(splitnw, lane.Vec.Normalize(), false);
                                plsc = pls.Clone();
                                plsc = plsc.Scale(ScareFactorForCollisionCheck);
                                crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(plsc).Cast<Polygon>().ToList();
                                crossedstring = new List<LineString>();
                                crossed.ForEach(e => crossedstring.Add(new LineString(e.Coordinates)));
                                Obstacles.ForEach(o =>
                                {
                                    if (o.Contains(plsc.Envelope.Centroid)) crossedstring.Add(new LineString(o.Coordinates));
                                });
                                Walls?.ForEach(wall =>
                                {
                                    try
                                    {
                                        if (plsc.IntersectPoint(wall).Count() > 0)
                                            crossedstring.Add(wall);
                                    }
                                    catch (Exception ex)
                                    {
                                        ;
                                    }
                                });
                                points = new List<Coordinate>();
                                foreach (var cross in crossedstring)
                                {
                                    points.AddRange(cross.Coordinates.Where(pt => pls.Contains(pt)));
                                    points.AddRange(cross.IntersectPoint(pls));
                                }
                                diss = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P1));
                                if (diss.Count() > 0)
                                {
                                    var collisionD = CollisionD;
                                    CollisionD = 300;
                                    var dis = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P1)).First().Distance(splitnw.P1);
                                    var disc = CollisionD - dis >= 0 ? CollisionD - dis : 0;
                                    splitnw = new LineSegment(splitnw.P0, splitnw.P1.Translation(-Vector(splitnw).Normalize() * disc));
                                    CollisionD = collisionD;
                                }
                            }
                            if (splitnw.Length < minlength) continue;
                            splitnw = TranslateReservedConnection(splitnw ,- lane.Vec.Normalize() * DisLaneWidth / 2);
                            Lane ln = new Lane(splitnw, lane.Vec);
                            lanes.Add(ln);
                        }
                    }
                }
            }
            return lanes;
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
    }
}
