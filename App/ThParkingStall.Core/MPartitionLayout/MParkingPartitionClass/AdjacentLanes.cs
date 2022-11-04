using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
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
        private double GenerateAdjacentLanesOptimizedByRealLength(ref GenerateLaneParas paras)
        {
            double generate_lane_length;
            double max_length = -1;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras = new GenerateLaneParas();
                var length = GenerateAdjacentLanesForUniqueLaneOptimizedByRealLength(ref _paras, i);
                switch (LayoutMode)
                {
                    case 0:
                        {
                            if (length > max_length)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            break;
                        }
                    case 1:
                        {
                            if (length > 0 && _paras.LanesToAdd.Count > 0 && !IsHorizontalLine(_paras.LanesToAdd[0].Line))
                            {
                                length = length * LayoutScareFactor_Adjacent;
                            }
                            if (length > max_length)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            break;
                        }
                    case 2:
                        {
                            if (length > 0 && _paras.LanesToAdd.Count > 0 && !IsVerticalLine(_paras.LanesToAdd[0].Line))
                            {
                                length = length * LayoutScareFactor_Adjacent;
                            }
                            if (length > max_length)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            break;
                        }
                }
            }
            generate_lane_length = max_length;
            return generate_lane_length;
        }
        private double GenerateAdjacentLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i)
        {
            double generate_lane_length = -1;
            var lane = IniLanes[i];
            if (lane.Line.Length <= LengthCanGAdjLaneConnectSingle) return generate_lane_length;
            if (CloseToWall(lane.Line.P0, lane.Line) && !lane.GStartAdjLine)
            {
                var generated = GenerateAdjacentLanesFunc(ref paras, lane, i, true);
                if (generated != -1)
                {
                    return generated;
                }
            }
            if (CloseToWall(lane.Line.P1, lane.Line) && !lane.GEndAdjLine)
            {
                var generated = GenerateAdjacentLanesFunc(ref paras, lane, i, false);
                if (generated != -1)
                {
                    return generated;
                }
            }
            return generate_lane_length;
        }
        private double GenerateAdjacentLanesFunc(ref GenerateLaneParas paras, Lane lane, int index, bool isStart)
        {
            double generate_lane_length = -1;
            Coordinate pt;
            Coordinate ps;

            //调整车道方向和计算生成点
            if (isStart)
            {
                pt = lane.Line.P0;
                ps = pt.Translation(Vector(lane.Line).Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
            }
            else
            {
                pt = lane.Line.P1;
                ps = pt.Translation(-Vector(lane.Line).Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
            }
            var line = LineSegmentSDL(ps, lane.Vec, MaxLength);
            var tmpline = SplitLine(line, Boundary).Where(e => e.Length > 1).First();
            if (Boundary.Contains(tmpline.MidPoint))
                line = tmpline;
            else return generate_lane_length;

            //gevc:远离墙线一方向的向量
            var gvec = Vector(line).GetPerpendicularVector().Normalize();
            var ptestvec = ps.Translation(gvec);
            if (ptestvec.Distance(pt) < (DisCarAndHalfLane + CollisionD - CollisionTOP)) gvec = -gvec;
            STRtree<Polygon> carBoxesStrTree = new STRtree<Polygon>();
            CarBoxes.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));

            #region 贴近建筑物的插车处理模块，已弃用————模块功能没有大问题挺好用，为迭代算法更快找到优解，在小分区中就不要这么智能于是注释掉了
            //var distnearbuilding = IsEssentialToCloseToBuilding(line, gvec);
            //if (distnearbuilding != -1)
            //{
            //    //贴近建筑物生成
            //    line = line.Translation(gvec * distnearbuilding);
            //    //与车道模块相交
            //    var linesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
            //    if (IsInAnyBoxes(linesplitcarboxes.MidPoint, carBoxesStrTree) || linesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
            //        return generate_lane_length;
            //    //与障碍物相交
            //    var plsplitbox = linesplitcarboxes.Buffer(DisLaneWidth / 2);
            //    plsplitbox = plsplitbox.Scale(ScareFactorForCollisionCheck);
            //    var obsplit = SplitLineBySpacialIndexInPoly(linesplitcarboxes, plsplitbox, ObstaclesSpatialIndex, false)
            //        .Where(e => e.Length > 1).First();
            //    if (obsplit.Length < LengthCanGAdjLaneConnectSingle)
            //        return generate_lane_length;
            //    var _tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(obsplit.MidPoint)).Cast<Polygon>().ToList();
            //    if (IsInAnyPolys(obsplit.MidPoint, _tmpobs))
            //        return generate_lane_length;

            //    //解决车道线靠墙的方向有车道线的情况
            //    var _line_to_wall = line.Translation(-gvec.Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
            //    var _wall_buffer = _line_to_wall.Buffer(/*DisLaneWidth / 2 - 1*/DisModulus + DisLaneWidth);
            //    var _wall_crossed_lanes_points = new List<Coordinate>();
            //    foreach (var lane_to_wall in IniLanes.Where(e => IsParallelLine(e.Line, line)).Select(e => e.Line.Buffer(DisLaneWidth / 2 - 1)))
            //    {
            //        _wall_crossed_lanes_points.AddRange(lane_to_wall.IntersectPoint(_wall_buffer));
            //    }
            //    _wall_crossed_lanes_points = _wall_crossed_lanes_points.Select(p => line.ClosestPoint(p)).ToList();
            //    _wall_crossed_lanes_points = SortAlongCurve(_wall_crossed_lanes_points, line.ToLineString());
            //    _wall_crossed_lanes_points = RemoveDuplicatePts(_wall_crossed_lanes_points);
            //    if (_wall_crossed_lanes_points.Count > 0)
            //    {
            //        if (_wall_crossed_lanes_points.Count == 2
            //            && Math.Abs(new LineSegment(_wall_crossed_lanes_points.First(), _wall_crossed_lanes_points.Last()).Length - obsplit.Length) < 1)
            //            return generate_lane_length;
            //        var line_to_wall_split = SplitLine(line, _wall_crossed_lanes_points).First();
            //        if (line_to_wall_split.Length < obsplit.Length)
            //            obsplit = line_to_wall_split;
            //    }
            //    if (obsplit.Length < DisVertCarLength) return generate_lane_length;

            //    if (isStart) paras.SetGStartAdjLane = index;
            //    else paras.SetGEndAdjLane = index;
            //    Lane lan = new Lane(obsplit, gvec);
            //    paras.LanesToAdd.Add(lan);
            //    paras.LanesToAdd.Add(new Lane(obsplit, -gvec));
            //    paras.CarBoxesToAdd.Add(PolyFromLine(obsplit));
            //    generate_lane_length = obsplit.Length;

            //    return generate_lane_length;
            //}
            #endregion

            #region 解决车道线靠墙的方向有车道线的情况及CarBoxes车道模块相交分割处理
            //与车道模块相交
            var inilinesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
            //解决车道线靠墙的方向有车道线的情况
            var line_to_wall = line.Translation(-gvec.Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
            var wall_buffer = line_to_wall.Buffer(/*DisLaneWidth / 2 - 1*/DisModulus /*+ DisLaneWidth*/);
            var wall_crossed_lanes_points = new List<Coordinate>();
            foreach (var lane_to_wall in IniLanes.Where(e => IsParallelLine(e.Line, line)).Select(e => e.Line.Buffer(DisLaneWidth / 2 - 1)))
            {
                wall_crossed_lanes_points.AddRange(lane_to_wall.IntersectPoint(wall_buffer));
                wall_crossed_lanes_points.AddRange(lane_to_wall.Coordinates.Where(p => wall_buffer.Contains(p)));
            }
            wall_crossed_lanes_points = wall_crossed_lanes_points.Select(p => line.ClosestPoint(p)).ToList();
            wall_crossed_lanes_points = SortAlongCurve(wall_crossed_lanes_points, line.ToLineString());
            wall_crossed_lanes_points = RemoveDuplicatePts(wall_crossed_lanes_points);
            if (wall_crossed_lanes_points.Count == 2
                && Math.Abs(new LineSegment(wall_crossed_lanes_points.First(), wall_crossed_lanes_points.Last()).Length - inilinesplitcarboxes.Length) < 1)
                return generate_lane_length;
            if (wall_crossed_lanes_points.Count > 0)
            {
                var line_to_wall_split = SplitLine(line, wall_crossed_lanes_points).First();
                if (line_to_wall_split.Length < inilinesplitcarboxes.Length)
                    inilinesplitcarboxes = line_to_wall_split;
            }
            if (inilinesplitcarboxes.Length < DisVertCarLength) return generate_lane_length;
            #endregion

            #region 与既有车道线做一次相交分割处理
            if (IsInAnyBoxes(inilinesplitcarboxes.MidPoint, carBoxesStrTree) || inilinesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            var inilinesplitcarboxesaction = new LineSegment(inilinesplitcarboxes);
            inilinesplitcarboxesaction = inilinesplitcarboxesaction.Translation(-gvec.Normalize() * (DisVertCarLength + DisLaneWidth));
            var inilinesplitcarboxesactionpolyline = PolyFromLines(inilinesplitcarboxes, inilinesplitcarboxesaction);
            var inilinesplitcarboxesactionlaneboxes = IniLanes.Where(e => IsParallelLine(e.Line, inilinesplitcarboxesaction))
                .Select(e => e.Line.Buffer(DisLaneWidth / 2));
            var inilinesplitcarboxesactionpoints = new List<Coordinate>();
            foreach (var box in inilinesplitcarboxesactionlaneboxes)
            {
                inilinesplitcarboxesactionpoints.AddRange(box.Coordinates);
                inilinesplitcarboxesactionpoints.AddRange(box.IntersectPoint(inilinesplitcarboxesactionpolyline));
            }
            inilinesplitcarboxesactionpoints = inilinesplitcarboxesactionpoints
                .Where(e => inilinesplitcarboxesactionpolyline.Scale(ScareFactorForCollisionCheck).Contains(e) /*|| inilinesplitcarboxesactionpolyline.ClosestPoint(e).Distance(e) < 0.0001*/)
                .Select(e => inilinesplitcarboxes.ClosestPoint(e)).ToList();
            SortAlongCurve(inilinesplitcarboxesactionpoints, inilinesplitcarboxes);
            if (inilinesplitcarboxesactionpoints.Count > 0)
                if (inilinesplitcarboxes.P0.Distance(inilinesplitcarboxesactionpoints[0]) < 10) return generate_lane_length;
            inilinesplitcarboxes = SplitLine(inilinesplitcarboxes, inilinesplitcarboxesactionpoints).First();
            #endregion


            //与障碍物相交
            var iniplsplitbox = inilinesplitcarboxes.Buffer(DisLaneWidth / 2);
            iniplsplitbox = iniplsplitbox.Scale(ScareFactorForCollisionCheck);
            var iniobsplit = SplitLineBySpacialIndexInPoly(inilinesplitcarboxes, iniplsplitbox, ObstaclesSpatialIndex, false)
                .Where(e => e.Length > 1).First();
            if (iniobsplit.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            var tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(iniobsplit.MidPoint)).Cast<Polygon>().ToList();
            if (IsInAnyPolys(iniobsplit.MidPoint, tmpobs))
                return generate_lane_length;

            //判断是否重复车道线
            var quit_repeat = false;
            foreach (var l in IniLanes.Select(e => e.Line))
            {
                var dis_start = l.ClosestPoint(iniobsplit.P0).Distance(iniobsplit.P0);
                var dis_end = l.ClosestPoint(iniobsplit.P1).Distance(iniobsplit.P1);
                if (IsParallelLine(l, iniobsplit) && dis_start < DisLaneWidth / 2 && dis_end < DisLaneWidth / 2)
                {
                    quit_repeat = true;
                    break;
                }
            }
            if (quit_repeat) return generate_lane_length;

            //附条件判断
            double dis_to_move = 0;
            var perpLine = new LineSegment();
            double dis_connected_double = 0;
            var para_lanes_add = new List<LineSegment>();
            if (HasParallelLaneForwardExisted(iniobsplit, gvec, DisModulus, 1, ref dis_to_move, ref perpLine, ref para_lanes_add)) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit) && iniobsplit.Length < LengthCanGAdjLaneConnectDouble) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit))
            {
                dis_connected_double = DisCarAndHalfLane;
            }
            var offsetline = new LineSegment(iniobsplit);
            offsetline = offsetline.Translation(-gvec * DisCarAndHalfLane);
            var pl = PolyFromLines(iniobsplit, offsetline);
            if (IsInAnyBoxes(pl.Envelope.Centroid.Coordinate, carBoxesStrTree)) return generate_lane_length;

            //生成参数赋值
            if (isStart) paras.SetGStartAdjLane = index;
            else paras.SetGEndAdjLane = index;
            Lane inilan = new Lane(iniobsplit, gvec);
            //inilan.IsAdjLaneForProcessLoopThroughEnd = true;
            paras.LanesToAdd.Add(inilan);
            Lane inilanopposite = new Lane(iniobsplit, -gvec);
            inilanopposite.IsAdjLaneForProcessLoopThroughEnd=true;
            paras.LanesToAdd.Add(inilanopposite);
            paras.CarBoxesToAdd.Add(pl);
            generate_lane_length = iniobsplit.Length;
            if (generate_lane_length - dis_connected_double > 0)
                generate_lane_length -= dis_connected_double;
            return generate_lane_length;
        }
    }
}
