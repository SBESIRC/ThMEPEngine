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
                linetest = TranslateReservedConnection(linetest,lane.Vec.Normalize() * mindistance,false);
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
                    bdsplit = TranslateReservedConnection(bdsplit ,- lane.Vec.Normalize() * mindistance,false);
                    var bdsplittest = new LineSegment(bdsplit);
                    bdsplittest = TranslateReservedConnection(bdsplittest,lane.Vec.Normalize() * mindistance,false);
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
                        boxsplit = TranslateReservedConnection(boxsplit ,- lane.Vec.Normalize() * mindistance,false);
                        var boxsplittest = new LineSegment(boxsplit);
                        boxsplittest = TranslateReservedConnection(boxsplittest,lane.Vec.Normalize() * mindistance,false);
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
                        var lnbox = LaneBufferSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>();
                        foreach (var bx in lnbox)
                        {
                            var pts = bx.Coordinates.ToList();
                            var pta = AveragePoint(pts[0], pts[3]);
                            var ptb = AveragePoint(pts[1], pts[2]);
                            var lin = new LineSegment(pta, ptb);
                            boxcrossed.Add(BufferReservedConnection(lin,DisLaneWidth / 2));
                        }
                        boxsplittest = TranslateReservedConnection(boxsplittest,lane.Vec.Normalize() * DisLaneWidth / 2,false);
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
                                k = TranslateReservedConnection(k,-lane.Vec.Normalize() * DisLaneWidth / 2/*(minlength < DisLaneWidth / 2? minlength : DisLaneWidth / 2)*/,false);
                                var k_pl = PolyFromLines(k, TranslateReservedConnection(k, -lane.Vec.Normalize() *(mindistance-DisLaneWidth/2)));
                                k_pl = k_pl.Scale(ScareFactorForCollisionCheck);
                                //20220712
                                foreach (var lbox in lnbox)
                                {
                                    if (lbox.IntersectPoint(k_pl).Count() > 0) return false;
                                }
                                return true;
                                //20220712
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
                            split = TranslateReservedConnection(split ,- lane.Vec.Normalize() * DisLaneWidth / 2,false);
                            split =TranslateReservedConnection(split ,- lane.Vec.Normalize() * mindistance,false);
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
                            splitnw = TranslateReservedConnection(splitnw,lane.Vec.Normalize() * DisLaneWidth / 2,false);
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
                            splitnw = TranslateReservedConnection(splitnw ,- lane.Vec.Normalize() * DisLaneWidth / 2,false);
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
        private void RemoveInvalidPillars()
        {
            List<Polygon> tmps = new List<Polygon>();
            foreach (var t in Pillars)
            {
                var clone = t.Clone();
                clone = clone.Scale(0.5);
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
        private void GeneratePerpModuleBoxes(List<Lane> lanes)
        {
            SortLaneByDirection(lanes, LayoutMode);
            foreach (var lane in lanes)
            {
                var line = new LineSegment(lane.Line);
                List<LineSegment> ilanes = new List<LineSegment>();
                var segs = new List<LineSegment>();
                //line.P0 = line.P0.Translation(Vector(line).Normalize() * (DisVertCarLength - DisVertCarLengthBackBack));

                var dis = DisVertCarLength - DisVertCarLengthBackBack;
                var point_near_start = line.P0.Translation(Vector(line).Normalize() * DisVertCarLengthBackBack);
                var line_near_start = LineSegmentSDL(point_near_start, lane.Vec, MaxLength);
                line_near_start = SplitLine(line_near_start, Boundary).First();
                var buffer_near_start = PolyFromLines(line_near_start, line_near_start.Translation(-Vector(line).Normalize() * DisVertCarLengthBackBack));
                buffer_near_start = buffer_near_start.Scale(ScareFactorForCollisionCheck);
                var crossedpoints = new List<Coordinate>();
                //对近障碍物不做车道偏移200的处理
                //var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(buffer_near_start).Cast<Polygon>();
                //foreach (var obj in obscrossed)
                //{
                //    crossedpoints.AddRange(obj.Coordinates);
                //    crossedpoints.AddRange(obj.IntersectPoint(buffer_near_start));
                //}
                crossedpoints.AddRange(Boundary.IntersectPoint(buffer_near_start));
                crossedpoints.AddRange(Boundary.Coordinates);
                crossedpoints = crossedpoints.Where(p => buffer_near_start.Contains(p)).OrderBy(p => line_near_start.ClosestPoint(p).Distance(p)).ToList();
                if (crossedpoints.Count > 0)
                {
                    var point_dis = line.ClosestPoint(crossedpoints[0]).Distance(line.P0);
                    dis += point_dis;
                    //当车道贴墙需偏移的时候才偏移
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * dis);
                }
                DivideCurveByLength(line, DisBackBackModulus, ref segs);
                ilanes.AddRange(segs.Where(t => Math.Abs(t.Length - DisBackBackModulus) < 1));
                int modulecount = ilanes.Count;
                int vertcount = ((int)Math.Floor((line.Length - modulecount * DisBackBackModulus) / DisVertCarWidth));
                PerpModlues perpModlue = ConstructPerpModules(lane.Vec, ilanes);

                int step = 1;
                for (int i = 0; i < vertcount; i++)
                {
                    var test = UpdataPerpModlues(perpModlue, step);
                    if (test.Count >= perpModlue.Count)
                    {
                        perpModlue = test;
                        step = 1;
                    }
                    else
                    {
                        step++;
                        continue;
                    }
                }
                foreach (var pl in perpModlue.Bounds)
                {
                    var a = pl.GetEdges()[1];
                    var b = pl.GetEdges()[3];
                    var vec = new Vector2D(a.MidPoint, b.ClosestPoint(a.MidPoint, true));
                    IniLanes.Add(new Lane(a, vec.Normalize()));
                    CarModule module = new CarModule();
                    module.Box = pl;
                    module.Line = a;
                    module.Vec = vec;
                    //注：当模块两端连接车道时，调高排布的优先级；但优先级的设置IsInVertUnsureModule最初不是为了这种case.
                    module.IsInVertUnsureModule = perpModlue.IsInVertUnsureModule;
                    CarModules.Add(module);
                }
                CarBoxes.AddRange(perpModlue.Bounds);
                CarBoxesSpatialIndex.Update(perpModlue.Bounds, new List<Polygon>());
            }
        }
        private PerpModlues UpdataPerpModlues(PerpModlues perpModlues, int step)
        {
            PerpModlues result;
            int minindex = perpModlues.Mminindex;
            List<LineSegment> ilanes = new List<LineSegment>(perpModlues.Lanes);
            if (perpModlues.Lanes.Count == 0) return perpModlues;
            var vec_move = Vector(perpModlues.Lanes[0]).Normalize() * DisVertCarWidth * step;
            for (int i = 0; i < ilanes.Count; i++)
            {
                if (i >= minindex)
                {
                    ilanes[i] = ilanes[i].Translation(vec_move);
                }
            }
            result = ConstructPerpModules(perpModlues.Vec, ilanes);
            return result;
        }
        private PerpModlues ConstructPerpModules(Vector2D vec, List<LineSegment> ilanes)
        {
            PerpModlues result = new PerpModlues();
            int count = 0;
            int minindex = 0;
            int mincount = 9999;
            List<Polygon> plys = new List<Polygon>();
            vec = vec.Normalize() * MaxLength;
            var isInVertUnsureModule = true;
            for (int i = 0; i < ilanes.Count; i++)
            {
                int mintotalcount = 7;
                var unitbase = ilanes[i];
                int generatedcount = 0;
                var tmpplycount = plys.Count;
                var curcount = GenerateUsefulModules(unitbase, vec, plys, ref generatedcount, ref isInVertUnsureModule);
                if (plys.Count > tmpplycount)
                {
                    var pl = plys[plys.Count - 1];
                    var lane = pl.GetEdges()[3];

                    if (ClosestPointInLines(lane.P0, lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e,lane))) <= DisLaneWidth / 2
                        && ClosestPointInLines(lane.P1, lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, lane))) <= DisLaneWidth / 2)
                        mintotalcount = 16;
                }
                if (curcount < mintotalcount)
                {
                    ilanes.RemoveAt(i);
                    i--;
                    for (int j = 0; j < generatedcount; j++)
                    {
                        plys.RemoveAt(plys.Count - 1);
                    }
                    continue;
                }
                if (curcount < mincount)
                {
                    mincount = curcount;
                    minindex = i;
                }
                count += curcount;
            }
            result.Bounds = plys;
            result.Count = count;
            result.Lanes = ilanes;
            result.Mminindex = minindex;
            result.Vec = vec;
            result.IsInVertUnsureModule = isInVertUnsureModule;
            return result;
        }
        private int GenerateUsefulModules(LineSegment lane, Vector2D vec, List<Polygon> plys, ref int generatedcount, ref bool isInVertUnsureModule)
        {
            int count = 0;
            var unittest = new LineSegment(lane);
            unittest = unittest.Translation(vec.Normalize() * MaxLength);
            var pltest = PolyFromPoints(new List<Coordinate>() { lane.P0, lane.P1, unittest.P1, unittest.P0 });
            var pltestsc = pltest.Clone();
            pltestsc = pltestsc.Scale(ScareFactorForCollisionCheck);
            var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(pltestsc).Cast<Polygon>().ToList();
            crossed.AddRange(CarBoxesSpatialIndex.SelectCrossingGeometry(pltestsc).Cast<Polygon>());
            crossed.Add(Boundary);
            List<Coordinate> points = new List<Coordinate>();
            foreach (var o in crossed)
            {
                points.AddRange(o.Coordinates);
                //多线程：在单核模式中为pltest，调试出bug，临时改为pltestsc
                //points.AddRange(o.IntersectPoint(pltest));
                points.AddRange(o.IntersectPoint(pltestsc));
                var splitselves = new List<LineString>();
                try
                {
                    //points.AddRange(SplitCurve(o, pltestsc).Select(e => e.GetPointAtParameter(e.EndParam / 2)));
                    splitselves = SplitCurve(o, pltestsc).Where(f =>
                    {
                        if (f.Coordinates.Count() == 2) return pltestsc.Contains(f.GetMidPoint());
                        else if (f.Coordinates.Count() > 2) return pltestsc.Contains(f.Envelope.Centroid);
                        else return false;
                    }).ToList();
                }
                catch
                {
                }
                foreach (var sp in splitselves)
                {
                    if (sp.Coordinates.Count() == 2) points.Add(sp.GetMidPoint());
                    else if (sp.Coordinates.Count() > 2)
                    {
                        points.AddRange(sp.GetEdges().Select(f => f.MidPoint));
                    }
                }
            }
            points = points.Where(e => pltest.IsPointInFast(e) || pltest.ClosestPoint(e).Distance(e) < 1).Distinct().ToList();
            LineSegment edgea = new LineSegment(lane.P0, unittest.P0);
            LineSegment edgeb = new LineSegment(lane.P1, unittest.P1);
            var pointsa = points.Where(e => edgea.ClosestPoint(e).Distance(e) <
                    DisVertCarLengthBackBack + DisLaneWidth).OrderBy(p => edgea.ClosestPoint(p).Distance(lane.P0)).ToList();
            var pointsb = points.Where(e => edgeb.ClosestPoint(e).Distance(e) <
                      DisVertCarLengthBackBack + DisLaneWidth).OrderBy(p => edgeb.ClosestPoint(p).Distance(lane.P1)).ToList();
            for (int i = 0; i < pointsa.Count - 1; i++)
            {
                if (edgea.ClosestPoint(pointsa[i]).Distance(pointsa[i]) < 1)
                {
                    pointsa.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < pointsb.Count - 1; i++)
            {
                if (edgeb.ClosestPoint(pointsb[i]).Distance(pointsb[i]) < 1)
                {
                    pointsb.RemoveAt(i);
                    i--;
                }
            }

            //测试：返回只要满足车道宽度的车道——不以满足整个模块宽度的为车道线——避免的的情况在线后半部分依然能排布的情况
            var pointsa_lane = pointsa.Where(p =>
            {
                var dis = edgea.ClosestPoint(p).Distance(p);
                return dis < DisVertCarLengthBackBack + DisLaneWidth && dis > DisVertCarLengthBackBack;
            }).Select(p => edgea.ClosestPoint(p)).ToList();
            var pointsb_lane = pointsb.Where(p =>
            {
                var dis = edgeb.ClosestPoint(p).Distance(p);
                return dis < DisVertCarLengthBackBack + DisLaneWidth && dis > DisVertCarLengthBackBack;
            }).Select(p => edgeb.ClosestPoint(p)).ToList();
            Coordinate pta_lane;
            Coordinate ptb_lane;
            pointsa_lane = pointsa_lane.Where(e => e.Distance(lane.P0) > 1).OrderBy(e => e.Distance(lane.P0)).ToList();
            pointsb_lane = pointsb_lane.Where(e => e.Distance(lane.P1) > 1).OrderBy(e => e.Distance(lane.P1)).ToList();
            if (pointsa_lane.ToArray().Length == 0) pta_lane = lane.P0;
            else pta_lane = pointsa_lane.First();
            if (pointsb_lane.ToArray().Length == 0) ptb_lane = lane.P0;
            else ptb_lane = pointsb_lane.First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.ClosestPoint(pta_lane).Distance(pta_lane);
                if (disa < DisLaneWidth / 2)
                {
                    pta_lane = pta_lane.Translation((new Vector2D(pta_lane, lane.P0)).Normalize() * (DisLaneWidth / 2 - disa));
                }
                var disb = la.Line.ClosestPoint(ptb_lane).Distance(ptb_lane);
                if (disb < DisLaneWidth / 2)
                {
                    ptb_lane = ptb_lane.Translation(new Vector2D(ptb_lane, lane.P1).Normalize() * (DisLaneWidth / 2 - disb));
                }
            }
            LineSegment eb_lane = new LineSegment(lane.P1, ptb_lane);
            LineSegment ea_lane = new LineSegment(lane.P0, pta_lane);
            var pa_lane = PolyFromPoints(new List<Coordinate>() { lane.P0, lane.P0.Translation(new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack),
                pta_lane.Translation(new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack), pta_lane });
            if (pa_lane.Area > 0)
            {
                if (ClosestPointInLines(ea_lane.P0, ea_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e,ea_lane)).ToList()) < 1 &&
                    Math.Abs(ClosestPointInLines(ea_lane.P1, ea_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, ea_lane)).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
                    ea_lane.Length < DisLaneWidth / 2 + DisVertCarWidth * 4)
                {
                    count = 0;
                }
                else
                {
                    plys.Add(pa_lane);
                    generatedcount++;
                }
            }
            var pb_lane = PolyFromPoints(new List<Coordinate>() { lane.P1, lane.P1.Translation(-new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack),
                 ptb_lane.Translation(-new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack),ptb_lane});
            if (pb_lane.Area > 0)
            {
                if (ClosestPointInLines(eb_lane.P0, eb_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, eb_lane)).ToList()) < 1 &&
                    Math.Abs(ClosestPointInLines(eb_lane.P1, eb_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, eb_lane)).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
    eb_lane.Length < DisLaneWidth / 2 + DisVertCarWidth * 5)
                {
                    count = 0;
                }
                else
                {
                    plys.Add(pb_lane);
                    generatedcount++;
                }
            }
            //以上为测试代码


            pointsa = pointsa.Select(e => edgea.ClosestPoint(e)).ToList();
            pointsb = pointsb.Select(e => edgeb.ClosestPoint(e)).ToList();
            Coordinate pta;
            Coordinate ptb;
            pointsa = pointsa.Where(e => e.Distance(lane.P0) > 1).OrderBy(e => e.Distance(lane.P0)).ToList();
            pointsb = pointsb.Where(e => e.Distance(lane.P1) > 1).OrderBy(e => e.Distance(lane.P1)).ToList();
            if (pointsa.ToArray().Length == 0) pta = lane.P0;
            else pta = pointsa.First();
            if (pointsb.ToArray().Length == 0) ptb = lane.P0;
            else ptb = pointsb.First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.ClosestPoint(pta).Distance(pta);
                if (disa < DisLaneWidth / 2)
                {
                    pta = pta.Translation((new Vector2D(pta, lane.P0)).Normalize() * (DisLaneWidth / 2 - disa));
                    isInVertUnsureModule = false;
                }
                var disb = la.Line.ClosestPoint(ptb).Distance(ptb);
                if (disb < DisLaneWidth / 2)
                {
                    ptb = ptb.Translation(new Vector2D(ptb, lane.P1).Normalize() * (DisLaneWidth / 2 - disb));
                    isInVertUnsureModule = false;
                }
            }
            LineSegment eb = new LineSegment(lane.P1, ptb);
            LineSegment ea = new LineSegment(lane.P0, pta);
            count += ((int)Math.Floor((ea.Length - DisLaneWidth / 2 - DisPillarLength * 2) / DisVertCarWidth));
            count += ((int)Math.Floor((eb.Length - DisLaneWidth / 2 - DisPillarLength * 2) / DisVertCarWidth));

            return count;
        }
    }
}
