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
        private double GenerateAdjacentLanesOptimizedByRealLength(ref GenerateLaneParas paras)
        {
            double generate_lane_length;
            double max_length = -1;
            var isCurDirection = false;
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
                    case ((int)LayoutDirection.FOLLOWPREVIOUS):
                        {
                            if (ParentDir != Vector2D.Zero && _paras.LanesToAdd.Count > 0 && IsPerpVector(ParentDir, Vector(_paras.LanesToAdd[0].Line)))
                            {
                                length *= LayoutScareFactor_ParentDir;
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
        /// <summary>
        /// laneVec:车道线的移动方向;p:车道线在墙上的点;lineVec:从墙上的点到车道线另一端方向单位向量
        /// </summary>
        /// <param name="line"></param>
        /// <param name="p"></param>
        /// <param name="lineVec"></param>
        /// <returns></returns>
        private double CalOffsetDistanceForSingleLaneNearNonPerpWall(Vector2D laneVec, Coordinate p, Vector2D lineVec)
        {
            var length = DisCarAndHalfLane + CollisionD - CollisionTOP;
            double distance = 0;
            var pt = p;
            var ps= pt.Translation(lineVec.Normalize() * length);
            //拿邻近wall的方向和线段
            var _nearwall_seg = new LineSegment();
            var vec = laneVec;
            foreach (var wall in Walls)
            {
                if (wall.Coordinates.Count() >= 2)
                {
                    var found = false;
                    for (int i = 0; i < wall.Coordinates.Count() - 1; i++)
                    {
                        var wl = new LineSegment(wall.Coordinates[i], wall.Coordinates[i + 1]);
                        if (wl.ClosestPoint(pt).Distance(pt) < 10)
                        {
                            _nearwall_seg = wl;
                            if (wl.Length < LengthCanGAdjLaneConnectSingle * 0.8) continue;
                            var angle = Math.Abs(Vector(wl).AngleTo(new Vector2D(pt, ps)) / Math.PI * 180);
                            angle = Math.Min(angle, 180 - angle);
                            //如果角度大于135(180-45),不生成
                            double angletol = 45;
                            if (wl.P0.Distance(pt) < wl.P1.Distance(pt))
                            {
                                if (angle < angletol)
                                {
                                    var ptest = ps.Translation(Vector(wl).Normalize() * (ps.Distance(pt)));
                                    var vec_a = new Vector2D(ps, pt);
                                    var vec_b = new Vector2D(ps, ptest);
                                    //判断同向
                                    if (vec_a.Dot(vec_b) < 0) continue;
                                }
                                found = true;
                                vec = Vector(wl);
                            }
                            else
                            {
                                if (angle < angletol)
                                {
                                    var ptest = ps.Translation(-Vector(wl).Normalize() * (ps.Distance(pt)));
                                    var vec_a = new Vector2D(ps, pt);
                                    var vec_b = new Vector2D(ps, ptest);
                                    if (vec_a.Dot(vec_b) < 0) continue;
                                }
                                found = true;
                                vec = -Vector(wl);
                            }
                            break;
                        }
                    }
                    if (found) break;
                }
            }
            //根据墙线与车道线的角度，调整从墙线偏移出的距离（1车位+半车道的三角函数值）——如果墙线与车道线不垂直,多偏移1mm，避免墙线微斜的精度问题
            var pt_closest_onwall = _nearwall_seg.ClosestPoint(ps, true);
            var angle_pspt_pswall = new Vector2D(ps, pt).AngleTo(new Vector2D(ps, pt_closest_onwall));
            var _nearwall_endpt = _nearwall_seg.P0.Distance(pt_closest_onwall) < _nearwall_seg.P1.Distance(pt_closest_onwall) ?
                _nearwall_seg.P1 : _nearwall_seg.P0;
            var pt_on_lane_project = new LineSegment(pt, ps).ClosestPoint(_nearwall_endpt, true);
            distance = pt_on_lane_project.Distance(pt);
            var extend_offset_distance = distance < 100 && distance < _nearwall_seg.Length / 100 && distance > 0;
            var angle_pspt_pswall_2 = new Vector2D(pt, pt_closest_onwall).AngleTo(new Vector2D(pt, _nearwall_endpt));
            if (Math.Abs(angle_pspt_pswall_2) / Math.PI * 180 < 0.1)
            {
                angle_pspt_pswall += Math.Abs(angle_pspt_pswall_2);
            }
            if (!extend_offset_distance)
            {
                distance = 1;
            }
            length = (DisCarAndHalfLane + distance + CollisionD - CollisionTOP) / Math.Cos(angle_pspt_pswall);
            return length;
        }
        private double GenerateAdjacentLanesFunc(ref GenerateLaneParas paras, Lane lane, int index, bool isStart)
        {
            double generate_lane_length = -1;
            Coordinate pt;
            Coordinate ps;

            #region 偏移参数赋值 pt墙点，ps从墙上出来的偏移点 
            //初始调整方向和移动点
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
            //拿邻近wall的方向和线段
            var _nearwall_seg = new LineSegment();
            var vec = lane.Vec;
            foreach (var wall in Walls)
            {
                if (wall.Coordinates.Count() >= 2)
                {
                    var found = false;
                    for (int i = 0; i < wall.Coordinates.Count() - 1; i++)
                    {
                        var wl = new LineSegment(wall.Coordinates[i], wall.Coordinates[i + 1]);
                        if (wl.ClosestPoint(pt).Distance(pt) < 10)
                        {
                            _nearwall_seg = wl;
                            if (wl.Length < LengthCanGAdjLaneConnectSingle * 0.8) continue;
                            var angle = Math.Abs(Vector(wl).AngleTo(new Vector2D(pt, ps)) / Math.PI * 180);
                            angle = Math.Min(angle, 180 - angle);
                            //如果角度大于135(180-45),不生成
                            double angletol = 45;
                            if (wl.P0.Distance(pt) < wl.P1.Distance(pt))
                            {
                                if (angle < angletol)
                                {
                                    var ptest = ps.Translation(Vector(wl).Normalize() * (ps.Distance(pt)));
                                    var vec_a = new Vector2D(ps, pt);
                                    var vec_b = new Vector2D(ps, ptest);
                                    //判断同向
                                    if (vec_a.Dot(vec_b) < 0) continue;
                                }
                                found = true;
                                vec = Vector(wl);
                            }
                            else
                            {
                                if (angle < angletol)
                                {
                                    var ptest = ps.Translation(-Vector(wl).Normalize() * (ps.Distance(pt)));
                                    var vec_a = new Vector2D(ps, pt);
                                    var vec_b = new Vector2D(ps, ptest);
                                    if (vec_a.Dot(vec_b) < 0) continue;
                                }
                                found = true;
                                vec = -Vector(wl);
                            }
                            break;
                        }
                    }
                    if (found) break;
                }
            }

            //根据墙线与车道线的角度，调整从墙线偏移出的距离（1车位+半车道的三角函数值）——如果墙线与车道线不垂直,多偏移1mm，避免墙线微斜的精度问题
            var pt_closest_onwall = _nearwall_seg.ClosestPoint(ps, true);
            var angle_pspt_pswall = new Vector2D(ps, pt).AngleTo(new Vector2D(ps, pt_closest_onwall));
            var _nearwall_endpt = _nearwall_seg.P0.Distance(pt_closest_onwall) < _nearwall_seg.P1.Distance(pt_closest_onwall) ?
                _nearwall_seg.P1 : _nearwall_seg.P0;
            var pt_on_lane_project = new LineSegment(pt, ps).ClosestPoint(_nearwall_endpt, true);
            var distance = pt_on_lane_project.Distance(pt);
            var extend_offset_distance = distance < 100 && distance < _nearwall_seg.Length / 100 && distance > 0;
            var angle_pspt_pswall_2 =new Vector2D(pt, pt_closest_onwall).AngleTo(new Vector2D(pt, _nearwall_endpt));
            if (Math.Abs(angle_pspt_pswall_2) / Math.PI * 180 < 0.1)
            {
                angle_pspt_pswall += Math.Abs(angle_pspt_pswall_2);
            }
            if (!extend_offset_distance)
            {
                distance = 1;
            }
            var length = (DisCarAndHalfLane + distance + CollisionD - CollisionTOP) / Math.Cos(angle_pspt_pswall);
            if (isStart)
            {
                pt = lane.Line.P0;
                ps = pt.Translation(Vector(lane.Line).Normalize() * length);
            }
            else
            {
                pt = lane.Line.P1;
                ps = pt.Translation(-Vector(lane.Line).Normalize() * length);
            }
            #endregion

            #region 业务逻辑判断
            //与边界相交处理
            var line = LineSegmentSDL(ps, vec, MaxLength);
            var tmpline = SplitLine(line, Boundary).Where(e => e.Length > 1).First();
            if (Boundary.Contains(tmpline.MidPoint))
                line = tmpline;
            else return generate_lane_length;
            //gevc:远离墙线一方向的向量
            var gvec = Vector(line).GetPerpendicularVector().Normalize();
            var ptestvec = ps.Translation(gvec);
            if (ptestvec.Distance(pt) < length) gvec = -gvec;
            //与车道模块相交处理
            STRtree<Polygon> carBoxesStrTree = new STRtree<Polygon>();
            var carBoxesSpatialIndexList = CarBoxesSpatialIndex.SelectAll().Cast<Polygon>().ToList();
            carBoxesSpatialIndexList.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));
            CarBoxes.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));
            var lanes_cutter = new List<Polygon>(CarBoxes);
            IniLanes.Where(e => !IsParallelLine(line, e.Line)).Select(e => e.Line).ToList().ForEach(e => lanes_cutter.Add(PolyFromLine(e)));
            var inilinesplitcarboxes = SplitLine(line, lanes_cutter).Where(e => e.Length > 1).First();

            //解决车道线靠墙的方向有车道线的情况
            var line_to_wall = TranslateReservedConnection(line, -gvec.Normalize() * length);
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
            //长度判断
            if (inilinesplitcarboxes.Length < DisVertCarLength) return generate_lane_length;
            if (IsInAnyBoxes(inilinesplitcarboxes.MidPoint, carBoxesStrTree) || inilinesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            //生成车道与生成车道沿墙线1模块+半车道的Box与车道模块的相交处理
            var inilinesplitcarboxesaction = new LineSegment(inilinesplitcarboxes);
            inilinesplitcarboxesaction = TranslateReservedConnection(inilinesplitcarboxesaction, -gvec.Normalize() * (length+DisLaneWidth/2));
            var inilinesplitcarboxesactionpolyline = PolyFromLines(inilinesplitcarboxes, inilinesplitcarboxesaction);
            var inilinesplitcarboxesactionlaneboxes = IniLanes.Where(e => IsParallelLine(e.Line, inilinesplitcarboxesaction))
                .Select(e => e.Line.Buffer(DisLaneWidth / 2 - 0.001));
            var inilinesplitcarboxesactionpoints = new List<Coordinate>();
            foreach (var box in inilinesplitcarboxesactionlaneboxes)
            {
                inilinesplitcarboxesactionpoints.AddRange(box.Coordinates);
                inilinesplitcarboxesactionpoints.AddRange(box.IntersectPoint(inilinesplitcarboxesactionpolyline));
            }
            inilinesplitcarboxesactionpoints = inilinesplitcarboxesactionpoints
                .Where(e => inilinesplitcarboxesactionpolyline.Contains(e) || inilinesplitcarboxesactionpolyline.ClosestPoint(e).Distance(e) < 0.0001)
                .Select(e => inilinesplitcarboxes.ClosestPoint(e)).ToList();
            SortAlongCurve(inilinesplitcarboxesactionpoints, inilinesplitcarboxes);
            if (inilinesplitcarboxesactionpoints.Count > 0)
                if (inilinesplitcarboxes.P0.Distance(inilinesplitcarboxesactionpoints[0]) < 10) return generate_lane_length;
            inilinesplitcarboxes = SplitLine(inilinesplitcarboxes, inilinesplitcarboxesactionpoints).First();
            //与障碍物的相交处理
            var iniplsplitbox = inilinesplitcarboxes.Buffer(DisLaneWidth / 2);
            iniplsplitbox = iniplsplitbox.Scale(ScareFactorForCollisionCheck);
            var iniobsplit = SplitLineBySpacialIndexInPoly(inilinesplitcarboxes, iniplsplitbox, ObstaclesSpatialIndex, false)
                .Where(e => e.Length > 1).First();
            if (iniobsplit.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            var tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(iniobsplit.MidPoint)).Cast<Polygon>().ToList();
            if (IsInAnyPolys(iniobsplit.MidPoint, tmpobs))
                return generate_lane_length;
            //重复生成判断
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
            #endregion

            #region 生成参数赋值
            double dis_to_move = 0;
            var perpLine = new LineSegment();
            double dis_connected_double = 0;
            var para_lanes_add = new List<LineSegment>();
            if (HasParallelLaneForwardExisted(iniobsplit, gvec, DisModulus, 1, ref dis_to_move, ref perpLine, ref para_lanes_add)) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit,IniLanes) && iniobsplit.Length < LengthCanGAdjLaneConnectDouble) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit,IniLanes))
            {
                dis_connected_double = DisCarAndHalfLane;
            }
            var offsetline = new LineSegment(iniobsplit);
            offsetline = TranslateReservedConnection(offsetline, -gvec * DisCarAndHalfLane);
            var pl = PolyFromLines(iniobsplit, offsetline);
            if (IsInAnyBoxes(pl.Envelope.Centroid.Coordinate, carBoxesStrTree)) return generate_lane_length;
            
            if (isStart) paras.SetGStartAdjLane = index;
            else paras.SetGEndAdjLane = index;
            Lane inilan = new Lane(iniobsplit, gvec);
            paras.LanesToAdd.Add(inilan);
            Lane inilanopposite = new Lane(iniobsplit, -gvec);
            paras.LanesToAdd.Add(inilanopposite);
            paras.CarBoxesToAdd.Add(pl);
            generate_lane_length = iniobsplit.Length;
            if (generate_lane_length - dis_connected_double > 0)
                generate_lane_length -= dis_connected_double;
            return generate_lane_length;
            #endregion
        }
    }
}
