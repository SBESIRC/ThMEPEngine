using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class ParkingPartitionPro
    {
        public static void SortLaneByDirection(List<Lane> lanes, int mode)
        {
            var comparer = new LaneComparer(mode);
            lanes.Sort(comparer);
        }
        private class LaneComparer : IComparer<Lane>
        {
            public LaneComparer(int mode)
            {
                Mode = mode;
            }
            private int Mode;
            public int Compare(Lane a, Lane b)
            {
                if (Mode == 0)
                {
                    return CompareLength(a.Line, b.Line);
                }
                else if (Mode == 1)
                {
                    if (IsHorizontalLine(a.Line) && !IsHorizontalLine(b.Line)) return -1;
                    else if (!IsHorizontalLine(a.Line) && IsHorizontalLine(b.Line)) return 1;
                    else
                    {
                        return CompareLength(a.Line, b.Line);
                    }
                }
                else if (Mode == 2)
                {
                    if (IsVerticalLine(a.Line) && !IsVerticalLine(b.Line)) return -1;
                    else if (!IsVerticalLine(a.Line) && IsVerticalLine(b.Line)) return 1;
                    else
                    {
                        return CompareLength(a.Line, b.Line);
                    }
                }
                else return 0;
            }
            private int CompareLength(Line a, Line b)
            {
                if (a.Length > b.Length) return -1;
                else if (a.Length < b.Length) return 1;
                return 0;
            }
        }

        private bool IsConnectedToLane(Line line)
        {
            if (ClosestPointInVertLines(line.StartPoint, line, IniLanes.Select(e => e.Line)) < 10
                || ClosestPointInVertLines(line.EndPoint, line, IniLanes.Select(e => e.Line)) < 10)
                return true;
            else return false;
        }

        private bool IsConnectedToLane(Line line, bool Startpoint)
        {
            if (Startpoint)
            {
                if (ClosestPointInVertLines(line.StartPoint, line, IniLanes.Select(e => e.Line)) < 10) return true;
                else return false;
            }
            else
            {
                if (ClosestPointInVertLines(line.EndPoint, line, IniLanes.Select(e => e.Line)) < 10) return true;
                else return false;
            }
        }

        private bool IsConnectedToLaneDouble(Line line)
        {
            if (IsConnectedToLane(line, true) && IsConnectedToLane(line, false)) return true;
            else return false;
        }

        private static void UnifyLaneDirection(ref Line lane, List<Lane> iniLanes)
        {
            var line = CreateLine(lane);
            var lanes = iniLanes.Select(e => e.Line).Where(e => IsPerpLine(line, e)).ToList();
            if (lanes.Count > 0)
            {
                if (ClosestPointInCurves(line.EndPoint, lanes) < 1 && ClosestPointInCurves(line.StartPoint, lanes) < 1)
                {
                    if (line.StartPoint.X - line.EndPoint.X > 1000) line.ReverseCurve();
                    else if (lanes.Count == 0 && line.StartPoint.Y - line.EndPoint.Y > 1000) line.ReverseCurve();
                }
                else if (ClosestPointInCurves(line.EndPoint, lanes) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.StartPoint, lanes) > DisCarAndHalfLane + 1) line.ReverseCurve();
                else if (ClosestPointInCurves(line.EndPoint, lanes) < DisCarAndHalfLane + 1 && ClosestPointInCurves(line.StartPoint, lanes) < DisCarAndHalfLane + 1
                    && ClosestPointInCurves(line.EndPoint, lanes) < ClosestPointInCurves(line.StartPoint, lanes)) line.ReverseCurve();
            }
            else
            {
                if (line.StartPoint.X - line.EndPoint.X > 1000) line.ReverseCurve();
                else if (lanes.Count == 0 && line.StartPoint.Y - line.EndPoint.Y > 1000) line.ReverseCurve();
            }
            lane = line;
        }

        private List<Line> SplitLineBySpacialIndexInPoly(Line line, Polyline polyline, ThCADCoreNTSSpatialIndex spatialIndex, bool allow_on_edge = true)
        {
            var crossed = spatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>();
            crossed = crossed.Where(e => Boundary.Contains(e.GetCenter()) || Boundary.Intersect(e,Intersect.OnBothOperands).Count>0);
            var points = new List<Point3d>();
            foreach (var c in crossed)
            {
                points.AddRange(c.Vertices().Cast<Point3d>());
                points.AddRange(c.Intersect(polyline, Intersect.OnBothOperands));
            }
            points = points.Where(p =>
            {
                var conda = polyline.Contains(p);
                if (!allow_on_edge) conda = conda || polyline.GetClosePoint(p).DistanceTo(p) < 1;
                return conda;
            }).Select(p => line.GetClosestPointTo(p, false)).ToList();
            return SplitLine(line, points);
        }

        private List<Line> SplitBufferLineByPoly(Line line, double distance, Polyline cutter)
        {
            var pl = line.Buffer(distance);
            var splits = SplitCurve(cutter, pl);
            if (splits.Count() == 1)
            {
                pl.Dispose();
                return new List<Line> { line };
            }
            splits = splits.Where(e => pl.Contains(e.GetCenter())).ToArray();
            var points = new List<Point3d>();
            foreach (var crv in splits)
                points.AddRange(crv.EntityVertices().Cast<Point3d>());
            points = points.Select(p => line.GetClosestPointTo(p, false)).ToList();
            pl.Dispose();
            return SplitLine(line, points);
        }

        /// <summary>
        /// 判断在准备生成背靠背车道的前方是否有已经存在的平行车道了
        /// </summary>
        /// <param name="line"></param>
        /// <param name="vec"></param>
        /// <param name="maxlength"></param>
        /// <param name="minlength"></param>
        /// <param name="dis_to_move"></param>
        /// <param name="prepLine"></param>
        /// <returns></returns>
        private bool HasParallelLaneForwardExisted(Line line, Vector3d vec, double maxlength, double minlength, ref double dis_to_move
            , ref Line prepLine)
        {
            var lperp = CreateLineFromStartPtAndVector(line.GetCenter().TransformBy(Matrix3d.Displacement(vec * 100)), vec, maxlength);
            var lins = IniLanes.Where(e => IsParallelLine(line, e.Line))
                .Where(e => e.Line.Intersect(lperp, Intersect.OnBothOperands).Count > 0)
                .Where(e => e.Line.Length > line.Length / 3)
                .OrderBy(e => line.GetClosestPointTo(e.Line.GetCenter(), true).DistanceTo(e.Line.GetCenter()))
                .Select(e => e.Line);
            lins = lins.Where(e => e.GetClosestPointTo(line.GetCenter(), true).DistanceTo(line.GetCenter()) > minlength);
            if (lins.Count() == 0) return false;
            else
            {
                var lin = lins.First();
                var dis1 = lin.GetClosestPointTo(line.StartPoint, false)
                    .DistanceTo(lin.GetClosestPointTo(line.StartPoint, true));
                var dis2 = lin.GetClosestPointTo(line.EndPoint, false)
                    .DistanceTo(lin.GetClosestPointTo(line.EndPoint, true));
                if (dis1 + dis2 < line.Length / 2)
                {
                    dis_to_move = lin.GetClosestPointTo(line.GetCenter(), true).DistanceTo(line.GetCenter());
                    prepLine = lperp;
                    return true;
                }
                else return false;
            }
        }

        /// <summary>
        /// 判断在该车道方向上是否需要生成贴近建筑物的车道
        /// </summary>
        /// <param name="line"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        private double IsEssentialToCloseToBuildingOld(Line line, Vector3d vec)
        {
            if (!IsPerpVector(vec, Vector3d.XAxis)) return -1;
            if (vec.Y < 0) return -1;
            var bf = line.Buffer(DisLaneWidth / 2);
            if (ObstaclesSpatialIndex.SelectCrossingPolygon(bf).Count > 0)
            {
                bf.Dispose();
                return -1;
            }
            var linetest = CreateLine(line);
            linetest.TransformBy(Matrix3d.Displacement(vec * MaxLength));
            var pl = CreatPolyFromLines(line, linetest);
            var points = new List<Point3d>();
            points = ObstacleVertexes.Where(e => pl.IsPointInFast(e)).OrderBy(e => line.GetClosestPointTo(e, false).DistanceTo(e)).ToList();
            if (points.Count() == 0) return -1;
            var ltest_ob_near_boundary = CreateLineFromStartPtAndVector(points.First(), vec, DisVertCarLength);
            if (ltest_ob_near_boundary.Intersect(Boundary, Intersect.OnBothOperands).Count > 0) return -1;
            var dist = line.GetClosestPointTo(points.First(), false).DistanceTo(points.First());
            var lperp = CreateLineFromStartPtAndVector(line.GetCenter().TransformBy(Matrix3d.Displacement(vec * 100)), vec, dist + 1);
            var lanes = IniLanes.Where(e => IsParallelLine(e.Line, line))
                .Where(e => e.Line.Intersect(lperp, Intersect.OnBothOperands).Count > 0);
            if (lanes.Count() > 0) return -1;
            var lt = CreateLine(line);
            lt.TransformBy(Matrix3d.Displacement(vec * dist));
            var ltbf = lt.Buffer(DisLaneWidth / 2);
            points = points.Where(e => ltbf.IsPointInFast(e)).OrderBy(e => e.X).ToList();
            if (points.Count() == 0) return -1;
            var length = points.Last().X - points.First().X;
            if (length >= line.Length / 3 || length >= 23000)
                return dist - DisLaneWidth / 2;
            else return -1;
        }

        /// <summary>
        /// 判断在该车道方向上是否需要生成贴近建筑物的车道
        /// </summary>
        /// <param name="line"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        private double IsEssentialToCloseToBuilding(Line line, Vector3d vec)
        {
            if (!GenerateLaneForLayoutingCarsInShearWall) return -1;
            if (!IsPerpVector(vec, Vector3d.XAxis)) return -1;
            if (vec.Y < 0) return -1;
            var bf = line.Buffer(DisLaneWidth / 2);
            if (ObstaclesSpatialIndex.SelectCrossingPolygon(bf).Count > 0)
            {
                bf.Dispose();
                return -1;
            }
            var linetest = CreateLine(line);
            linetest.TransformBy(Matrix3d.Displacement(vec * MaxLength));
            var pl = CreatPolyFromLines(line, linetest);
            var points = new List<Point3d>();
            points = ObstacleVertexes.Where(e => pl.IsPointInFast(e)).OrderBy(e => line.GetClosestPointTo(e, false).DistanceTo(e)).ToList();
            if (points.Count() == 0) return -1;
            var ltest_ob_near_boundary = CreateLineFromStartPtAndVector(points.First(), vec, DisVertCarLength);
            if (ltest_ob_near_boundary.Intersect(Boundary, Intersect.OnBothOperands).Count > 0) return -1;
            var dist = line.GetClosestPointTo(points.First(), false).DistanceTo(points.First());
            var lperp = CreateLineFromStartPtAndVector(line.GetCenter().TransformBy(Matrix3d.Displacement(vec * 100)), vec, dist + 1);
            var lanes = IniLanes.Where(e => IsParallelLine(e.Line, line))
                .Where(e => e.Line.Intersect(lperp, Intersect.OnBothOperands).Count > 0);
            if (lanes.Count() > 0) return -1;
            var lt = CreateLine(line);
            lt.TransformBy(Matrix3d.Displacement(vec * dist));
            var ltbf = lt.Buffer(DisLaneWidth / 2);
            points = points.Where(e => ltbf.IsPointInFast(e)).OrderBy(e => e.X).ToList();
            if (points.Count() == 0) return -1;
            var length = points.Last().X - points.First().X;
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            int offsetcount = 0;
            bool isvalid = false;
            lt.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
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
                var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, new Lane(lt, vec.GetNormal()));
                double validlength = 0;
                vertlanes.ForEach(e => validlength += e.Line.Length);
                if (validlength >= length / 2)
                {
                    isvalid = true;
                    break;
                }
                offsetcount++;
                lt.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * (DisVertCarLength / cyclecount)));
            }
            if (isvalid)
            {
                dist = dist - DisLaneWidth / 2;
                dist = dist - offsetcount * (DisVertCarLength / cyclecount);
                return dist;
            }
            //var length = points.Last().X - points.First().X;
            //if (length >= line.Length / 3 || length >= 23000)
            //    return dist - DisLaneWidth / 2;
            //else return -1;
            return -1;
        }

        private bool CloseToWall(Point3d point)
        {
            if (Walls.Count == 0) return false;
            var dis = ClosestPointInCurvesFast(point, Walls);
            if (dis < 10) return true;
            else return false;
        }

        private double GenerateAdjacentLanesFunc(ref GenerateLaneParas paras, Lane lane, int index, bool isStart)
        {
            double generate_lane_length = -1;
            Point3d pt;
            Point3d ps;
            if (isStart)
            {
                pt = lane.Line.StartPoint;
                ps = pt.TransformBy(Matrix3d.Displacement(CreateVector(lane.Line).GetNormal() * DisCarAndHalfLane));
            }
            else
            {
                pt = lane.Line.EndPoint;
                ps = pt.TransformBy(Matrix3d.Displacement(-CreateVector(lane.Line).GetNormal() * DisCarAndHalfLane));
            }
            var line = CreateLineFromStartPtAndVector(ps, lane.Vec, MaxLength);
            var tmpline = SplitLine(line, Boundary).Where(e => e.Length > 1).First();
            if (Boundary.Contains(tmpline.GetCenter()))
                line = tmpline;
            else return generate_lane_length;
            var gvec = CreateVector(line).GetPerpendicularVector().GetNormal();
            var ptestvec = ps.TransformBy(Matrix3d.Displacement(gvec));
            if (ptestvec.DistanceTo(pt) < DisCarAndHalfLane) gvec = -gvec;
            var distnearbuilding = IsEssentialToCloseToBuilding(line, gvec);
            if (distnearbuilding != -1)
            {
                //贴近建筑物生成
                line.TransformBy(Matrix3d.Displacement(gvec * distnearbuilding));
                //与车道模块相交
                var linesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
                if (IsInAnyBoxes(linesplitcarboxes.GetCenter(), CarBoxes) || linesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                    return generate_lane_length;
                //与障碍物相交
                var plsplitbox = linesplitcarboxes.Buffer(DisLaneWidth / 2);
                plsplitbox.Scale(plsplitbox.GetRecCentroid(), ScareFactorForCollisionCheck);
                var obsplit = SplitLineBySpacialIndexInPoly(linesplitcarboxes, plsplitbox, ObstaclesSpatialIndex, false)
                    .Where(e => e.Length > 1).First();
                if (IsInAnyPolys(obsplit.GetCenter(), Obstacles) || obsplit.Length < LengthCanGAdjLaneConnectSingle)
                    return generate_lane_length;

                if (isStart) paras.SetGStartAdjLane = index;
                else paras.SetGEndAdjLane = index;
                Lane lan = new Lane(obsplit, gvec);
                paras.LanesToAdd.Add(lan);
                paras.LanesToAdd.Add(new Lane(obsplit, -gvec));
                paras.CarBoxesToAdd.Add(CreatePolyFromLine(obsplit));
                generate_lane_length = obsplit.Length;

                return generate_lane_length;
            }
            //与车道模块相交
            var inilinesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
            if (IsInAnyBoxes(inilinesplitcarboxes.GetCenter(), CarBoxes) || inilinesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            var inilinesplitcarboxesaction = CreateLine(inilinesplitcarboxes);
            inilinesplitcarboxesaction.TransformBy(Matrix3d.Displacement(-gvec.GetNormal() * (DisVertCarLength + DisLaneWidth)));
            var inilinesplitcarboxesactionpolyline = CreatPolyFromLines(inilinesplitcarboxes, inilinesplitcarboxesaction);
            var inilinesplitcarboxesactionlaneboxes = IniLanes.Where(e => IsParallelLine(e.Line, inilinesplitcarboxesaction))
                .Select(e => e.Line.Buffer(DisLaneWidth / 2 - 0.001));
            var inilinesplitcarboxesactionpoints = new List<Point3d>();
            foreach (var box in inilinesplitcarboxesactionlaneboxes)
            {
                inilinesplitcarboxesactionpoints.AddRange(box.Vertices().Cast<Point3d>());
                inilinesplitcarboxesactionpoints.AddRange(box.Intersect(inilinesplitcarboxesactionpolyline, Intersect.OnBothOperands));
            }
            inilinesplitcarboxesactionpoints = inilinesplitcarboxesactionpoints
                .Where(e => inilinesplitcarboxesactionpolyline.Contains(e) || inilinesplitcarboxesactionpolyline.GetClosestPointTo(e, false).DistanceTo(e) < 0.0001)
                .Select(e => inilinesplitcarboxes.GetClosestPointTo(e, false)).ToList();
            SortAlongCurve(inilinesplitcarboxesactionpoints, inilinesplitcarboxes);
            if(inilinesplitcarboxesactionpoints.Count > 0)
                if (inilinesplitcarboxes.StartPoint.DistanceTo(inilinesplitcarboxesactionpoints[0]) < 10) return generate_lane_length;
            inilinesplitcarboxes =SplitLine(inilinesplitcarboxes, inilinesplitcarboxesactionpoints).First();
            //与障碍物相交
            var iniplsplitbox = inilinesplitcarboxes.Buffer(DisLaneWidth / 2);
            iniplsplitbox.Scale(iniplsplitbox.GetRecCentroid(), ScareFactorForCollisionCheck);
            var iniobsplit = SplitLineBySpacialIndexInPoly(inilinesplitcarboxes, iniplsplitbox, ObstaclesSpatialIndex, false)
                .Where(e => e.Length > 1).First();
            if (IsInAnyPolys(iniobsplit.GetCenter(), Obstacles) || iniobsplit.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            double dis_to_move = 0;
            Line perpLine = new Line();
            if (HasParallelLaneForwardExisted(iniobsplit, gvec, DisModulus, 1, ref dis_to_move,ref perpLine)) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit) && iniobsplit.Length < LengthCanGAdjLaneConnectDouble) return generate_lane_length;
            var offsetline = CreateLine(iniobsplit);
            offsetline.TransformBy(Matrix3d.Displacement(-gvec * DisCarAndHalfLane));
            var pl = CreatPolyFromLines(iniobsplit, offsetline);
            if (IsInAnyBoxes(pl.GetRecCentroid(), CarBoxes)) return generate_lane_length;
            if (isStart) paras.SetGStartAdjLane = index;
            else paras.SetGEndAdjLane = index;
            Lane inilan = new Lane(iniobsplit, gvec);
            paras.LanesToAdd.Add(inilan);
            paras.CarBoxesToAdd.Add(pl);
            CarModule module = new CarModule(pl, iniobsplit, -gvec);
            paras.CarModulesToAdd.Add(module);
            generate_lane_length = iniobsplit.Length;
            return generate_lane_length;
        }

        //可以使用并行化操作
        public List<Lane> GeneratePerpModuleLanes(double mindistance, double minlength, bool judge_cross_carbox = true
            , Lane specialLane = null)
        {
            var lanes = new List<Lane>();
            var Lanes = IniLanes;
            if (specialLane != null)
                Lanes = new List<Lane>() { specialLane };
            foreach (var lane in Lanes)
            {
                var line = CreateLine(lane.Line);
                var linetest = CreateLine(line);
                linetest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * mindistance));
                var bdpl = CreatPolyFromLines(line, linetest);
                bdpl.Scale(bdpl.GetRecCentroid(), ScareFactorForCollisionCheck);
                var bdpoints = Boundary.Vertices().Cast<Point3d>().ToList();
                bdpoints.AddRange(Boundary.Intersect(bdpl, Intersect.OnBothOperands));
                bdpl.Scale(bdpl.GetRecCentroid(), 1 / (ScareFactorForCollisionCheck - 0.01));
                bdpoints = bdpoints.Where(p => bdpl.IsPointInFast(p)).Select(p => linetest.GetClosestPointTo(p, false)).ToList();
                var bdsplits = SplitLine(linetest, bdpoints).Where(e => Boundary.Contains(e.GetCenter()) || Boundary.GetClosestPointTo(e.GetCenter(), false).DistanceTo(e.GetCenter()) < 1).Where(e => e.Length >= minlength);
                foreach (var bdsplit in bdsplits)
                {
                    bdsplit.TransformBy(Matrix3d.Displacement(-lane.Vec.GetNormal() * mindistance));
                    var bdsplittest = CreateLine(bdsplit);
                    bdsplittest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * mindistance));
                    var obpl = CreatPolyFromLines(bdsplit, bdsplittest);
                    obpl.Scale(obpl.GetRecCentroid(), ScareFactorForCollisionCheck);
                    var obcrossed = ObstaclesSpatialIndex.SelectCrossingPolygon(obpl).Cast<Polyline>().ToList();
                    var obpoints = new List<Point3d>();
                    foreach (var cross in obcrossed)
                    {
                        obpoints.AddRange(cross.Vertices().Cast<Point3d>());
                        obpoints.AddRange(cross.Intersect(obpl, Intersect.OnBothOperands));
                    }
                    obpl.Scale(obpl.GetRecCentroid(), 1 / (ScareFactorForCollisionCheck - 0.01));
                    obpoints = obpoints.Where(p => obpl.IsPointInFast(p)).Select(p => bdsplittest.GetClosestPointTo(p, false)).ToList();
                    var boxsplits = SplitLine(bdsplittest, obpoints).Where(e => !IsInAnyPolys(e.GetCenter(), obcrossed)).Where(e => e.Length >= minlength);
                    foreach (var boxsplit in boxsplits)
                    {
                        boxsplit.TransformBy(Matrix3d.Displacement(-lane.Vec.GetNormal() * mindistance));
                        var boxsplittest = CreateLine(boxsplit);
                        boxsplittest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * mindistance));
                        var boxpl = CreatPolyFromLines(boxsplit, boxsplittest);
                        boxpl.Scale(boxpl.GetRecCentroid(), ScareFactorForCollisionCheck);
                        var boxcrossed = new List<Polyline>();
                        if (judge_cross_carbox)
                            boxcrossed = CarBoxesSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>().ToList();
                        else
                            boxcrossed = CarSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>().ToList();
                        boxpl.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * DisLaneWidth / 2));
                        //boxsplittest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * DisLaneWidth / 2));
                        //boxpl = CreatPolyFromLines(boxsplit, boxsplittest);
                        //boxpl.Scale(boxpl.GetRecCentroid(), ScareFactorForCollisionCheck);
                        boxcrossed.AddRange(LaneSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>());
                        //boxcrossed.AddRange(LaneBufferSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>());
                        foreach (var bx in LaneBufferSpatialIndex.SelectCrossingPolygon(boxpl).Cast<Polyline>())
                        {
                            var pts = bx.Vertices().Cast<Point3d>().ToList();
                            var pta = AveragePoint(pts[0], pts[3]);
                            var ptb = AveragePoint(pts[1], pts[2]);
                            var lin = new Line(pta, ptb);
                            boxcrossed.Add(lin.Buffer(DisLaneWidth / 2));
                            lin.Dispose();
                        }
                        boxsplittest.TransformBy(Matrix3d.Displacement(lane.Vec.GetNormal() * DisLaneWidth / 2));
                        var boxpoints = new List<Point3d>();
                        foreach (var cross in boxcrossed)
                        {
                            boxpoints.AddRange(cross.Vertices().Cast<Point3d>());
                            boxpoints.AddRange(cross.Intersect(boxpl, Intersect.OnBothOperands));
                        }
                        boxpl.Scale(boxpl.GetRecCentroid(), 1 / (ScareFactorForCollisionCheck - 0.01));
                        boxpoints = boxpoints.Where(p => boxpl.IsPointInFast(p)).Select(p => boxsplittest.GetClosestPointTo(p, false)).ToList();
                        var splits = SplitLine(boxsplittest, boxpoints).Where(e => e.Length >= minlength)
                            .Where(e =>
                            {
                                var k = CreateLine(e);
                                k.TransformBy(Matrix3d.Displacement(-lane.Vec.GetNormal() * DisLaneWidth / 2));
                                var con = !IsInAnyBoxes(k.GetCenter(), LaneBoxes, true);
                                k.Dispose();
                                if (con) return true;
                                else return false;
                            });
                        if (judge_cross_carbox)
                        {
                            splits = splits.Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes, true));
                        }
                        splits = splits.Where(e =>
                     {
                         var tlt = new Line(boxsplit.GetClosestPointTo(e.StartPoint, true), boxsplit.GetClosestPointTo(e.EndPoint, true));
                         var plt = CreatPolyFromLines(tlt, e);
                         foreach (var il in boxcrossed)
                         {
                             if (plt.Contains(il.GetCenter())) return false;
                         }
                         return true;
                     });
                        foreach (var split in splits)
                        {
                            split.TransformBy(Matrix3d.Displacement(-lane.Vec.GetNormal() * DisLaneWidth / 2));
                            split.TransformBy(Matrix3d.Displacement(-lane.Vec.GetNormal() * mindistance));
                            if (ClosestPointInVertLines(split.StartPoint, split, IniLanes.Select(e => e.Line)) < 10)
                                split.StartPoint = split.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(split).GetNormal() * DisLaneWidth / 2));
                            if (ClosestPointInVertLines(split.EndPoint, split, IniLanes.Select(e => e.Line)) < 10)
                                split.EndPoint = split.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(split).GetNormal() * DisLaneWidth / 2));
                            if (split.Length < minlength) continue;


                            Lane ln = new Lane(split, lane.Vec);
                            lanes.Add(ln);
                        }
                    }
                }
            }
            return lanes;
        }

        private void GeneratePerpModuleBoxes(List<Lane> lanes)
        {
            SortLaneByDirection(lanes, LayoutMode);
            foreach (var lane in lanes)
            {
                var line = CreateLine(lane.Line);
                List<Line> ilanes = new List<Line>();
                var segs = new DBObjectCollection();
                DivideCurveByLength(line, DisModulus, ref segs);
                ilanes.AddRange(segs.Cast<Line>().Where(t => Math.Abs(t.Length - DisModulus) < 1));
                int modulecount = ilanes.Count;
                int vertcount = ((int)Math.Floor((line.Length - modulecount * DisModulus) / DisVertCarWidth));
                PerpModlues perpModlue = ConstructPerpModules(lane.Vec, ilanes);
                for (int i = 0; i < vertcount; i++)
                {
                    var test = UpdataPerpModlues(perpModlue);
                    if (test.Count >= perpModlue.Count)
                    {
                        perpModlue = test;
                    }
                    else continue;
                }
                foreach (var pl in perpModlue.Bounds)
                {
                    var objs = new DBObjectCollection();
                    pl.Explode(objs);
                    var plsegs = objs.Cast<Line>().ToArray();
                    var a = plsegs[1];
                    var b = plsegs[3];
                    var vec = CreateVector(a.GetCenter(), b.GetClosestPointTo(a.GetCenter(), true));
                    IniLanes.Add(new Lane(a, vec.GetNormal()));
                    CarModule module = new CarModule();
                    module.Box = pl;
                    module.Line = a;
                    module.Vec = vec;
                    CarModules.Add(module);
                }
                CarBoxes.AddRange(perpModlue.Bounds);
                CarBoxesSpatialIndex.Update(perpModlue.Bounds.ToCollection(), new DBObjectCollection());
            }
        }

        private void RemoveDuplicateCars()
        {
            for (int i = 1; i < CarSpots.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (CarSpots[i].GetRecCentroid().DistanceTo(CarSpots[j].GetRecCentroid()) < 1000)
                    {
                        CarSpots.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        private void RemoveCarsIntersectedWithBoundary()
        {
            var obspls = Obstacles.Where(e => e.Closed).Where(e => e.Area > DisVertCarLength * DisLaneWidth * 5).ToList();
            //CarSpots = CarSpots.Where(e =>
            //{
            //    var k = e.Clone() as Polyline;
            //    k.Scale(k.GetRecCentroid(), ScareFactorForCollisionCheck);
            //    var conda = Boundary.Contains(k.GetRecCentroid());
            //    var condb = !IsInAnyPolys(k.GetRecCentroid(), obspls);
            //    var condc = Boundary.Intersect(k, Intersect.OnBothOperands).Count == 0;
            //    if (conda && condb && condc) return true;
            //    else return false;
            //}).ToList();
            var tmps = new List<Polyline>();
            foreach (var e in CarSpots)
            {
                var k = e.Clone() as Polyline;
                k.Scale(k.GetRecCentroid(), ScareFactorForCollisionCheck);
                var conda = Boundary.Contains(k.GetRecCentroid());
                var condb = !IsInAnyPolys(k.GetRecCentroid(), obspls);
                //var condc = Boundary.Intersect(k, Intersect.OnBothOperands).Count == 0;
                //if (conda && condb && condc) tmps.Add(e);
                if (conda && condb)
                {
                    if (Boundary.GetClosestPointTo(k.GetRecCentroid(), false).DistanceTo(k.GetRecCentroid()) > DisVertCarLength)
                        tmps.Add(e);
                    else if (Boundary.Intersect(k, Intersect.OnBothOperands).Count == 0)
                        tmps.Add(e);
                }
            }
            CarSpots = tmps;
        }

        private void RemoveInvalidPillars()
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //Pillars = Pillars.Where(t =>
            //{
            //    var clone = t.Clone() as Polyline;
            //    clone.Scale(clone.GetRecCentroid(), 0.5);
            //    if (ClosestPointInCurvesFast(clone.GetRecCentroid(), CarSpots) > DisPillarLength + DisHalfCarToPillar)
            //    {
            //        clone.Dispose();
            //        return false;
            //    }
            //    clone.Dispose();
            //    return true;
            //}).ToList();
            List<Polyline> tmps = new List<Polyline>();
            foreach (var t in Pillars)
            {
                var clone = t.Clone() as Polyline;
                clone.Scale(clone.GetRecCentroid(), 0.5);
                if (ClosestPointInCurveInAllowDistance(clone.GetRecCentroid(), CarSpots, DisPillarLength + DisHalfCarToPillar))
                {
                    clone.Dispose();
                    tmps.Add(t);
                }
                else
                    clone.Dispose();
            }
            Pillars = tmps;
            //List<Polyline> tmps = new List<Polyline>();
            //Pillars.AsParallel().ForAll(t =>
            //{
            //    var clone = t.Clone() as Polyline;
            //    clone.Scale(clone.GetRecCentroid(), 0.5);
            //    if (ClosestPointInCurvesFast(clone.GetRecCentroid(), CarSpots) > DisPillarLength + DisHalfCarToPillar)
            //        clone.Dispose();
            //    else
            //    {
            //        clone.Dispose();
            //        tmps.Add(t);
            //    }
            //});
            //Pillars = tmps;
            //sw.Stop();
            //var time = sw.Elapsed.TotalMilliseconds.ToString();
            //Active.Editor.WriteMessage("test：" + time + "\n");
        }

        private void ReDefinePillarDimensions()
        {
            if (HasImpactOnDepthForPillarConstruct)
            {
                Pillars = Pillars.Select(e =>
                  {
                      var segobjs = new DBObjectCollection();
                      e.Explode(segobjs);
                      if (DisPillarLength < DisPillarDepth)
                      {
                          double t = DisPillarDepth;
                          DisPillarDepth = DisPillarLength;
                          DisPillarLength = t;
                          t = PillarNetLength;
                          PillarNetLength = PillarNetDepth;
                          PillarNetDepth = t;
                      }
                      var segs = segobjs.Cast<Line>().OrderByDescending(t => t.Length).ToList();
                      if (DisPillarLength < DisPillarDepth)
                          segs = segobjs.Cast<Line>().OrderBy(t => t.Length).ToList();
                      Line a = new Line();
                      Line b = new Line();
                      if (DisPillarLength != DisPillarDepth)
                      {
                          a = segs[0];
                          b = segs[1];
                      }
                      else
                      {
                          a=segs[0];
                          segs.RemoveAt(0);
                          b = segs.Where(t => IsParallelLine(t, a)).First();
                      }
                      b.ReverseCurve();
                      a.Scale(a.GetCenter(), PillarNetLength / a.Length);
                      b.Scale(b.GetCenter(), PillarNetLength / b.Length);
                      a.TransformBy(Matrix3d.Displacement(CreateVector(a.GetCenter(), b.GetCenter()).GetNormal() * ThicknessOfPillarConstruct));
                      b.TransformBy(Matrix3d.Displacement(-CreateVector(a.GetCenter(), b.GetCenter()).GetNormal() * ThicknessOfPillarConstruct));
                      var pl = CreatPolyFromLines(a, b);
                      e.Dispose();
                      a.Dispose();
                      b.Dispose();
                      return pl;
                  }).ToList();
            }
        }

        private PerpModlues ConstructPerpModules(Vector3d vec, List<Line> ilanes)
        {
            PerpModlues result = new PerpModlues();
            int count = 0;
            int minindex = 0;
            int mincount = 9999;
            List<Polyline> plys = new List<Polyline>();
            vec = vec.GetNormal() * MaxLength;
            for (int i = 0; i < ilanes.Count; i++)
            {
                int mintotalcount = 7;
                var unitbase = ilanes[i];
                int generatedcount = 0;
                var tmpplycount = plys.Count;
                var curcount = GenerateUsefulModules(unitbase, vec, plys, ref generatedcount);
                if (plys.Count > tmpplycount)
                {
                    var pl = plys[plys.Count - 1];
                    var segs = new DBObjectCollection();
                    pl.Explode(segs);
                    var lane = (Line)segs[3];

                    if (ClosestPointInVertLines(lane.StartPoint, lane, IniLanes.Select(e => e.Line)) <= DisLaneWidth / 2
                        && ClosestPointInVertLines(lane.EndPoint, lane, IniLanes.Select(e => e.Line)) <= DisLaneWidth / 2)
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
            return result;
        }

        private PerpModlues UpdataPerpModlues(PerpModlues perpModlues)
        {
            PerpModlues result;
            int minindex = perpModlues.Mminindex;
            List<Line> ilanes = new List<Line>(perpModlues.Lanes);
            if (perpModlues.Lanes.Count == 0) return perpModlues;
            var vec_move = CreateVector(perpModlues.Lanes[0]).GetNormal() * DisVertCarWidth;
            for (int i = 0; i < ilanes.Count; i++)
            {
                if (i >= minindex)
                {
                    ilanes[i].TransformBy(Matrix3d.Displacement(vec_move));
                }
            }
            result = ConstructPerpModules(perpModlues.Vec, ilanes);
            return result;
        }

        private int GenerateUsefulModules(Line lane, Vector3d vec, List<Polyline> plys, ref int generatedcount)
        {
            int count = 0;
            Line unittest = CreateLine(lane);
            unittest.TransformBy(Matrix3d.Displacement(vec.GetNormal() * MaxLength));
            var pltest = CreatePolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, unittest.EndPoint, unittest.StartPoint });
            var pltestsc = pltest.Clone() as Polyline;
            pltestsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pltestsc.GetRecCentroid()));
            var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pltestsc).Cast<Polyline>().ToList();
            crossed.AddRange(CarBoxesSpatialIndex.SelectCrossingPolygon(pltestsc).Cast<Polyline>());
            crossed.Add(Boundary);
            List<Point3d> points = new List<Point3d>();
            foreach (var o in crossed)
            {
                points.AddRange(o.Vertices().Cast<Point3d>().ToArray());
                points.AddRange(o.Intersect(pltest, Intersect.OnBothOperands));
                var splitselves = new List<Curve>();
                try
                {
                    //points.AddRange(SplitCurve(o, pltestsc).Select(e => e.GetPointAtParameter(e.EndParam / 2)));
                    splitselves = SplitCurve(o, pltestsc).Where(f =>
                    {
                        if (f is Line) return pltestsc.Contains(((Line)f).GetCenter());
                        else if (f is Polyline) return pltestsc.Contains(((Polyline)f).GetRecCentroid());
                        else return false;
                    }).ToList();
                }
                catch
                {
                }
                foreach (var sp in splitselves)
                {
                    if (sp is Line) points.Add(((Line)sp).GetCenter());
                    else if (sp is Polyline)
                    {
                        var segs = new DBObjectCollection();
                        ((Polyline)sp).Explode(segs);
                        points.AddRange(segs.Cast<Line>().Select(f => f.GetCenter()));
                    }
                }
            }
            points = points.Where(e => pltest.IsPointInFast(e) || pltest.GetClosePoint(e).DistanceTo(e) < 1).Distinct().ToList();
            Line edgea = new Line(lane.StartPoint, unittest.StartPoint);
            Line edgeb = new Line(lane.EndPoint, unittest.EndPoint);
            var pointsa = points.Where(e => edgea.GetClosestPointTo(e, false).DistanceTo(e) <
                    DisVertCarLength + DisLaneWidth).OrderBy(p => edgea.GetClosestPointTo(p, false).DistanceTo(lane.StartPoint)).ToList();
            var pointsb = points.Where(e => edgeb.GetClosestPointTo(e, false).DistanceTo(e) <
                      DisVertCarLength + DisLaneWidth).OrderBy(p => edgeb.GetClosestPointTo(p, false).DistanceTo(lane.EndPoint)).ToList();
            for (int i = 0; i < pointsa.Count - 1; i++)
            {
                if (edgea.GetClosestPointTo(pointsa[i], false).DistanceTo(pointsa[i]) < 1)
                {
                    pointsa.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < pointsb.Count - 1; i++)
            {
                if (edgeb.GetClosestPointTo(pointsb[i], false).DistanceTo(pointsb[i]) < 1)
                {
                    pointsb.RemoveAt(i);
                    i--;
                }
            }
            pointsa = pointsa.Select(e => edgea.GetClosestPointTo(e, false)).ToList();
            pointsb = pointsb.Select(e => edgeb.GetClosestPointTo(e, false)).ToList();
            Point3d pta;
            Point3d ptb;
            if (pointsa.ToArray().Length == 0) pta = lane.StartPoint;
            else pta = pointsa.Where(e => e.DistanceTo(lane.StartPoint) > 1).OrderBy(e => e.DistanceTo(lane.StartPoint)).First();
            if (pointsb.ToArray().Length == 0) ptb = lane.StartPoint;
            else ptb = pointsb.Where(e => e.DistanceTo(lane.EndPoint) > 1).OrderBy(e => e.DistanceTo(lane.EndPoint)).First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.GetClosestPointTo(pta, false).DistanceTo(pta);
                if (disa < DisLaneWidth / 2)
                {
                    pta = pta.TransformBy(Matrix3d.Displacement(CreateVector(pta, lane.StartPoint).GetNormal() * (DisLaneWidth / 2 - disa)));
                }
                var disb = la.Line.GetClosestPointTo(ptb, false).DistanceTo(ptb);
                if (disb < DisLaneWidth / 2)
                {
                    ptb = ptb.TransformBy(Matrix3d.Displacement(CreateVector(ptb, lane.EndPoint).GetNormal() * (DisLaneWidth / 2 - disb)));
                }
            }
            Line eb = new Line(lane.EndPoint, ptb);
            Line ea = new Line(lane.StartPoint, pta);
            count += ((int)Math.Floor((ea.Length - DisLaneWidth / 2) / DisVertCarWidth));
            count += ((int)Math.Floor((eb.Length - DisLaneWidth / 2) / DisVertCarWidth));
            var pa = CreatePolyFromPoints(new Point3d[] { lane.StartPoint, lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),
                pta.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)), pta });
            if (pa.Area > 0)
            {
                if (ClosestPointInVertCurves(ea.StartPoint, ea, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    Math.Abs(ClosestPointInVertCurves(ea.EndPoint, ea, IniLanes.Select(e => e.Line).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
                    ea.Length < DisLaneWidth / 2 + DisVertCarWidth * 5)
                {
                    pa.Dispose();
                    count = 0;
                }
                else
                {
                    plys.Add(pa);
                    generatedcount++;
                }
            }
            var pb = CreatePolyFromPoints(new Point3d[] { lane.EndPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),
                 ptb.TransformBy(Matrix3d.Displacement(-CreateVector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),ptb});
            if (pb.Area > 0)
            {
                if (ClosestPointInVertCurves(eb.StartPoint, eb, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    Math.Abs(ClosestPointInVertCurves(eb.EndPoint, eb, IniLanes.Select(e => e.Line).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
    eb.Length < DisLaneWidth / 2 + DisVertCarWidth * 5)
                {
                    pb.Dispose();
                    count = 0;
                }
                else
                {
                    plys.Add(pb);
                    generatedcount++;
                }
            }
            unittest.Dispose();
            pltest.Dispose();
            pltestsc.Dispose();
            ea.Dispose();
            eb.Dispose();
            return count;
        }

        public void GenerateCarsAndPillarsForEachLane(Line line, Vector3d vec, double length_divided, double length_offset,
          bool add_to_car_spacialindex = true, bool judge_carmodulebox = true, bool adjust_pillar_edge = false, bool judge_modulebox = false,
          bool gfirstpillar = true, bool allow_pillar_in_wall = false, bool align_back_to_back = true, bool judge_in_obstacles = false, bool glastpillar = true, bool judge_intersect_bound = false,
          bool generate_middle_pillar = false, bool isin_backback = false)
        {
            int inipillar_count = Pillars.Count;
            //允许柱子穿墙
            if (allow_pillar_in_wall && GeneratePillars && Obstacles.Count > 0)
            {
                double dis_judge_under_building = 5000;
                if (ClosestPointInCurvesFast(line.StartPoint, Obstacles) < dis_judge_under_building)
                {
                    var dis = ClosestPointInVertCurves(line.StartPoint, line, IniLanes.Select(e => e.Line).ToList());
                    if (dis >= DisLaneWidth + DisPillarLength - 1 && Math.Abs(dis - DisCarAndHalfLane) > 1)
                        line = new Line(line.StartPoint.TransformBy(Matrix3d.Displacement(-CreateVector(line).GetNormal() * DisPillarLength)), line.EndPoint);
                    else if (line.Length < DisVertCarWidth * 4)
                        line = new Line(line.StartPoint.TransformBy(Matrix3d.Displacement(-CreateVector(line).GetNormal() * DisPillarLength)), line.EndPoint);
                }
            }
            if (line.Length == 0) return;
            //背靠背对齐
            if (Math.Abs(length_divided - DisVertCarWidth) < 1 && align_back_to_back)
            {
                double dis_judge_in_backtoback = 20000;
                var pts = line.StartPoint.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisVertCarLength * 1.5));
                var pte = pts.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * dis_judge_in_backtoback));
                var tl = new Line(pts, pte);
                Polyline tlbf = tl.Buffer(1);
                var crosscars = CarSpatialIndex.SelectCrossingPolygon(tlbf).Cast<Polyline>().OrderBy(t => t.GetRecCentroid().DistanceTo(pts)).ToList();
                if (crosscars.Count > 1)
                {
                    for (int i = 1; i < crosscars.Count; i++)
                    {
                        if (Math.Abs(crosscars[i].GetRecCentroid().DistanceTo(crosscars[i - 1].GetRecCentroid()) - DisVertCarWidth - DisPillarLength) < 10)
                        {
                            var p = crosscars[i].Vertices().Cast<Point3d>().OrderBy(t => t.DistanceTo(line.StartPoint)).First();
                            var ponline_ex = line.GetClosestPointTo(p, true);
                            var dis = ponline_ex.DistanceTo(line.StartPoint) % (DisVertCarWidth * CountPillarDist + DisPillarLength);
                            line = new Line(line.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * (dis - DisPillarLength))), line.EndPoint);
                            break;
                        }
                    }
                }
            }
            //长度划分
            var segobjs = new DBObjectCollection();
            Line[] segs;
            if (GeneratePillars)
            {
                var dividecount = Math.Abs(length_divided - DisVertCarWidth) < 1 ? CountPillarDist : 1;
                //DivideCurveByDifferentLength(line, ref segobjs, DisPillarLength, 1, length_divided, dividecount);
                DivideCurveByKindsOfLength(line, ref segobjs, DisPillarLength, 1, DisHalfCarToPillar, 1,
                    length_divided, dividecount, DisHalfCarToPillar, 1);
            }
            else
            {
                DivideCurveByLength(line, length_divided, ref segobjs);
            }
            segs = segobjs.Cast<Line>().Where(e => Math.Abs(e.Length - length_divided) < 1).ToArray();
            var precar = new Polyline();
            int segscount = segs.Count();
            int c = 0;
            foreach (var seg in segs)
            {
                c++;
                var s = CreateLine(seg);
                s.TransformBy(Matrix3d.Displacement(vec.GetNormal() * (length_offset)));
                var car = CreatePolyFromPoints(new Point3d[] { seg.StartPoint, seg.EndPoint, s.EndPoint, s.StartPoint });
                var carsc = car.Clone() as Polyline;
                carsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, carsc.GetRecCentroid()));
                var cond = ObstaclesSpatialIndex.SelectCrossingPolygon(carsc).Count == 0;
                if (judge_carmodulebox)
                {
                    cond = cond && (!IsInAnyPolys(carsc.GetRecCentroid(), CarBoxes))
                        && CarBoxesSpatialIndex.SelectCrossingPolygon(carsc).Count == 0;
                }
                else
                    cond = cond && CarSpatialIndex.SelectCrossingPolygon(carsc).Count == 0;
                if (cond)
                {
                    if (add_to_car_spacialindex)
                        AddToSpatialIndex(car, ref CarBoxesSpatialIndex);
                    AddToSpatialIndex(car, ref CarSpatialIndex);
                    CarSpots.Add(car);
                    if (Pillars.Count > 0)
                    {
                        if (car.GeometricExtents.IsPointIn(Pillars[Pillars.Count - 1].GetRecCentroid()))
                        {
                            Pillars.RemoveAt(Pillars.Count - 1);
                        }
                    }
                    //如果是生成该车道上的第一个车位，判断是否需要在前方生成柱子，如果需要则生成
                    if (precar.Area == 0)
                    {
                        if (gfirstpillar && GeneratePillars)
                        {
                            var ed = seg;
                            if (adjust_pillar_edge)
                            {
                                ed = s;
                                vec = -vec;
                            }
                            var pp = ed.StartPoint.TransformBy(Matrix3d.Displacement(-CreateVector(ed).GetNormal() * DisPillarLength));
                            var li = new Line(pp, ed.StartPoint);
                            var lf = CreateLine(li);
                            lf.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                            var pillar = CreatePolyFromPoints(new Point3d[] { li.StartPoint, li.EndPoint, lf.EndPoint, lf.StartPoint });
                            pillar.TransformBy(Matrix3d.Displacement(-CreateVector(ed).GetNormal() * DisHalfCarToPillar));
                            if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                            {
                                bool condg = true;
                                if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 2].IsPointInFast(pillar.GetRecCentroid()))
                                    condg = false;
                                if (condg)
                                {
                                    //AddToSpatialIndex(pillar, ref CarSpatialIndex);                           
                                    if (isin_backback)
                                        pillar.TransformBy(Matrix3d.Displacement(CreateVector(new Line(li.StartPoint, lf.StartPoint)).GetNormal() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2)));
                                    else
                                        pillar.TransformBy(Matrix3d.Displacement(CreateVector(new Line(li.StartPoint, lf.StartPoint)).GetNormal() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2)));
                                    Pillars.Add(pillar);
                                }
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                            li.Dispose();
                            lf.Dispose();
                        }
                        precar = car;
                    }
                    else
                    {
                        var dist = car.GetRecCentroid().DistanceTo(precar.GetRecCentroid());
                        if (Math.Abs(dist - length_divided - DisPillarLength - DisHalfCarToPillar * 2) < 1 && GeneratePillars)
                        {
                            var ed = seg;
                            if (adjust_pillar_edge)
                            {
                                ed = s;
                                vec = -vec;
                            }
                            var pp = precar.GetClosestPointTo(ed.StartPoint, false);
                            var li = new Line(pp, ed.StartPoint);
                            li.EndPoint = pp.TransformBy(Matrix3d.Displacement(CreateVector(li).GetNormal() * DisPillarLength));
                            var lf = CreateLine(li);
                            lf.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                            var pillar = CreatePolyFromPoints(new Point3d[] { li.StartPoint, li.EndPoint, lf.EndPoint, lf.StartPoint });
                            pillar.TransformBy(Matrix3d.Displacement(CreateVector(ed).GetNormal() * DisHalfCarToPillar));
                            if (isin_backback)
                                pillar.TransformBy(Matrix3d.Displacement(CreateVector(new Line(li.StartPoint, lf.StartPoint)).GetNormal() * (DisPillarMoveDeeplyBackBack-DisPillarDepth/2)));
                            else
                                pillar.TransformBy(Matrix3d.Displacement(CreateVector(new Line(li.StartPoint, lf.StartPoint)).GetNormal() * (DisPillarMoveDeeplySingle-DisPillarDepth/2)));
                            if (Math.Abs(pillar.Area - DisPillarDepth * DisPillarLength) < 1)
                            {
                                if (add_to_car_spacialindex)
                                    AddToSpatialIndex(pillar, ref CarBoxesSpatialIndex);
                                Pillars.Add(pillar);
                                AddToSpatialIndex(pillar, ref CarSpatialIndex);
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                            li.Dispose();
                            lf.Dispose();
                        }
                        else { }
                        precar = car;
                    }
                    //判断是否需要生成最后一个柱子
                    if (glastpillar && c == segscount && GeneratePillars)
                    {
                        var ed = seg;
                        if (adjust_pillar_edge)
                        {
                            ed = s;
                            vec = -vec;
                        }
                        var pp = ed.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(ed).GetNormal() * DisPillarLength));
                        var li = new Line(pp, ed.EndPoint);
                        var lf = CreateLine(li);
                        lf.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                        var pillar = CreatePolyFromPoints(new Point3d[] { li.StartPoint, li.EndPoint, lf.EndPoint, lf.StartPoint });
                        pillar.TransformBy(Matrix3d.Displacement(CreateVector(ed).GetNormal() * DisHalfCarToPillar));
                        if (isin_backback)
                            pillar.TransformBy(Matrix3d.Displacement(CreateVector(new Line(li.StartPoint, lf.StartPoint)).GetNormal() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2)));
                        else
                            pillar.TransformBy(Matrix3d.Displacement(CreateVector(new Line(li.StartPoint, lf.StartPoint)).GetNormal() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2)));
                        if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                        {
                            bool condg = true;
                            if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 1].IsPointInFast(pillar.GetRecCentroid()))
                                condg = false;
                            if (condg)
                            {
                                Pillars.Add(pillar);
                            }
                        }
                        if (adjust_pillar_edge)
                        {
                            vec = -vec;
                        }
                        li.Dispose();
                        lf.Dispose();
                    }
                }
                carsc.Dispose();
                seg.Dispose();
                s.Dispose();
            }
            segobjs.Dispose();
            //判断是否需要生成中柱
            if (generate_middle_pillar)
            {
                var middle_pillars = new List<Polyline>();
                for (int i = inipillar_count; i < Pillars.Count; i++)
                {
                    var p = Pillars[i].Clone() as Polyline;
                    double dist = DisVertCarLength - DisPillarMoveDeeplyBackBack;
                    p.TransformBy(Matrix3d.Displacement(vec.GetNormal() * dist));
                    var pp = p.Clone() as Polyline;
                    pp.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisPillarDepth));
                    var pisegs = new DBObjectCollection();
                    pp.Explode(pisegs);
                    var piseg = pisegs.Cast<Line>().Where(e => IsPerpVector(CreateVector(e), vec)).First();
                    piseg.Scale(piseg.GetCenter(), (DisHalfCarToPillar * 2 + DisPillarLength) / DisPillarLength + 1);
                    var buffer = piseg.Buffer(1);
                    if (CarSpatialIndex.SelectCrossingPolygon(buffer).Count > 0)
                        middle_pillars.Add(p);
                }
                Pillars.AddRange(middle_pillars);
            }
        }
    }
}