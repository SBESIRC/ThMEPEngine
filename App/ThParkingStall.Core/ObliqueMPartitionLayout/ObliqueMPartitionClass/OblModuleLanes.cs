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
                }
            }
            generate_lane_length = max_length;
            return generate_lane_length;
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
            {
                offsetlane = TranslateReservedConnection(offsetlane, vec * (DisBackBackModulus + DisLaneWidth / 2));
                //offsetlane = offsetlane.Translation(vec * (DisBackBackModulus + DisLaneWidth / 2));
            }
            else
            {
                offsetlane = TranslateReservedConnection(offsetlane, vec * (DisModulus));
                //offsetlane = offsetlane.Translation(vec * (DisModulus));
            }
            offsetlane = offsetlane.Scale(20);
            //与边界相交

            var _splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
            var splits = new List<LineSegment>();
            foreach (var s in _splits)
            {
                //var k = s.Translation(-vec * DisLaneWidth / 2);
                var k = TranslateReservedConnection(s, -vec * DisLaneWidth / 2);
                if (!isBackBackModule)
                    k = s;
                splits.AddRange(SplitBufferLineByPoly(k, DisLaneWidth / 2, Boundary)
                    .Select(e =>
                    {
                        if (isBackBackModule) return TranslateReservedConnection(e, vec * DisLaneWidth / 2);
                        else return e;
                    }));
            }
            var linesplitbounds =/* SplitLine(offsetlane, Boundary)*/
                splits
                .Where(e =>
                {
                    var l = new LineSegment(e);
                    l = TranslateReservedConnection(l, -vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    var bf = BufferReservedConnection(l, DisLaneWidth / 2 - 1);
                    bf = bf.Scale(ScareFactorForCollisionCheck);
                    var result = bf.IntersectPoint(Boundary).Count() == 0;
                    //var result = true;
                    l = TranslateReservedConnection(l, vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    bf = BufferReservedConnection(l, DisLaneWidth / 2 - 1);
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
                        e = TranslateReservedConnection(e, -vec * (DisLaneWidth / 2));
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
                    linesplitboundback = TranslateReservedConnection(linesplitboundback, (-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2)));
                else
                    linesplitboundback = TranslateReservedConnection(linesplitboundback, (-vec * (DisVertCarLength + DisLaneWidth / 2)));
                var plcarbox = PolyFromLines(linesplitbound, linesplitboundback);
                plcarbox = plcarbox.Scale(ScareFactorForCollisionCheck);
                var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false, true)
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
                    k = TranslateReservedConnection(k, vec * DisLaneWidth / 2);
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
                    fixlinesplitcarboxes.AddRange(SplitLineBySpacialIndexInPoly(k, plcarboxfix, spindex, false, true)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        .Where(e => !IsInAnyBoxes(e.MidPoint, boxs, true))
                        .Where(e =>
                        {
                            var ep = new LineSegment(e);
                            ep = TranslateReservedConnection(ep, -vec * DisLaneWidth / 2);
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
                        offsetback = TranslateReservedConnection(offsetback, -vec * (DisVertCarLengthBackBack + DisLaneWidth));
                    else
                        offsetback = TranslateReservedConnection(offsetback, -vec * (DisVertCarLength + DisLaneWidth / 2));
                    var plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    if (!allow_through_build)
                    {
                        if (SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false).Count > 1) continue;
                    }
                    //与障碍物相交
                    linesplit = TranslateReservedConnection(linesplit, vec * DisLaneWidth / 2);
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
                            tmpline = TranslateReservedConnection(tmpline, -vec * DisLaneWidth / 2);
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
                        ////孤立单排
                        //if (!CalculateSingleVertModule(obsplits, lane, linesplit, vec, carBoxesStrTree, ref generate_lane_length, i, ref paras))
                        //    continue;
                    }
                    if (quitcycle) break;
                }
                if (quitcycle) break;
            }
            return generate_lane_length;
        }
        private void CalculateBackBackLength(IEnumerable<LineSegment> obsplits, LineSegment lane, Vector2D vec, STRtree<Polygon> carBoxesStrTree
            , ref double generate_lane_length, int i, ref GenerateLaneParas paras, ref bool quitcycle, ref bool generate, ref List<LineSegment> para_lanes_add)
        {
            foreach (var slit in obsplits)
            {
                var split = slit;
                var splitback = new LineSegment(split);
                split = TranslateReservedConnection(split, -vec * DisLaneWidth / 2);
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
                splitback = TranslateReservedConnection(splitback, -vec * (DisVertCarLengthBackBack + DisLaneWidth));
                var splitori = new LineSegment(splitback);
                splitori = TranslateReservedConnection(splitori, -vec * (DisVertCarLengthBackBack + DisLaneWidth));

                //temp20220711
                //var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                //if (splits_bd.Count() == 0) continue;
                //splitori = splits_bd.First();
                splitback = TranslateReservedConnection(splitori, vec * (DisVertCarLengthBackBack + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;

                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = TranslateReservedConnection(splitori, vec * DisLaneWidth / 2);
                var splitori_buffer = BufferReservedConnection(splitori, DisLaneWidth / 2);
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
                double dis_connected_double = 0;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                {
                    dis_connected_double = DisVertCarLengthBackBack + DisLaneWidth / 2;
                }
                if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;
                paras.SetNotBeMoved = i;
                var pl = PolyFromLines(split, splitback);
                splitback = TranslateReservedConnection(splitback, -vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                var splitbackbuffer = BufferReservedConnection(splitback, DisLaneWidth / 2);
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
                    var lane_ini_replace = TranslateReservedConnection(splitori, vec * ((DisVertCarLengthBackBack + DisLaneWidth / 2) + DisLaneWidth / 2));
                    var lane_ini_replace_pair = TranslateReservedConnection(lane_ini_replace, -vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
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
                        var lane_ini_pair = TranslateReservedConnection(lane, vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
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
        private double GenerateLaneForLayoutingSingleVertModule(ref GenerateLaneParas paras)
        {
            return -1;
        }
    }
}
