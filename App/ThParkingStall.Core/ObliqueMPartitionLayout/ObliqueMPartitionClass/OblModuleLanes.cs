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
                    case ((int)LayoutDirection.LENGTH):
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

            #region 与边界的判断及处理，返回车道位置：1模块的位置
            //偏移一个模块+半车道后进行条件判断，针对单排模块一个背靠背模块宽度（它不需要生成背后的车道）——但这个宽度似乎也有冗余
            var offsetlane = new LineSegment(lane);
            if (isBackBackModule)
            {
                offsetlane = TranslateReservedConnection(offsetlane, vec * (DisBackBackModulus + DisLaneWidth / 2));
            }
            else
            {
                offsetlane = TranslateReservedConnection(offsetlane, vec * (DisModulus));
            }

            //与边界相交：如果是背靠背，1模块+1半车道相交后再偏移回1模块深度的距离作与边界相交处理，再偏移至1模块+1半车道的位置
            offsetlane = offsetlane.Scale(20);
            var _splits = SplitLine(offsetlane, Boundary);
            var splits = new List<LineSegment>();
            if (!isBackBackModule)
            {
                splits = _splits;
            }
            else
            {
                foreach (var s in _splits)
                {
                    var k = TranslateReservedConnection(s, -vec * DisLaneWidth / 2);
                    splits.AddRange(SplitLine(k, Boundary)
                        .Select(e => TranslateReservedConnection(e, vec * DisLaneWidth / 2)));
                }
            }
            //splits位置：背靠背1模块+1半车道，单模块：1模块

            //与边界分割处理后的车道线Buffer与边界判断，如果是背靠背模块，判断完的车道线由1模块+1半车道位置返回1模块的位置
            var linesplitbounds = splits
                .Where(e =>
                {
                    var l = new LineSegment(e);
                    //针对背靠背模块
                    //返回1模块的位置作半车道的buffer与边界相交判断，如果相交，返回
                    l = TranslateReservedConnection(l, -vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    var bf = BufferReservedConnection(l, DisLaneWidth / 2 - 1);
                    bf = bf.Scale(ScareFactorForCollisionCheck);
                    var result = bf.IntersectPoint(Boundary).Count() == 0;
                    //modified:有的边界在BOX内折了一下
                    var edge_a = TranslateReservedConnection(l,Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth / 2 - 1));
                    var edge_b = TranslateReservedConnection(l, -Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth / 2 - 1));
                    var pl = PolyFromLines(edge_a, edge_a.Translation(-Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth - 2)));
                    if (edge_b.Length < edge_a.Length)
                        pl = PolyFromLines(edge_b, edge_b.Translation(Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth - 2)));
                    pl = pl.Scale(ScareFactorForCollisionCheck);
                    result = pl.IntersectPoint(Boundary).Count() == 0 || result;
                    //end modified
                    //返回1模块+1半车道的位置作半车道的buffer与最外层原始边界相交判断，如果相交，返回
                    l = TranslateReservedConnection(l, vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    bf = BufferReservedConnection(l, DisLaneWidth / 2 - 1);
                    bf = bf.Scale(ScareFactorForCollisionCheck);
                    //if (bf.IntersectPoint(OutBoundary).Count() > 0)
                    //    result = false;
                    //modified:有的边界在BOX内折了一下
                    edge_a = TranslateReservedConnection(l, Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth / 2-1));
                    edge_b = TranslateReservedConnection(l, -Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth / 2 - 1));
                    pl = PolyFromLines(edge_a, edge_a.Translation(-Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth - 2)));
                    if (edge_b.Length > edge_a.Length)
                        pl = PolyFromLines(edge_b, edge_b.Translation(Vector(l).Normalize().GetPerpendicularVector() * (DisLaneWidth - 2)));
                    pl = pl.Scale(ScareFactorForCollisionCheck);
                    if(pl.IntersectPoint(OutBoundary).Count() > 0 && bf.IntersectPoint(OutBoundary).Count() > 0)
                        result = false;
                    //end modified
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
                    else return e;
                });
            #endregion

            bool generate = false;
            var quitcycle = false;
            STRtree<Polygon> carBoxesStrTree = new STRtree<Polygon>();
            CarBoxes.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));
            foreach (var linesplitbound in linesplitbounds)
            {
                #region 与已有的车道模块相交处理，返回车道位置：1模块的位置
                var linesplitboundback = new LineSegment(linesplitbound);
                if (isBackBackModule)
                    linesplitboundback = TranslateReservedConnection(linesplitboundback, (-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2)));
                else
                    linesplitboundback = TranslateReservedConnection(linesplitboundback, (-vec * (DisVertCarLength + DisLaneWidth / 2)));
                //背靠背对面一排的车位+半车道模块Box内含的车道模块用来分割车道线
                var plcarbox = PolyFromLines(linesplitbound, linesplitboundback);
                plcarbox = plcarbox.Scale(ScareFactorForCollisionCheck);
                var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false, true)
                    .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                    .Where(e => !IsInAnyBoxes(e.MidPoint/*.TransformBy(Matrix3d.Displacement(-vec.GetNormal())) * 200*/, carBoxesStrTree, true))
                    .Where(e => IsConnectedToLane(e));
                #endregion

                #region 一种CASE：生成背靠背模块的车道线前方有与车道模块的短边平行，做一次分割操作，返回车道位置：1模块的位置
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
                #endregion

                foreach (var lnesplit in fixlinesplitcarboxes)
                {
                    #region 与障碍物的相交处理,返回车道位置：1模块+半车道的位置
                    //与障碍物相交判断，如果是背靠背模块，往回偏1车位+1车道位置判断——理论上用1车位+半车道就够了，不知道
                    //当时为什么用1车道，如果后续调试有问题，可以改回半车道试试
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
                    //把1模块位置的车道线在往外偏半车道后，与前述车道线组合成Polyline与障碍物作分割处理
                    linesplit = TranslateReservedConnection(linesplit, vec * DisLaneWidth / 2);
                    plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    //分割车道线
                    var obsplits = SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        .Where(e =>
                        {
                            var tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(e.MidPoint)).Cast<Polygon>().ToList();
                            return !IsInAnyPolys(e.MidPoint, tmpobs);
                        })
                        .Where(e =>
                        {
                            //与原始车道线模块不相接判断，回到1模块的位置
                            var tmpline = new LineSegment(e);
                            tmpline = TranslateReservedConnection(tmpline, -vec * DisLaneWidth / 2);
                            if (!IsConnectedToLane(tmpline)) return false;
                            //生成车道线与原车道线平行完全错位了，没有共同部分的处理
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
                //车道返回1模块的位置
                split = TranslateReservedConnection(split, -vec * DisLaneWidth / 2);
                var quit_repeat = false;

                //如果是重复车道（在半车道容差范围内），直接返回
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

                #region 原始车道线部分的障碍物相交处理，返回车道位置：splitori原始车道线,splitback半模块车道线
                //把车道线（splitori）回到原始车道线的位置
                var splitori = TranslateReservedConnection(splitback, -vec * (DisBackBackModulus + DisLaneWidth));
                splitback = TranslateReservedConnection(splitori, vec * (DisVertCarLengthBackBack + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;
                //ploritolane:原始车道后退半车道，半模块车道线位置
                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = TranslateReservedConnection(splitori, vec * DisLaneWidth / 2);

                //原始车道线作Buffer，与障碍物作相交判断处理
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
                #endregion

                //生成模块与车道线错开且原车道线碰障碍物
                if (((lane.ClosestPoint(splitori.P0).Distance(splitori.P0) >/* 5000*/splitori.Length / 3
                    || lane.ClosestPoint(splitori.P1).Distance(splitori.P1) > splitori.Length / 3)
                    && ObstaclesSpatialIndex.SelectCrossingGeometry(ploritolane).Cast<Polygon>().Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0).Count() > 0)
                    || IsInAnyBoxes(splitori.MidPoint, carBoxesStrTree))
                {         
                    continue;
                }
                //预生成车道的长度等条件判断
                double dis_connected_double = 0;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                {
                    dis_connected_double = DisVertCarLengthBackBack + DisLaneWidth / 2;
                }
                if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;

                #region 参数赋值
                paras.SetNotBeMoved = i;
                //split1模块车道线，splitback半模块车道线
                //pl第二个半模块，plback第一个半模块
                var pl = PolyFromLines(split, splitback);
                var plback = pl.Clone();
                plback = plback.Translation(-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                var mod = new CarModule(plback, splitori, vec);
                int generate_module_count = paras.CarModulesToAdd.Count;

                //如果在原车道线长度预生成的车道线长度大于原始车道线长度，以原始车道线来生成
                if (splitori.Length > lane.Length)
                {
                    var lane_ini_pair = TranslateReservedConnection(lane, vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                    var pl_initial = PolyFromLines(lane, lane_ini_pair);
                    plback = pl_initial;
                    mod = new CarModule(pl_initial, lane, vec);
                }
                //生成车位长度的权重赋值
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
                else
                    continue;
                //如果生成车道双向连通，长度减掉1车位+半车道权重长度
                if (generate_lane_length - dis_connected_double > 0)
                    generate_lane_length -= dis_connected_double;

                //将第一个半模块添加进生成参数
                mod.IsInBackBackModule = true;
                paras.CarModulesToAdd.Add(mod);
                paras.CarBoxPlusToAdd.Add(new CarBoxPlus(plback));
                paras.CarBoxesToAdd.Add(plback);
                generate = true;

                double dis_to_move = 0;
                LineSegment perpLine = new LineSegment(new Coordinate(0, 0), new Coordinate(0, 0));
                //判断生成车道的前方是否已有平行车道
                if (HasParallelLaneForwardExisted(split, vec, /*28700 - 15700*//*20220704*/DisLaneWidth / 2 + DisVertCarWidth * 2/*DisLaneWidth+DisVertCarLength*/, /*19000 - 15700*//*0*/3000, ref dis_to_move, ref perpLine, ref para_lanes_add))
                {
                    //移除已添加生成参数
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
                    //将第二个半模块添加进生成参数
                    var segs = pl.GetEdges().OrderByDescending(e => e.Length).Take(2);
                    //如果第二个半模块的车道长度与半模块位置长度相差太大，只添加车道，后期进入RestLane部分排布车位
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
                #endregion
            }
        }
        private double GenerateLaneForLayoutingSingleVertModule(ref GenerateLaneParas paras)
        {
            return -1;
        }
    }
}
