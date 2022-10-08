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

            #region 与边界的相交处理，返回车道位置：1模块位置
            var offsetlane = new LineSegment(lane);
            //背靠背，检测1模块+半车道位置的边界等条件
            if (isBackBackModule)
                offsetlane = offsetlane.Translation(vec * (DisBackBackModulus + DisLaneWidth / 2));
            else
                offsetlane = offsetlane.Translation(vec * (DisModulus));
            offsetlane = offsetlane.Scale(20);
            //与边界相交:车道线Buffer的Polyline与边界的相交点作为车道线的分割点分割处理车道线
            //背靠背：检测1模块和1模块+半车道的位置各一次
            var _splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
            var splits = new List<LineSegment>();
            if (isBackBackModule)
            {
                foreach (var s in _splits)
                {
                    var k = s.Translation(-vec * DisLaneWidth / 2);
                    splits.AddRange(SplitBufferLineByPoly(k, DisLaneWidth / 2, Boundary)
                    .Select(e => e.Translation(vec * DisLaneWidth / 2)));
                }
            }
            else splits = _splits;
            //上一步先分割，这一部通过交点为0作了加强判断
            var linesplitbounds = splits.Where(e =>
            {
                var l = new LineSegment(e);
                //回到1模块的位置
                l = l.Translation(-vec * DisLaneWidth / 2);
                l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                var bf = l.Buffer(DisLaneWidth / 2 - 1);
                bf = bf.Scale(ScareFactorForCollisionCheck);
                //加强判断，经过上一步处理这里应该没有交点
                var result = bf.IntersectPoint(Boundary).Count() == 0;
                //回到1模块+半车道位置，同理
                l = l.Translation(vec * DisLaneWidth / 2);
                l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                bf = l.Buffer(DisLaneWidth / 2 - 1);
                bf = bf.Scale(ScareFactorForCollisionCheck);
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
                });
            #endregion

            bool generate = false;
            var quitcycle = false;
            STRtree<Polygon> carBoxesStrTree = new STRtree<Polygon>();
            CarBoxes.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));
            foreach (var linesplitbound in linesplitbounds)
            {
                #region 与车道模块的相交处理，返回车道位置，1模块的位置
                //与车道模块相交
                var linesplitboundback = new LineSegment(linesplitbound);
                if (isBackBackModule)
                    linesplitboundback = linesplitboundback.Translation((-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2)));
                else
                    linesplitboundback = linesplitboundback.Translation((-vec * (DisVertCarLength + DisLaneWidth / 2)));
                //第二个半模块的Box
                var plcarbox = PolyFromLines(linesplitbound, linesplitboundback);
                plcarbox = plcarbox.Scale(ScareFactorForCollisionCheck);
                var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false);
                linesplitcarboxes = linesplitcarboxes.Where(e => e.Length > LengthCanGIntegralModulesConnectSingle).ToList();
                linesplitcarboxes = linesplitcarboxes.Where(e => !IsInAnyBoxes(e.MidPoint/*.TransformBy(Matrix3d.Displacement(-vec.GetNormal())) * 200*/, carBoxesStrTree, true)).ToList();
                linesplitcarboxes = linesplitcarboxes.Where(e => IsConnectedToLane(e)).ToList();

                #region 一种CASE：生成背靠背模块车道线的前方有与车道模块的短边平行，做一次分割操作，返回车道位置：1模块的位置
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
                #endregion
                #endregion

                foreach (var lnesplit in fixlinesplitcarboxes)
                {
                    var linesplit = lnesplit;

                    #region 与障碍物的相交判断及处理,返回车道位置：1模块+半车道（单排：1模块(5300+5300) +半车道）位置
                    var offsetback = new LineSegment(linesplit);
                    if (isBackBackModule)
                        offsetback = offsetback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth));
                    else
                        offsetback = offsetback.Translation(-vec * (DisVertCarLength + DisLaneWidth / 2));
                    //第二个半模块
                    var plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    if (!allow_through_build)
                    {
                        if (SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false).Count > 1) continue;
                    }
                    //与障碍物相交,半模块车道与1模块+半车道的位置BOX与车道相交判断及处理
                    linesplit = linesplit.Translation(vec * DisLaneWidth / 2);
                    plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    var obsplits = SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
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
                    #endregion

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
                //回到1模块车道位置
                split = split.Translation(-vec * DisLaneWidth / 2);
                //判断是否重复车道
                var quit_repeat = false;
                foreach (var l in IniLanes.Select(e => e.Line))
                {
                    var dis_start = l.ClosestPoint(split.P0).Distance(split.P0);
                    var dis_end = l.ClosestPoint(split.P1).Distance(split.P1);
                    if (IsParallelLine(l, split) && dis_start < DisLaneWidth / 2 && dis_end < DisLaneWidth / 2
                        && dis_start>10 && dis_end>10)
                    {
                        quit_repeat = true;
                        break;
                    }
                }
                if (quit_repeat) return false;

                #region 与原始地库边界的相交判断处理（防止分区的另一部分车道宽度不够）返回车道位置splitori回到原始车道线位置 splitback 半模块位置
                //splitback回到半模块车道位置
                splitback = splitback.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                var splitori = new LineSegment(splitback);
                //splitori退回原始车道线以前半车道位置与最外层原始边界线相交判断，防止分区的另一部分车道宽度不够
                splitori = splitori.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                if (splits_bd.Count() == 0) return false;
                splitori = splits_bd.First();
                //splitback回到半模块车道位置
                splitback = splitori.Translation(vec * (DisVertCarLength + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) return false;
                //ploritolane原始车道线以前半车道+半模块的Box
                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = splitori.Translation(vec * DisLaneWidth / 2);
                //splitori原始车道线位置，splitback半模块位置
                #endregion

                #region 原始车道线的Buffer与障碍物的相交判断处理,返回车道位置原始车道线位置
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
                #endregion

                //附条件判断
                double dis_connected_double = 0;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) return false;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                {
                    dis_connected_double = DisCarAndHalfLane;
                }
                if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) return false;


                paras.SetNotBeMoved = i;
                //pl第二个半模块 plback第一个半模块
                var pl = PolyFromLines(split, splitback);
                var plback = pl.Clone();
                plback = plback.Translation(-vec * DisCarAndHalfLane);

                //添加生成参数设置
                var mod = new CarModule(plback, splitori, vec);
                var plbacksc = plback.Scale(ScareFactorForCollisionCheck);
                if (ObstaclesSpatialIndex.SelectCrossingGeometry(plbacksc).Count() > 0) return false;
                var addlane = splitori.Translation(vec * (DisCarAndHalfLane + DisLaneWidth / 2));
                var addlane_buffer = addlane.Buffer(DisLaneWidth / 2);
                addlane_buffer = addlane_buffer.Scale(ScareFactorForCollisionCheck);
                if (ObstaclesSpatialIndex.SelectCrossingGeometry(addlane_buffer).Count() > 0) return false;

                //单模块车道与车道的相交判断
                var lane_split_points = new List<Coordinate>();
                foreach (var ln in IniLanes.Where(e => !IsParallelLine(e.Line,addlane)).Select(e => e.Line))
                {
                    lane_split_points.AddRange(ln.IntersectPoint(addlane));
                }
                lane_split_points=SortAlongCurve(lane_split_points, addlane.ToLineString());
                var lane_splits=SplitLine(addlane, lane_split_points).Where(e => IsConnectedToLane(e)).Where(e => {
                    if (IsConnectedToLaneDouble(e)) return e.Length > LengthCanGIntegralModulesConnectDouble;
                    else return e.Length > LengthCanGIntegralModulesConnectSingle;
                } );
                foreach (var lane_split in lane_splits)
                {
                    //生成判断条件估计比较生成单排车位数generatecars_count与不生成单排用PerpMoudle生成车位数孰多
                    #region 构造MParkingPartitionPro调用车位生成方法，看生成孤立单排的实际车位数量
                    MParkingPartitionPro tmpro = new MParkingPartitionPro();
                    tmpro.Walls = Walls;
                    tmpro.Boundary = Boundary;
                    var tmpro_lane = new LineSegment(lane_split);
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
                    #endregion

                    #region 估计不生成单排的车位数estimated_cars_count,estimated_this_fullcount
                    var estimated_module_count = lane_split.Length / DisModulus;
                    var pl_forward = plback.Translation(vec.Normalize() * DisCarAndHalfLane);
                    var crosseded_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(pl_forward).Cast<Polygon>();
                    var crossed_points = new List<Coordinate>();
                    foreach (var crossed in crosseded_obs)
                    {
                        crossed_points.AddRange(crossed.IntersectPoint(pl_forward));
                        crossed_points.AddRange(crossed.Coordinates.Where(p => pl_forward.Contains(p)));
                    }
                    crossed_points = crossed_points.OrderBy(p => splitori.ClosestPoint(p, true).Distance(p)).ToList();
                    var estimated_depth_count = (splitori.ClosestPoint(lane_split.MidPoint, true).Distance(lane_split.MidPoint)) / DisVertCarWidth;
                    if (crossed_points.Count > 0)
                    {
                        var crossed_point = crossed_points.First();
                        estimated_depth_count = (splitori.ClosestPoint(crossed_point, true).Distance(crossed_point)) / DisVertCarWidth;
                    }
                    var estimated_cars_count = ((int)Math.Floor(estimated_depth_count * estimated_module_count));
                    var estimated_this_fullcount = ((int)Math.Floor(tmpro_lane.Length / (DisVertCarWidth * 3 + DisPillarLength) * 3));
                    #endregion

                    //生成参数赋值
                    if (generatecars_count > estimated_cars_count * SingleVertModulePlacementFactor && tmpro.Cars.Count >= (estimated_this_fullcount / 3 + 2) * SingleVertModulePlacementFactor)
                    {
                        mod.IsInBackBackModule = true;
                        var split_pl = PolyFromLines(lane_split.Translation(-vec.Normalize() * (DisCarAndHalfLane + DisLaneWidth / 2)),
                            lane_split.Translation(-vec.Normalize() * DisLaneWidth / 2));
                        paras.CarBoxesToAdd.Add(/*plback */split_pl);
                        mod.Box = split_pl;
                        mod.Line = new LineSegment(splitori.ClosestPoint(lane_split.P0), splitori.ClosestPoint(lane_split.P1));
                        paras.CarModulesToAdd.Add(mod);
                        Lane ln = new Lane(lane_split, vec);
                        paras.LanesToAdd.Add(ln);
                        generate_lane_length += splitori.Length;
                    }
                }

                ////生成判断条件估计比较生成单排车位数generatecars_count与不生成单排用PerpMoudle生成车位数孰多
                //#region 构造MParkingPartitionPro调用车位生成方法，看生成孤立单排的实际车位数量
                //MParkingPartitionPro tmpro = new MParkingPartitionPro();
                //tmpro.Walls = Walls;
                //tmpro.Boundary = Boundary;
                //var tmpro_lane = new LineSegment(addlane);
                //if (IsConnectedToLane(tmpro_lane)) tmpro_lane.P0 = tmpro_lane.P0.Translation(Vector(tmpro_lane).Normalize() * DisLaneWidth / 2);
                //if (IsConnectedToLane(tmpro_lane, false)) tmpro_lane.P1 = tmpro_lane.P1.Translation(-Vector(tmpro_lane).Normalize() * DisLaneWidth / 2);
                //tmpro.IniLanes.Add(new Lane(tmpro_lane, vec.Normalize()));
                //tmpro.Obstacles = Obstacles;
                //tmpro.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
                //var vertlanes = tmpro.GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
                //foreach (var k in vertlanes)
                //{
                //    var vl = k.Line;
                //    var line = new LineSegment(vl);
                //    line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                //    var line_align_backback_rest = new LineSegment();
                //    tmpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.Normalize(), DisVertCarWidth, DisVertCarLength
                //        , ref line_align_backback_rest, true, false, false, false, true, true, false, false, false, true, false, false, false, true);
                //}
                //tmpro.UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
                //vertlanes = tmpro.GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
                //SortLaneByDirection(vertlanes, LayoutMode);
                //foreach (var k in vertlanes)
                //{
                //    var vl = k.Line;
                //    UnifyLaneDirection(ref vl, IniLanes);
                //    var line = new LineSegment(vl);
                //    line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                //    var line_align_backback_rest = new LineSegment();
                //    tmpro.GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                //        , ref line_align_backback_rest, true, false, false, false, true, true, false);
                //}
                //var generatecars_count = tmpro.Cars.Count;
                //generatecars_count += ((int)Math.Floor(tmpro_lane.Length / DisVertCarWidth));
                //#endregion

                //#region 估计不生成单排的车位数estimated_cars_count,estimated_this_fullcount
                //var estimated_module_count = addlane.Length / DisModulus;
                //var pl_forward = plback.Translation(vec.Normalize() * DisCarAndHalfLane);
                //var crosseded_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(pl_forward).Cast<Polygon>();
                //var crossed_points = new List<Coordinate>();
                //foreach (var crossed in crosseded_obs)
                //{
                //    crossed_points.AddRange(crossed.IntersectPoint(pl_forward));
                //    crossed_points.AddRange(crossed.Coordinates.Where(p => pl_forward.Contains(p)));
                //}
                //crossed_points = crossed_points.OrderBy(p => splitori.ClosestPoint(p, true).Distance(p)).ToList();
                //var estimated_depth_count = (splitori.ClosestPoint(addlane.MidPoint, true).Distance(addlane.MidPoint)) / DisVertCarWidth;
                //if (crossed_points.Count > 0)
                //{
                //    var crossed_point = crossed_points.First();
                //    estimated_depth_count = (splitori.ClosestPoint(crossed_point, true).Distance(crossed_point)) / DisVertCarWidth;
                //}
                //var estimated_cars_count = ((int)Math.Floor(estimated_depth_count * estimated_module_count));
                //var estimated_this_fullcount = ((int)Math.Floor(tmpro_lane.Length / (DisVertCarWidth * 3 + DisPillarLength) * 3));
                //#endregion

                ////生成参数赋值
                //if (generatecars_count > estimated_cars_count * SingleVertModulePlacementFactor && tmpro.Cars.Count >= (estimated_this_fullcount / 3 + 2) * SingleVertModulePlacementFactor)
                //{
                //    mod.IsInBackBackModule = true;
                //    paras.CarBoxesToAdd.Add(plback);
                //    paras.CarModulesToAdd.Add(mod);
                //    Lane ln = new Lane(addlane, vec);
                //    paras.LanesToAdd.Add(ln);
                //    generate_lane_length += splitori.Length;
                //}
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
                //回到1模块的位置
                split = split.Translation(-vec * DisLaneWidth / 2);
                //判断是否重复
                var quit_repeat = false;
                foreach (var l in IniLanes.Select(e => e.Line))
                {
                    var dis_start = l.ClosestPoint(split.P0).Distance(split.P0);
                    var dis_end = l.ClosestPoint(split.P1).Distance(split.P1);
                    if (IsParallelLine(l, split) && dis_start < DisLaneWidth / 2 && dis_end < DisLaneWidth / 2 
                        && dis_start>10 && dis_end>10)
                    {
                        quit_repeat = true;
                        break;
                    }
                }
                if (quit_repeat) continue;

                #region 与原始地库边界的相交判断处理（防止分区的另一部分车道宽度不够）返回车道位置splitori回到原始车道线位置 splitback 半模块位置
                //splitback回到半模块位置
                splitback = splitback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth));
                var splitori = new LineSegment(splitback);
                splitori = splitori.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth));
                //退回原始车道后半车道位置与最外层大边界做相交判断，防止车道线另一部分车道宽度不够
                var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                if (splits_bd.Count() == 0) continue;
                splitori = splits_bd.First();
                //splitback回到半模块位置
                splitback = splitori.Translation(vec * (DisVertCarLengthBackBack + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;
                //ploritolane原始车道线以前半车道+半模块的Box
                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = splitori.Translation(vec * DisLaneWidth / 2);
                //splitori 回到原始车道线位置 splitback 半模块位置
                #endregion

                #region 原始车道线的Buffer与障碍物的相交判断处理,返回车道位置原始车道线位置
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
                #endregion

                #region 贴近建筑物的插车处理模块，已弃用————模块功能没有大问题挺好用，为迭代算法更快找到优解，在小分区中就不要这么智能于是注释掉了
                //var distnearbuilding = IsEssentialToCloseToBuilding(splitori, vec);
                //if (distnearbuilding != -1)
                //{
                //    //贴近建筑物生成
                //    bool removed = false;
                //    if (splitori.Length >= generate_lane_length && generate_lane_length > 0)
                //    {
                //        removed = true;
                //        generate_lane_length = splitori.Length;
                //    }
                //    else if (splitori.Length >= generate_lane_length)
                //        generate_lane_length = splitori.Length;
                //    else if (generate_lane_length > 0)
                //        removed = true;
                //    else
                //        continue;
                //    if (!removed)
                //        paras.SetNotBeMoved = i;
                //    splitori = splitori.Translation(vec * distnearbuilding);
                //    var splitoribackup = new LineSegment(splitori);
                //    if (!IsConnectedToLaneDouble(splitori))
                //    {
                //        if (ClosestPointInVertLines(splitori.P0, splitori, IniLanes.Select(e => e.Line).ToList()) > 1)
                //            splitori = new LineSegment(splitori.P1, splitori.P0);
                //        if (ClosestPointInVertLines(splitoribackup.P0, splitoribackup, IniLanes.Select(e => e.Line).ToList()) > 1)
                //            splitoribackup = new LineSegment(splitoribackup.P1, splitoribackup.P0);
                //        splitori.P1 = splitori.P1.Translation(Vector(splitori).Normalize() * MaxLength);
                //        splitori = SplitLine(splitori, Boundary).First();
                //        var lanepoints = new List<Coordinate>();
                //        var splitori_bf = splitori.Buffer(DisLaneWidth / 2 - 1);
                //        foreach (var ln in IniLanes.Where(e => e.Line.IntersectPoint(splitori).Count() > 0 && e.Line.ClosestPoint(splitori.P0).Distance(splitori.P0) > 1))
                //        {
                //            lanepoints.AddRange(ln.Line.IntersectPoint(splitori));
                //            lanepoints.AddRange(ln.Line.Buffer(DisLaneWidth / 2).IntersectPoint(splitori_bf));
                //        }
                //        var obcrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_bf).Cast<Polygon>();
                //        foreach (var crossed in obcrossed)
                //        {
                //            lanepoints.AddRange(crossed.IntersectPoint(splitori_bf));
                //        }
                //        lanepoints = lanepoints.Select(e => splitori.ClosestPoint(e)).ToList();
                //        lanepoints = SortAlongCurve(lanepoints, splitori.ToLineString());
                //        lanepoints = RemoveDuplicatePts(lanepoints);
                //        var build_ex_splits = SplitLine(splitori, lanepoints);
                //        if (build_ex_splits.Count > 0)
                //        {
                //            splitori = build_ex_splits.First();
                //            if (splitori.Length > splitoribackup.Length)
                //            {
                //                //处理生成的车道线长度比原始车道线长，来判断是否按照新长度生成的情况——因为延长出去的车道线有可能在平行方向有阻碍其生成的车道线
                //                var splitori_test_ps = splitoribackup.P1;
                //                var splitori_test_pe = splitori.P1;
                //                var splitori_test_l = new LineSegment(splitori_test_ps, splitori_test_pe);
                //                splitori_test_l = splitori_test_l.Scale(0.5);
                //                var splitori_test_l_up = splitori_test_l.Translation(Vector(splitori_test_l).Normalize().GetPerpendicularVector() * MaxLength);
                //                var splitori_test_l_down = splitori_test_l.Translation(-Vector(splitori_test_l).Normalize().GetPerpendicularVector() * MaxLength);
                //                var splitori_test_bf_up = PolyFromLines(splitori_test_l, splitori_test_l_up);
                //                var splitori_test_bf_down = PolyFromLines(splitori_test_l, splitori_test_l_down);
                //                var lan_pts_up = new List<Coordinate>();
                //                var obs_pts_up = new List<Coordinate>();
                //                var lan_pts_down = new List<Coordinate>();
                //                var obs_pts_down = new List<Coordinate>();
                //                foreach (var ln in IniLanes.Select(e => e.Line.ToLineString()))
                //                {
                //                    lan_pts_up.AddRange(ln.Coordinates);
                //                    lan_pts_up.AddRange(ln.IntersectPoint(splitori_test_bf_up));
                //                    lan_pts_down.AddRange(ln.Coordinates);
                //                    lan_pts_down.AddRange(ln.IntersectPoint(splitori_test_bf_down));
                //                }
                //                foreach (var obs in ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_test_bf_up))
                //                {
                //                    obs_pts_up.AddRange(obs.Coordinates);
                //                    obs_pts_up.AddRange(obs.IntersectPoint(splitori_test_bf_up));
                //                }
                //                foreach (var obs in ObstaclesSpatialIndex.SelectCrossingGeometry(splitori_test_bf_down))
                //                {
                //                    obs_pts_down.AddRange(obs.Coordinates);
                //                    obs_pts_down.AddRange(obs.IntersectPoint(splitori_test_bf_down));
                //                }
                //                double sc_tol = 1.0001;
                //                splitori_test_bf_up = splitori_test_bf_up.Scale(sc_tol);
                //                splitori_test_bf_down = splitori_test_bf_down.Scale(sc_tol);
                //                splitori_test_bf_up = splitori_test_bf_up.Scale(sc_tol);
                //                splitori_test_bf_down = splitori_test_bf_down.Scale(sc_tol);
                //                lan_pts_up = lan_pts_up.Where(p => splitori_test_bf_up.Contains(p)).ToList();
                //                lan_pts_down = lan_pts_down.Where(p => splitori_test_bf_down.Contains(p)).ToList();
                //                obs_pts_up = obs_pts_up.Where(p => splitori_test_bf_up.Contains(p)).ToList();
                //                obs_pts_down = obs_pts_down.Where(p => splitori_test_bf_down.Contains(p)).ToList();
                //                var uplanedis = 0.0;
                //                var downlanedis = 0.0;
                //                var upobsdis = 0.0;
                //                var downobsdis = 0.0;
                //                uplanedis = lan_pts_up.Count() > 0 ? splitori.ClosestPoint(lan_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(lan_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                //                downlanedis = lan_pts_down.Count() > 0 ? splitori.ClosestPoint(lan_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(lan_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                //                upobsdis = obs_pts_up.Count() > 0 ? splitori.ClosestPoint(obs_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(obs_pts_up.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                //                downobsdis = obs_pts_down.Count() > 0 ? splitori.ClosestPoint(obs_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()).Distance(obs_pts_down.OrderBy(p => splitori.ClosestPoint(p).Distance(p)).First()) : double.PositiveInfinity;
                //                if (upobsdis < uplanedis && downobsdis < downlanedis)
                //                    splitoribackup = splitori;
                //            }
                //        }
                //    }
                //    splitori = splitoribackup;
                //    Lane lan = new Lane(splitori, vec);
                //    paras.LanesToAdd.Add(lan);
                //    paras.LanesToAdd.Add(new Lane(splitori, -vec));
                //    paras.CarBoxesToAdd.Add(PolyFromLine(splitori));
                //    quitcycle = true;
                //    generate = true;
                //    break;
                //}
                #endregion

                //最后的几个逻辑判断
                double dis_connected_double = 0;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                {
                    dis_connected_double = DisVertCarLengthBackBack + DisLaneWidth / 2;
                }
                //生成车道线是否与原车道线完全错位
                if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;

                #region 针对尽端环通的处理
                var split_un_loopthrough_cut = new LineSegment(split);
                var generated_LoopThroughEnd = false;
                if (LoopThroughEnd)
                {
                    var distance_allow_generate_loopthrough_end = DisConsideringLoopThroughEnd;
                    if (split.Length >= distance_allow_generate_loopthrough_end)
                    {
                        var dis_singleModule_depth = DisCarAndHalfLane + CollisionD - CollisionTOP;
                        // 只有尽端和原始车道线尽端对齐(在允许值范围内)，才生成尽端环通的车道线
                        double dis_to_iniLane_end_allow_generate = DisBackBackModulus;//大概值
                                                                                      //起点不连通
                        var cond_start_disconnected = !IsConnectedToLane(split,true);
                        //终点不连通
                        var cond_end_disconnected = !IsConnectedToLane(split, false);
                        if (cond_start_disconnected || cond_end_disconnected)
                        {
                            var generateLanePt = split.P0.Translation(Vector(split).Normalize() * dis_singleModule_depth);
                            if (cond_end_disconnected)
                                generateLanePt = split.P1.Translation(-Vector(split).Normalize() * dis_singleModule_depth);
                            var generateLanePt_on_iniLane = generateLanePt.Translation(-vec * DisBackBackModulus);
                            if (splitback.P0.Distance(generateLanePt_on_iniLane) < dis_to_iniLane_end_allow_generate || splitback.P1.Distance(generateLanePt_on_iniLane) < dis_to_iniLane_end_allow_generate)
                            {
                                var generateLane = new LineSegment(generateLanePt, generateLanePt_on_iniLane);
                                var generateLane_buffer = generateLane.Buffer(DisLaneWidth / 2);
                                generateLane_buffer = generateLane_buffer.Scale(ScareFactorForCollisionCheck);
                                var cond = ObstaclesSpatialIndex.SelectCrossingGeometry(generateLane_buffer).Count == 0;
                                cond = cond && Boundary.IntersectPoint(generateLane_buffer).Count() == 0;
                                foreach (var testlane in IniLanes)
                                    cond = cond && testlane.Line.IntersectPoint(generateLane_buffer).Count() == 0;
                                if (cond)
                                {
                                    generated_LoopThroughEnd=true;
                                    var perpVec = -Vector(split).Normalize();
                                    if (cond_end_disconnected)
                                        perpVec = Vector(split).Normalize();
                                    split = new LineSegment(generateLanePt, split.P1);
                                    if (cond_end_disconnected)
                                        split = new LineSegment(split_un_loopthrough_cut.P0, generateLanePt);
                                    var split_length = split.Length;
                                    var rest= CalRestLength(split_length);
                                    generateLane = generateLane.Translation(-perpVec * rest);
                                    split = new LineSegment(generateLanePt.Translation(-perpVec * rest), split.P1);
                                    if (cond_end_disconnected)
                                        split = new LineSegment(split_un_loopthrough_cut.P0, generateLanePt.Translation(-perpVec * rest));
                                    Lane endthrough_lane = new Lane(generateLane, perpVec);
                                    endthrough_lane.IsGeneratedForLoopThrough = true;
                                    endthrough_lane.NOTJUDGELAYOUTBYPARENT = true;
                                    paras.LanesToAdd.Add(endthrough_lane);
                                    


                                    splitback = split.Translation(-vec.Normalize() * DisBackBackModulus/2);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region 生成参数赋值
                paras.SetNotBeMoved = i;
                //pl第二个半模块
                var pl = PolyFromLines(split, splitback);
                var plback = pl.Clone();
                //plback第一个半模块
                plback = plback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                var split_splitori_points = plback.IntersectPoint(Boundary).Select(e => splitori.ClosestPoint(e)).ToList();
                
                var mod = new CarModule(plback, splitori, vec);
                int generate_module_count = paras.CarModulesToAdd.Count;
                //第一个半模块生成车道线长度以原始车道线为准，不超过它
                if (splitori.Length > lane.Length)
                {
                    var lane_ini_pair = lane.Translation(vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                    var pl_initial = PolyFromLines(lane, lane_ini_pair);
                    plback = pl_initial;
                    mod = new CarModule(pl_initial, lane, vec);
                }

                //生成车道长度的权重赋值
                if (generate_lane_length > 0)
                {
                    paras.SetNotBeMoved = -1;
                }
                if (split.Length >= generate_lane_length)
                {
                    generate_lane_length = split.Length;
                    if (splitori.Length / 2 > lane.Length)
                        generate_lane_length = split.Length / 3;
                }
                if (generate_lane_length <= 0 && split.Length < generate_lane_length)
                    continue;

                //如果生成车道双向连通，减除多的半车道无法排车部分
                if (generate_lane_length - dis_connected_double > 0)
                    generate_lane_length -= dis_connected_double;
                //将第一个半车道模块加入生成参数
                mod.IsInBackBackModule = true;
                paras.CarModulesToAdd.Add(mod);
                paras.CarBoxPlusToAdd.Add(new CarBoxPlus(plback));
                paras.CarBoxesToAdd.Add(plback);
                generate = true;

                //判断生成车道前方是否已有平行阻碍其生成的车道
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
                            //从生成参数中剔除已加入的第一个半车道模块————在后续RestLane部分再生成车位
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
                        Lane ln = new Lane(split_un_loopthrough_cut, vec);
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
                        Lane ln = new Lane(split_un_loopthrough_cut, vec);
                        paras.LanesToAdd.Add(ln);
                    }
                }
                //如果只生成一个modulebox，宽度是7850，车位是5300，如果在后续生成车道的过程中有可能碰车位，这时应该缩短车道，作特殊处理
                if (paras.CarModulesToAdd.Count - generate_module_count == 1)
                {
                    paras.CarModulesToAdd[paras.CarModulesToAdd.Count - 1].IsSingleModule = true;
                }
                #endregion

                #region 如果生成的车道线太长了，将一根在中间分为两根
                var tmpLanesToAdd = new List<Lane>();
                var generate_split_lanes = new List<LineSegment>();
                if (paras.CarModulesToAdd.Count == 1)
                {
                    if (split.Length> DisAllowMaxLaneLength * 2)
                    {
                        var para_module = new CarModule(pl, split, -vec);
                        var generate_line = para_module.Line;
                        var pt_on_generate_lane = generate_line.MidPoint;
                        var pt_on_iniLane = lane.ClosestPoint(pt_on_generate_lane);
                        var split_lane = new LineSegment(pt_on_generate_lane, pt_on_iniLane);
                        //向量垂直，即剪切线在两根车道范围内
                        if (Vector(split_lane).Normalize().Dot(Vector(lane).Normalize()) == 0)
                        {
                            var split_vec = Vector(split_lane).Normalize().GetPerpendicularVector();
                            var lane_a = new Lane(split_lane, split_vec);
                            lane_a.IsGeneratedForRestrictLength = true;
                            lane_a.NOTJUDGELAYOUTBYPARENT = true;
                            var lane_b = new Lane(split_lane, -split_vec);
                            lane_b.IsGeneratedForRestrictLength = true;
                            lane_b.NOTJUDGELAYOUTBYPARENT= true;
                            tmpLanesToAdd.Add(lane_a);
                            tmpLanesToAdd.Add(lane_b);
                            generate_split_lanes.Add(split_lane);
                        }
                    }
                }
                else if (paras.CarModulesToAdd.Count == 2)
                {
                    var generate_module_paras = paras.CarModulesToAdd
                   .Where(e => lane.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) > 10)
                   .Where(e => e.Vec.Dot(vec) < 0)
                   .Where(e => e.Line.Length > DisAllowMaxLaneLength * 2).ToList();

                    foreach (var para in generate_module_paras)
                    {
                        var generate_line = para.Line;
                        var pt_on_generate_lane = generate_line.MidPoint;
                        var pt_on_iniLane = lane.ClosestPoint(pt_on_generate_lane);
                        if (pt_on_iniLane.Distance(lane.ClosestPoint(pt_on_generate_lane, true)) < 1)
                        {
                            var split_lane = new LineSegment(pt_on_generate_lane, pt_on_iniLane);
                            var split_vec = Vector(split_lane).Normalize().GetPerpendicularVector();
                            tmpLanesToAdd.Add(new Lane(split_lane, split_vec) { NOTJUDGELAYOUTBYPARENT = true, IsGeneratedForRestrictLength = true });
                            tmpLanesToAdd.Add(new Lane(split_lane, -split_vec) { NOTJUDGELAYOUTBYPARENT = true, IsGeneratedForRestrictLength = true });
                            generate_split_lanes.Add(split_lane);
                        }
                    }
                }
                paras.LanesToAdd.AddRange(tmpLanesToAdd);
                foreach (var generate_split_lane in generate_split_lanes)
                {
                    var generate_split_lane_buffer = generate_split_lane.Buffer(DisLaneWidth / 2);
                    var crossed_modules = paras.CarModulesToAdd.Where(e => e.Box.IntersectPoint(generate_split_lane_buffer).Count()>0);
                    var temp_modules = new List<CarModule>();
                    foreach (var module in crossed_modules)
                    {
                        var cutter_edges = generate_split_lane_buffer.GetEdges().OrderByDescending(e => e.Length).Take(2)
                            .Select(e => e.Scale(1.1)).ToList();
                        var cutter = PolyFromLines(cutter_edges[0], new LineSegment(cutter_edges[1].P1, cutter_edges[1].P0));
                        var lstrings = SplitCurveByNTS(module.Box, cutter)
                            .Where(e => e.Length > 1);
                        var boxes = lstrings.Select(e => PolyFromPoints(e.Coordinates.ToList())).ToList();
                        var lines = SplitLine(module.Line, generate_split_lane_buffer)
                            .Where(e => generate_split_lane_buffer.ClosestPoint(e.MidPoint).Distance(e.MidPoint) > 10).ToList();
                        if (boxes.Count == lines.Count && boxes.Count() != 0)
                        {
                            lines = lines.OrderBy(e => boxes.First().ClosestPoint(e.MidPoint).Distance(e.MidPoint)).ToList();
                            for (int k = 0; k < lines.Count; k++)
                            {
                                CarModule module1 = new CarModule();
                                module1.Box = boxes[k];
                                module1.Line = lines[k];
                                module1.Vec = module.Vec;
                                module1.IsInVertUnsureModule = module.IsInVertUnsureModule;
                                module1.GenerateCars = module.GenerateCars;
                                module1.IsInBackBackModule = module.IsInBackBackModule;
                                module1.IsSingleModule = module.IsSingleModule;
                                temp_modules.Add(module1);
                            }
                        }
                    }
                    paras.CarModulesToAdd = paras.CarModulesToAdd.Except(crossed_modules).ToList();
                    paras.CarModulesToAdd.AddRange(temp_modules);
                }
                #endregion
            }
        }
        double CalRestLength(double length)
        {
            length -= DisLaneWidth;
            length -= DisPillarLength;
            var d = length % (DisPillarLength + 3 * DisVertCarWidth);
            if (d > DisPillarLength + DisVertCarWidth)
            {
                d -= DisPillarLength + DisVertCarWidth;
                d = d % DisVertCarWidth;
            }
            return d;
        }
    }
}
