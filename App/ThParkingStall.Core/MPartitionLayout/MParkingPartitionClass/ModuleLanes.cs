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
        private double GenerateIntegralModuleLanesOptimizedByRealLength(ref GenerateLaneParas paras, bool allow_through_build = true)
        {
            double generate_lane_length;
            double max_length = -1;
            var isCurDirection = false;
            var para_lanes_add = new List<LineSegment>();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras = new GenerateLaneParas();
                var length = GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref _paras, i, ref para_lanes_add, true);
                para_lanes_add.AddRange(_paras.LanesToAdd.Select(e => e.Line));
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
                                length = length * LayoutScareFactor_Intergral;
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
                                length = length * LayoutScareFactor_Intergral;
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

        private double GenerateLaneForLayoutingSingleVertModule(ref GenerateLaneParas paras)
        {
            double generate_lane_length;
            double max_length = -1;
            var isCurDirection = false;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras = new GenerateLaneParas();
                var length = GenerateSingleModuleLanesForUniqueLaneOptimizedByRealLength(ref _paras, i, true);
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
                                length = length * LayoutScareFactor_SingleVert;
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
                                length = length * LayoutScareFactor_SingleVert;
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

        private double GenerateSingleModuleLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i, bool allow_through_build = true)
        {
            var para_lanes_add = new List<LineSegment>();
            return CalculateModuleLanes(ref paras, i, ref para_lanes_add, allow_through_build, false);
        }
        private double GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i, ref List<LineSegment> para_lanes_add, bool allow_through_build = true)
        {
            return CalculateModuleLanes(ref paras, i, ref para_lanes_add, allow_through_build);
        }

        private double CalculateModuleLanes(ref GenerateLaneParas paras, int i, ref List<LineSegment> para_lanes_add, bool allow_through_build = true, bool isBackBackModule = true)
        {
            double generate_lane_length = -1;
            var lane = IniLanes[i].Line;
            var vec = IniLanes[i].Vec;
            if (!IniLanes[i].CanBeMoved) return generate_lane_length;
            if (lane.Length < LengthCanGIntegralModulesConnectSingle) return generate_lane_length;
            var offsetlane = new LineSegment(lane);
            if (isBackBackModule)
                offsetlane = offsetlane.Translation(vec * (DisBackBackModulus + DisLaneWidth / 2));
            else
                offsetlane = offsetlane.Translation(vec * (DisModulus));
            offsetlane = offsetlane.Scale(20);
            //与边界相交

            var _splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
            var splits = new List<LineSegment>();
            foreach (var s in _splits)
            {
                var k = s.Translation(-vec * DisLaneWidth / 2);
                if (!isBackBackModule)
                    k = s;
                splits.AddRange(SplitBufferLineByPoly(k, DisLaneWidth / 2, Boundary)
                    .Select(e =>
                    {
                        if (isBackBackModule) return e.Translation(vec * DisLaneWidth / 2);
                        else return e;
                    }));
            }
            var linesplitbounds =/* SplitLine(offsetlane, Boundary)*/
                splits
                .Where(e =>
                {
                    var l = new LineSegment(e);
                    l = l.Translation(-vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    var bf = l.Buffer(DisLaneWidth / 2 - 1);
                    bf = bf.Scale(ScareFactorForCollisionCheck);
                    var result = bf.IntersectPoint(Boundary).Count() == 0;
                    //var result = true;
                    l = l.Translation(vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    bf = l.Buffer(DisLaneWidth / 2 - 1);
                    bf = bf.Scale(ScareFactorForCollisionCheck);
                    //20220630modified
                    //foreach (var wl in Walls)
                    //{
                    //    if (bf.IntersectPoint(wl).Count() > 0)
                    //    {
                    //        result = false;
                    //        break;
                    //    }
                    //}
                    if (bf.IntersectPoint(OutBoundary).Count() > 0)
                        result = false;
                    return result;
                })
                .Where(e => Boundary.Contains(e.MidPoint))
                .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                .Select(e =>
                {
                    if (isBackBackModule)
                    {
                        e = e.Translation(-vec * (DisLaneWidth / 2));
                        return e;
                    }
                    else
                        return e;
                })
                /*.Where(e => IsConnectedToLane(e))*/;
            bool generate = false;
            var quitcycle = false;
            STRtree<Polygon> carBoxesStrTree = new STRtree<Polygon>();
            CarBoxes.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));
            foreach (var linesplitbound in linesplitbounds)
            {
                //与车道模块相交
                var linesplitboundback = new LineSegment(linesplitbound);
                if (isBackBackModule)
                    linesplitboundback = linesplitboundback.Translation((-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2)));
                else
                    linesplitboundback = linesplitboundback.Translation((-vec * (DisVertCarLength + DisLaneWidth / 2)));
                var plcarbox = PolyFromLines(linesplitbound, linesplitboundback);
                plcarbox = plcarbox.Scale(ScareFactorForCollisionCheck);
                var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false)
                    //.Where(e =>
                    //{
                    //    return !IsInAnyBoxes(AveragePoint(e.GetCenter(), linesplitboundback.GetClosestPointTo(e.GetCenter(), true)), CarBoxes);
                    //})
                    .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                    .Where(e => !IsInAnyBoxes(e.MidPoint/*.TransformBy(Matrix3d.Displacement(-vec.GetNormal())) * 200*/, carBoxesStrTree, true))
                    .Where(e => IsConnectedToLane(e));
                //解决车道线与车道模块短边平行长度不够的情况
                var fixlinesplitcarboxes = new List<LineSegment>();
                foreach (var tmplinesplitcarboxes in linesplitcarboxes)
                {
                    var k = new LineSegment(tmplinesplitcarboxes);
                    k = k.Translation(vec * DisLaneWidth / 2);
                    var boxs = CarBoxes.Where(f =>
                    {
                        var segs = f.GetEdges();
                        var seg = segs.Where(s => Math.Abs(s.Length - DisCarAndHalfLane) > 1).First();
                        if (isBackBackModule)
                            seg = segs.Where(s => Math.Abs(s.Length - DisVertCarLengthBackBack - DisLaneWidth / 2) > 1).First();
                        if (IsPerpLine(seg, k))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }).Select(box => box.Clone()).ToList();
                    var spindex = new MNTSSpatialIndex(boxs);
                    var plcarboxfix = PolyFromLines(k, linesplitboundback);
                    plcarboxfix = plcarboxfix.Scale(ScareFactorForCollisionCheck);
                    fixlinesplitcarboxes.AddRange(SplitLineBySpacialIndexInPoly(k, plcarboxfix, spindex, false)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        .Where(e => !IsInAnyBoxes(e.MidPoint, boxs, true))
                        .Where(e =>
                        {
                            var ep = new LineSegment(e);
                            ep = ep.Translation(-vec * DisLaneWidth / 2);
                            if (IsConnectedToLane(ep))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }).Select(e =>
                        {
                            e.P0 = e.P0.Translation(-vec * DisLaneWidth / 2);
                            e.P1 = e.P1.Translation(-vec * DisLaneWidth / 2);
                            return e;
                        })
                        );
                    spindex.Dispose();
                }
                foreach (var lnesplit in fixlinesplitcarboxes)
                {
                    var linesplit = lnesplit;
                    var offsetback = new LineSegment(linesplit);
                    if (isBackBackModule)
                        offsetback = offsetback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth));
                    else
                        offsetback = offsetback.Translation(-vec * (DisVertCarLength + DisLaneWidth / 2));
                    var plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    if (!allow_through_build)
                    {
                        if (SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false).Count > 1) continue;
                    }
                    //与障碍物相交
                    linesplit = linesplit.Translation(vec * DisLaneWidth / 2);
                    plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    var obsplits = SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        //.Where(e => !IsInAnyPolys(e.MidPoint, Obstacles))
                        .Where(e =>
                        {
                            var tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(e.MidPoint)).Cast<Polygon>().ToList();
                            return !IsInAnyPolys(e.MidPoint, tmpobs);
                        })
                        .Where(e =>
                        {
                            //与原始车道线模块不相接
                            var tmpline = new LineSegment(e);
                            tmpline = tmpline.Translation(-vec * DisLaneWidth / 2);
                            if (!IsConnectedToLane(tmpline)) return false;
                            var ptonlane = lane.ClosestPoint(e.MidPoint);
                            var ptone = e.ClosestPoint(ptonlane);
                            if (isBackBackModule)
                            {
                                if (ptonlane.Distance(ptone) - DisBackBackModulus - DisLaneWidth / 2 > 1) return false;
                                else return true;
                            }
                            else
                            {
                                if (ptonlane.Distance(ptone) - DisModulus - DisLaneWidth / 2 > 1) return false;
                                else return true;
                            }
                        });

                    if (isBackBackModule)
                    {
                        //背靠背
                        CalculateBackBackLength(obsplits, lane, vec, carBoxesStrTree, ref generate_lane_length, i, ref paras, ref quitcycle, ref generate, ref para_lanes_add);
                    }
                    else
                    {
                        //孤立单排
                        if (!CalculateSingleVertModule(obsplits, lane, linesplit, vec, carBoxesStrTree, ref generate_lane_length, i, ref paras))
                            continue;
                    }
                    if (quitcycle) break;
                }
                if (quitcycle) break;
            }
            return generate_lane_length;
        }
        private bool CalculateSingleVertModule(IEnumerable<LineSegment> obsplits, LineSegment lane, LineSegment linesplit, Vector2D vec
            , STRtree<Polygon> carBoxesStrTree, ref double generate_lane_length, int i, ref GenerateLaneParas paras)
        {
            if (obsplits.Count() == 0)
            {
                var split = linesplit;
                var splitback = new LineSegment(split);
                split = split.Translation(-vec * DisLaneWidth / 2);
                var quit_repeat = false;
                foreach (var l in IniLanes.Select(e => e.Line))
                {
                    var dis_start = l.ClosestPoint(split.P0).Distance(split.P0);
                    var dis_end = l.ClosestPoint(split.P1).Distance(split.P1);
                    if (IsParallelLine(l, split) && dis_start < DisLaneWidth / 2 && dis_end < DisLaneWidth / 2)
                    {
                        quit_repeat = true;
                        break;
                    }
                }
                if (quit_repeat) return false;
                splitback = splitback.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                var splitori = new LineSegment(splitback);
                splitori = splitori.Translation(-vec * (DisVertCarLength + DisLaneWidth));


                var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                if (splits_bd.Count() == 0) return false;
                splitori = splits_bd.First();
                splitback = splitori.Translation(vec * (DisVertCarLength + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) return false;

                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = splitori.Translation(vec * DisLaneWidth / 2);
                var splitori_buffer = splitori.Buffer(DisLaneWidth / 2);
                var obs_splitori_buffer_crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_buffer).Cast<Polygon>();
                var obs_splitori_buffer_crossed_points = new List<Coordinate>();
                foreach (var crossed in obs_splitori_buffer_crossed)
                {
                    obs_splitori_buffer_crossed_points.AddRange(crossed.Coordinates);
                    obs_splitori_buffer_crossed_points.AddRange(crossed.IntersectPoint(splitori_buffer));
                }
                obs_splitori_buffer_crossed_points = obs_splitori_buffer_crossed_points.Where(p => splitori_buffer.Contains(p)).Select(p => splitori.ClosestPoint(p)).ToList();
                obs_splitori_buffer_crossed_points = RemoveDuplicatePts(obs_splitori_buffer_crossed_points);
                obs_splitori_buffer_crossed_points = SortAlongCurve(obs_splitori_buffer_crossed_points, splitori.ToLineString());
                var splitori_splits = SplitLine(splitori, obs_splitori_buffer_crossed_points).ToList();
                if (splitori_splits.Count > 0)
                {
                    splitori = splitori_splits.OrderByDescending(e => e.Length).First();
                }
                if (((lane.ClosestPoint(splitori.P0).Distance(splitori.P0) >/* 5000*/splitori.Length / 3
                    || lane.ClosestPoint(splitori.P1).Distance(splitori.P1) > splitori.Length / 3)
                    && ObstaclesSpatialIndex.SelectCrossingGeometry(ploritolane).Cast<Polygon>().Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0).Count() > 0)
                    || IsInAnyBoxes(splitori.MidPoint, carBoxesStrTree))
                {
                    //生成模块与车道线错开且原车道线碰障碍物
                    return false;
                }
                double dis_connected_double = 0;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) return false;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                {
                    dis_connected_double = DisCarAndHalfLane;
                }
                if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) return false;
                paras.SetNotBeMoved = i;
                var pl = PolyFromLines(split, splitback);
                splitback = splitback.Translation(-vec * DisCarAndHalfLane);
                var splitbackbuffer = splitback.Buffer(DisLaneWidth / 2);
                splitbackbuffer = splitbackbuffer.Scale(ScareFactorForCollisionCheck);
                var continue_for_back_near_wall = false;
                foreach (var wall in Walls)
                    if (wall.IntersectPoint(splitbackbuffer).Count() > 0 || splitbackbuffer.Contains(wall.GetMidPoint()))
                        continue_for_back_near_wall = true;
                var plback = pl.Clone();
                plback = plback.Translation(-vec * DisCarAndHalfLane);
                var split_splitori_points = plback.IntersectPoint(Boundary).Select(e => splitori.ClosestPoint(e)).ToList();
                var mod = new CarModule(plback, splitori, vec);
                var plbacksc = plback.Scale(ScareFactorForCollisionCheck);
                if (ObstaclesSpatialIndex.SelectCrossingGeometry(plbacksc).Count() > 0) return false;
                var addlane = splitori.Translation(vec * (DisCarAndHalfLane + DisLaneWidth / 2));
                var addlane_buffer = addlane.Buffer(DisLaneWidth / 2);
                addlane_buffer = addlane_buffer.Scale(ScareFactorForCollisionCheck);
                if (ObstaclesSpatialIndex.SelectCrossingGeometry(addlane_buffer).Count() > 0) return false;

                //判断条件
                MParkingPartitionPro tmpro = new MParkingPartitionPro();
                tmpro.Walls = Walls;
                tmpro.Boundary = Boundary;
                var tmpro_lane = new LineSegment(addlane);
                if (IsConnectedToLane(tmpro_lane)) tmpro_lane.P0 = tmpro_lane.P0.Translation(Vector(tmpro_lane).Normalize() * DisLaneWidth / 2);
                if (IsConnectedToLane(tmpro_lane, false)) tmpro_lane.P1 = tmpro_lane.P1.Translation(-Vector(tmpro_lane).Normalize() * DisLaneWidth / 2);
                tmpro.IniLanes.Add(new Lane(tmpro_lane, vec.Normalize()));
                tmpro.Obstacles = Obstacles;
                tmpro.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
                var vertlanes = tmpro.GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
                foreach (var k in vertlanes)
                {
                    var vl = k.Line;
                    var line = new LineSegment(vl);
                    line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                    var line_align_backback_rest = new LineSegment();
                    tmpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.Normalize(), DisVertCarWidth, DisVertCarLength
                        , ref line_align_backback_rest, true, false, false, false, true, true, false, false, false, true, false, false, false, true);
                }
                tmpro.UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
                vertlanes = tmpro.GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
                SortLaneByDirection(vertlanes, LayoutMode);
                foreach (var k in vertlanes)
                {
                    var vl = k.Line;
                    UnifyLaneDirection(ref vl, IniLanes);
                    var line = new LineSegment(vl);
                    line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                    var line_align_backback_rest = new LineSegment();
                    tmpro.GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                        , ref line_align_backback_rest, true, false, false, false, true, true, false);
                }
                var generatecars_count = tmpro.Cars.Count;
                generatecars_count += ((int)Math.Floor(tmpro_lane.Length / DisVertCarWidth));
                var estimated_module_count = addlane.Length / DisModulus;
                var pl_forward = plback.Translation(vec.Normalize() * DisCarAndHalfLane);
                var crosseded_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(pl_forward).Cast<Polygon>();
                var crossed_points = new List<Coordinate>();
                foreach (var crossed in crosseded_obs)
                {
                    crossed_points.AddRange(crossed.IntersectPoint(pl_forward));
                    crossed_points.AddRange(crossed.Coordinates.Where(p => pl_forward.Contains(p)));
                }
                crossed_points = crossed_points.OrderBy(p => splitori.ClosestPoint(p, true).Distance(p)).ToList();
                var estimated_depth_count = (splitori.ClosestPoint(addlane.MidPoint, true).Distance(addlane.MidPoint)) / DisVertCarWidth;
                if (crossed_points.Count > 0)
                {
                    var crossed_point = crossed_points.First();
                    estimated_depth_count = (splitori.ClosestPoint(crossed_point, true).Distance(crossed_point)) / DisVertCarWidth;
                }
                var estimated_cars_count = ((int)Math.Floor(estimated_depth_count * estimated_module_count));
                var estimated_this_fullcount = ((int)Math.Floor(tmpro_lane.Length / (DisVertCarWidth * 3 + DisPillarLength) * 3));
                if (generatecars_count > estimated_cars_count * SingleVertModulePlacementFactor && tmpro.Cars.Count >= (estimated_this_fullcount / 3 + 2) * SingleVertModulePlacementFactor)
                {
                    mod.IsInBackBackModule = true;
                    paras.CarBoxesToAdd.Add(plback);
                    paras.CarModulesToAdd.Add(mod);
                    Lane ln = new Lane(addlane, vec);
                    paras.LanesToAdd.Add(ln);
                    generate_lane_length += splitori.Length;
                }
            }
            return true;
        }
        private void CalculateBackBackLength(IEnumerable<LineSegment> obsplits, LineSegment lane, Vector2D vec, STRtree<Polygon> carBoxesStrTree
            , ref double generate_lane_length, int i, ref GenerateLaneParas paras, ref bool quitcycle, ref bool generate, ref List<LineSegment> para_lanes_add)
        {
            foreach (var slit in obsplits)
            {
                var split = slit;
                var splitback = new LineSegment(split);
                split = split.Translation(-vec * DisLaneWidth / 2);
                var quit_repeat = false;
                foreach (var l in IniLanes.Select(e => e.Line))
                {
                    var dis_start = l.ClosestPoint(split.P0).Distance(split.P0);
                    var dis_end = l.ClosestPoint(split.P1).Distance(split.P1);
                    if (IsParallelLine(l, split) && dis_start < DisLaneWidth / 2 && dis_end < DisLaneWidth / 2)
                    {
                        quit_repeat = true;
                        break;
                    }
                }
                if (quit_repeat) continue;
                splitback = splitback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth));
                var splitori = new LineSegment(splitback);
                splitori = splitori.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth));


                var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                if (splits_bd.Count() == 0) continue;
                splitori = splits_bd.First();
                splitback = splitori.Translation(vec * (DisVertCarLengthBackBack + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;

                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = splitori.Translation(vec * DisLaneWidth / 2);
                var splitori_buffer = splitori.Buffer(DisLaneWidth / 2);
                var obs_splitori_buffer_crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_buffer.Scale(ScareFactorForCollisionCheck)).Cast<Polygon>();
                var obs_splitori_buffer_crossed_points = new List<Coordinate>();
                foreach (var crossed in obs_splitori_buffer_crossed)
                {
                    obs_splitori_buffer_crossed_points.AddRange(crossed.Coordinates);
                    obs_splitori_buffer_crossed_points.AddRange(crossed.IntersectPoint(splitori_buffer));
                }
                obs_splitori_buffer_crossed_points = obs_splitori_buffer_crossed_points.Where(p => splitori_buffer.Contains(p)).Select(p => splitori.ClosestPoint(p)).ToList();
                obs_splitori_buffer_crossed_points = RemoveDuplicatePts(obs_splitori_buffer_crossed_points);
                obs_splitori_buffer_crossed_points = SortAlongCurve(obs_splitori_buffer_crossed_points, splitori.ToLineString());
                var splitori_splits = SplitLine(splitori, obs_splitori_buffer_crossed_points).ToList();
                if (splitori_splits.Count > 0)
                {
                    splitori = splitori_splits.OrderByDescending(e => e.Length).First();
                }
                if (((lane.ClosestPoint(splitori.P0).Distance(splitori.P0) >/* 5000*/splitori.Length / 3
                    || lane.ClosestPoint(splitori.P1).Distance(splitori.P1) > splitori.Length / 3)
                    && ObstaclesSpatialIndex.SelectCrossingGeometry(ploritolane).Cast<Polygon>().Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0).Count() > 0)
                    || IsInAnyBoxes(splitori.MidPoint, carBoxesStrTree))
                {
                    //生成模块与车道线错开且原车道线碰障碍物
                    continue;
                }
                var distnearbuilding = IsEssentialToCloseToBuilding(splitori, vec);
                if (distnearbuilding != -1)
                {
                    //贴近建筑物生成
                    bool removed = false;
                    if (splitori.Length >= generate_lane_length && generate_lane_length > 0)
                    {
                        removed = true;
                        generate_lane_length = splitori.Length;
                    }
                    else if (splitori.Length >= generate_lane_length)
                        generate_lane_length = splitori.Length;
                    else if (generate_lane_length > 0)
                        removed = true;
                    else
                        continue;
                    if (!removed)
                        paras.SetNotBeMoved = i;
                    splitori = splitori.Translation(vec * distnearbuilding);
                    var splitoribackup = new LineSegment(splitori);
                    if (!IsConnectedToLaneDouble(splitori))
                    {
                        if (ClosestPointInVertLines(splitori.P0, splitori, IniLanes.Select(e => e.Line).ToList()) > 1)
                            splitori = new LineSegment(splitori.P1, splitori.P0);
                        if (ClosestPointInVertLines(splitoribackup.P0, splitoribackup, IniLanes.Select(e => e.Line).ToList()) > 1)
                            splitoribackup = new LineSegment(splitoribackup.P1, splitoribackup.P0);
                        splitori.P1 = splitori.P1.Translation(Vector(splitori).Normalize() * MaxLength);
                        splitori = SplitLine(splitori, Boundary).First();
                        var lanepoints = new List<Coordinate>();
                        var splitori_bf = splitori.Buffer(DisLaneWidth / 2 - 1);
                        foreach (var ln in IniLanes.Where(e => e.Line.IntersectPoint(splitori).Count() > 0 && e.Line.ClosestPoint(splitori.P0).Distance(splitori.P0) > 1))
                        {
                            lanepoints.AddRange(ln.Line.IntersectPoint(splitori));
                            lanepoints.AddRange(ln.Line.Buffer(DisLaneWidth / 2).IntersectPoint(splitori_bf));
                        }
                        var obcrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_bf).Cast<Polygon>();
                        foreach (var crossed in obcrossed)
                        {
                            lanepoints.AddRange(crossed.IntersectPoint(splitori_bf));
                        }
                        lanepoints = lanepoints.Select(e => splitori.ClosestPoint(e)).ToList();
                        lanepoints = SortAlongCurve(lanepoints, splitori.ToLineString());
                        lanepoints = RemoveDuplicatePts(lanepoints);
                        var build_ex_splits = SplitLine(splitori, lanepoints);
                        if (build_ex_splits.Count > 0)
                        {
                            splitori = build_ex_splits.First();
                            if (splitori.Length > splitoribackup.Length)
                            {
                                var splitori_test_ps = splitoribackup.P1;
                                var splitori_test_pe = splitori.P1;
                                var splitori_test_l = new LineSegment(splitori_test_ps, splitori_test_pe);
                                splitori_test_l = splitori_test_l.Scale(0.5);
                                var splitori_test_l_up = splitori_test_l.Translation(Vector(splitori_test_l).Normalize().GetPerpendicularVector() * MaxLength);
                                var splitori_test_l_down = splitori_test_l.Translation(-Vector(splitori_test_l).Normalize().GetPerpendicularVector() * MaxLength);
                                var splitori_test_bf_up = PolyFromLines(splitori_test_l, splitori_test_l_up);
                                var splitori_test_bf_down = PolyFromLines(splitori_test_l, splitori_test_l_down);
                                var lan_pts_up = new List<Coordinate>();
                                var obs_pts_up = new List<Coordinate>();
                                var lan_pts_down = new List<Coordinate>();
                                var obs_pts_down = new List<Coordinate>();
                                foreach (var ln in IniLanes.Select(e => e.Line.ToLineString()))
                                {
                                    lan_pts_up.AddRange(ln.Coordinates);
                                    lan_pts_up.AddRange(ln.IntersectPoint(splitori_test_bf_up));
                                    lan_pts_down.AddRange(ln.Coordinates);
                                    lan_pts_down.AddRange(ln.IntersectPoint(splitori_test_bf_down));
                                }
                                foreach (var obs in ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_test_bf_up))
                                {
                                    obs_pts_up.AddRange(obs.Coordinates);
                                    obs_pts_up.AddRange(obs.IntersectPoint(splitori_test_bf_up));
                                }
                                foreach (var obs in ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_test_bf_down))
                                {
                                    obs_pts_down.AddRange(obs.Coordinates);
                                    obs_pts_down.AddRange(obs.IntersectPoint(splitori_test_bf_down));
                                }
                                double sc_tol = 1.0001;
                                splitori_test_bf_up = splitori_test_bf_up.Scale(sc_tol);
                                splitori_test_bf_down = splitori_test_bf_down.Scale(sc_tol);
                                splitori_test_bf_up = splitori_test_bf_up.Scale(sc_tol);
                                splitori_test_bf_down = splitori_test_bf_down.Scale(sc_tol);
                                lan_pts_up = lan_pts_up.Where(p => splitori_test_bf_up.Contains(p)).ToList();
                                lan_pts_down = lan_pts_down.Where(p => splitori_test_bf_down.Contains(p)).ToList();
                                obs_pts_up = obs_pts_up.Where(p => splitori_test_bf_up.Contains(p)).ToList();
                                obs_pts_down = obs_pts_down.Where(p => splitori_test_bf_down.Contains(p)).ToList();
                                var uplanedis = 0.0;
                                var downlanedis = 0.0;
                                var upobsdis = 0.0;
                                var downobsdis = 0.0;
                                uplanedis = lan_pts_up.Count() > 0 ? splitori.ClosestPoint(lan_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(lan_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                                downlanedis = lan_pts_down.Count() > 0 ? splitori.ClosestPoint(lan_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(lan_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                                upobsdis = obs_pts_up.Count() > 0 ? splitori.ClosestPoint(obs_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(obs_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                                downobsdis = obs_pts_down.Count() > 0 ? splitori.ClosestPoint(obs_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(obs_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                                if (upobsdis < uplanedis && downobsdis < downlanedis)
                                    splitoribackup = splitori;
                            }
                        }
                    }
                    splitori = splitoribackup;
                    Lane lan = new Lane(splitori, vec);
                    paras.LanesToAdd.Add(lan);
                    paras.LanesToAdd.Add(new Lane(splitori, -vec));
                    paras.CarBoxesToAdd.Add(PolyFromLine(splitori));
                    quitcycle = true;
                    generate = true;
                    break;
                }
                double dis_connected_double = 0;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                {
                    dis_connected_double = DisVertCarLengthBackBack + DisLaneWidth / 2;
                }
                if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;
                paras.SetNotBeMoved = i;
                var pl = PolyFromLines(split, splitback);
                splitback = splitback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                var splitbackbuffer = splitback.Buffer(DisLaneWidth / 2);
                splitbackbuffer = splitbackbuffer.Scale(ScareFactorForCollisionCheck);
                var continue_for_back_near_wall = false;
                foreach (var wall in Walls)
                    if (wall.IntersectPoint(splitbackbuffer).Count() > 0 || splitbackbuffer.Contains(wall.GetMidPoint()))
                        continue_for_back_near_wall = true;
                var plback = pl.Clone();
                plback = plback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                var split_splitori_points = plback.IntersectPoint(Boundary).Select(e => splitori.ClosestPoint(e)).ToList();
                var mod = new CarModule(plback, splitori, vec);
                if (/*splitori.Length / 3 > lane.Length*/false)
                {
                    var lane_ini_replace = splitori.Translation(vec * ((DisVertCarLengthBackBack + DisLaneWidth / 2) + DisLaneWidth / 2));
                    var lane_ini_replace_pair = lane_ini_replace.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                    var pl_replace = PolyFromLines(lane_ini_replace, lane_ini_replace_pair);
                    mod = new CarModule(pl_replace, lane_ini_replace, -vec);
                    if (split.Length >= generate_lane_length && generate_lane_length > 0)
                    {
                        paras.SetNotBeMoved = -1;
                        generate_lane_length = split.Length;
                    }
                    else if (split.Length >= generate_lane_length)
                        generate_lane_length = split.Length;
                    else if (generate_lane_length > 0)
                        paras.SetNotBeMoved = -1;
                    else
                        continue;
                    mod.IsInBackBackModule = true;
                    paras.CarModulesToAdd.Add(mod);
                    paras.CarBoxPlusToAdd.Add(new CarBoxPlus(pl_replace));
                    paras.CarBoxesToAdd.Add(pl_replace);
                    generate = true;
                    Lane ln = new Lane(lane_ini_replace, vec);
                    paras.LanesToAdd.Add(ln);
                    continue;
                }
                else
                {
                    int generate_module_count = paras.CarModulesToAdd.Count;
                    if (splitori.Length > lane.Length)
                    {
                        var lane_ini_pair = lane.Translation(vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                        var pl_initial = PolyFromLines(lane, lane_ini_pair);
                        plback = pl_initial;
                        mod = new CarModule(pl_initial, lane, vec);
                    }
                    if (split.Length >= generate_lane_length && generate_lane_length > 0)
                    {
                        paras.SetNotBeMoved = -1;
                        generate_lane_length = split.Length;
                        if (splitori.Length / 2 > lane.Length)
                            generate_lane_length = split.Length / 3;
                    }
                    else if (split.Length >= generate_lane_length)
                    {
                        generate_lane_length = split.Length;
                        if (splitori.Length / 2 > lane.Length)
                            generate_lane_length = split.Length / 3;
                    }
                    else if (generate_lane_length > 0)
                        paras.SetNotBeMoved = -1;
                    else
                        continue;
                    if (generate_lane_length - dis_connected_double > 0)
                        generate_lane_length -= dis_connected_double;
                    mod.IsInBackBackModule = true;
                    paras.CarModulesToAdd.Add(mod);
                    paras.CarBoxPlusToAdd.Add(new CarBoxPlus(plback));
                    paras.CarBoxesToAdd.Add(plback);
                    generate = true;
                    //generate_lane_length = split.Length;
                    double dis_to_move = 0;
                    LineSegment perpLine = new LineSegment(new Coordinate(0, 0), new Coordinate(0, 0));
                    if (HasParallelLaneForwardExisted(split, vec, /*28700 - 15700*//*20220704*/DisLaneWidth / 2 + DisVertCarWidth * 2/*DisLaneWidth+DisVertCarLength*/, /*19000 - 15700*//*0*/3000, ref dis_to_move, ref perpLine, ref para_lanes_add))
                    {
                        paras.CarBoxPlusToAdd[paras.CarBoxPlusToAdd.Count - 1].IsSingleForParallelExist = true;
                        var existBoxes = CarBoxesPlus.Where(e => e.IsSingleForParallelExist).Select(e => e.Box);
                        var selectedGeos = existBoxes;
                        if (existBoxes.Count() > STRTreeCount)
                        {
                            STRtree<Polygon> strTree = new STRtree<Polygon>();
                            foreach (var cutter in existBoxes) strTree.Insert(cutter.EnvelopeInternal, cutter);
                            selectedGeos = strTree.Query(perpLine.ToLineString().EnvelopeInternal);
                        }
                        foreach (var box in selectedGeos)
                        {
                            if (perpLine.IntersectPoint(box).Count() > 0)
                            {
                                paras.CarModulesToAdd.RemoveAt(paras.CarModulesToAdd.Count - 1);
                                paras.CarBoxPlusToAdd.RemoveAt(paras.CarBoxPlusToAdd.Count - 1);
                                paras.CarBoxesToAdd.RemoveAt(paras.CarBoxesToAdd.Count - 1);
                                generate = false;
                                generate_lane_length = -1;
                                break;
                            }
                        }
                    }
                    else
                    {
                        var segs = pl.GetEdges().OrderByDescending(e => e.Length).Take(2);
                        if (segs.First().Length - segs.Last().Length > 10)
                        {
                            paras.CarBoxesToAdd.Add(pl);
                            Lane ln = new Lane(split, vec);
                            paras.LanesToAdd.Add(ln);
                            Lane _ln = new Lane(split, -vec);
                            paras.LanesToAdd.Add(_ln);
                        }
                        else
                        {
                            paras.CarBoxesToAdd.Add(pl);
                            CarModule module = new CarModule(pl, split, -vec);
                            module.IsInBackBackModule = true;
                            paras.CarModulesToAdd.Add(module);
                            Lane ln = new Lane(split, vec);
                            paras.LanesToAdd.Add(ln);
                        }
                    }
                    if (paras.CarModulesToAdd.Count - generate_module_count == 1)
                    {
                        //如果只生成一个modulebox，宽度是7850，车位是5300，如果在后续生成车道的过程中有可能碰车位，这时应该缩短车道，作特殊处理
                        paras.CarModulesToAdd[paras.CarModulesToAdd.Count - 1].IsSingleModule = true;
                    }
                }
            }
        }

    }
}
