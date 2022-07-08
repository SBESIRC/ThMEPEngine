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
        public void PreProcess()
        {
            var iniLanes = IniLanes.Select(e => e.Line).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var line = IniLanes[i].Line;
                var pl = line.Buffer(DisLaneWidth / 2 - 1);
                var points = new List<Coordinate>();
                STRtree<Polygon> strTree = new STRtree<Polygon>();
                foreach (var cutter in Obstacles) strTree.Insert(cutter.EnvelopeInternal, cutter);
                var selectedGeos = strTree.Query(pl.EnvelopeInternal);
                foreach (var obj in selectedGeos)
                    points.AddRange(obj.IntersectPoint(pl));
                foreach (var obj in Obstacles)
                {
                    points.AddRange(obj.Coordinates);
                    //points.AddRange(obj.IntersectPoint(pl));
                }
                points = points.Where(e => pl.Contains(e) || pl.ClosestPoint(e).Distance(e) < 0.001)
                    .Select(e => line.ClosestPoint(e)).ToList();
                var splits = SplitLine(line, points);
                for (int j = 0; j < splits.Count; j++)
                {
                    foreach (var obj in Obstacles)
                        if (obj.Contains(splits[j].MidPoint))
                        {
                            splits.RemoveAt(j);
                            j--;
                            break;
                        }
                }
                splits = splits.OrderByDescending(e => ClosestPointInCurves(e.MidPoint, Walls)).ToList();
                if (splits.Count > 0)
                {
                    var lane = splits.First();
                    IniLanes[i].Line = lane;
                }
                else
                {
                    IniLanes.RemoveAt(i);
                    i--;
                }
            }
            if (RampList.Count > 0)
            {
                var ramp = RampList[0];
                var pt = ramp.InsertPt.Coordinate;
                var pl = ramp.Area;
                var seg = pl.GetEdges().OrderByDescending(t => t.Length).First();
                var vec = Vector(seg).Normalize();
                var ptest = pt.Translation(vec);
                if (pl.Contains(ptest)) vec = -vec;
                var rampline = LineSegmentSDL(pt, vec, MaxLength);
                rampline = SplitLine(rampline, IniLanes.Select(e => e.Line).ToList()).OrderBy(t => t.ClosestPoint(pt, false).Distance(pt)).First();
                var prepvec = vec.GetPerpendicularVector();
                IniLanes.Add(new Lane(rampline, prepvec));
                IniLanes.Add(new Lane(rampline, -prepvec));
                OriginalLanes.Add(rampline);
                IniLaneBoxes.Add(rampline.Buffer(DisLaneWidth / 2));
                for (int i = 0; i < IniLanes.Count; i++)
                {
                    var line = IniLanes[i].Line;
                    var nvec = IniLanes[i].Vec;
                    var intersect_points = rampline.IntersectPoint(line).ToList();
                    intersect_points = SortAlongCurve(intersect_points, line.ToLineString());
                    intersect_points = RemoveDuplicatePts(intersect_points, 1);
                    var splits = SplitLine(line, intersect_points);
                    if (splits.Count() > 1)
                    {
                        IniLanes.RemoveAt(i);
                        IniLanes.Add(new Lane(splits[0], nvec));
                        IniLanes.Add(new Lane(splits[1], nvec));
                        break;
                    }
                }
            }
        }
        public void GenerateLanes()
        {
            int count = 0;
            while (true)
            {
                count++;
                if (count > 20) break;

                SortLaneByDirection(IniLanes, LayoutMode);
                GenerateLaneParas paras_integral_modules = new GenerateLaneParas();
                GenerateLaneParas paras_adj_lanes = new GenerateLaneParas();
                GenerateLaneParas paras_between_two_builds = new GenerateLaneParas();
                GenerateLaneParas paras_single_vert_modules = new GenerateLaneParas();

                var length_integral_modules = ((int)GenerateIntegralModuleLanesOptimizedByRealLength(ref paras_integral_modules, true));
                var length_adj_lanes = ((int)GenerateAdjacentLanesOptimizedByRealLength(ref paras_adj_lanes));
                var length_between_two_builds = ((int)GenerateLaneBetweenTwoBuilds(ref paras_between_two_builds));
                var length_single_vert_modules = (int)GenerateLaneForLayoutingSingleVertModule(ref paras_single_vert_modules);
                var max = Math.Max(Math.Max(length_integral_modules, length_adj_lanes), Math.Max(length_adj_lanes, length_between_two_builds));
                max = Math.Max(max, length_single_vert_modules);
                if (max > 0)
                {
                    if (max == length_integral_modules)
                    {
                        RealizeGenerateLaneParas(paras_integral_modules);
                    }
                    else if (max == length_adj_lanes)
                    {
                        RealizeGenerateLaneParas(paras_adj_lanes);
                    }
                    else if (max == length_between_two_builds)
                    {
                        RealizeGenerateLaneParas(paras_between_two_builds);
                    }
                    else
                    {
                        RealizeGenerateLaneParas(paras_single_vert_modules);
                    }
                }
                else
                {
                    break;
                }
            }
        }
        public void GeneratePerpModules()
        {

        }
        public void GenerateCarsInModules()
        {

        }
        public void ProcessLanes(ref List<Lane> Lanes, bool preprocess = false)
        {

        }
        public void GenerateCarsOnRestLanes()
        {

        }
        public void PostProcess()
        {

        }
        private void RealizeGenerateLaneParas(GenerateLaneParas paras)
        {
            if (paras.SetNotBeMoved != -1) IniLanes[paras.SetNotBeMoved].CanBeMoved = false;
            if (paras.SetGStartAdjLane != -1) IniLanes[paras.SetGStartAdjLane].GStartAdjLine = true;
            if (paras.SetGEndAdjLane != -1) IniLanes[paras.SetGEndAdjLane].GEndAdjLine = true;
            if (paras.LanesToAdd.Count > 0)
            {
                IniLanes.AddRange(paras.LanesToAdd);
                foreach (var lane in paras.LanesToAdd)
                {
                    if (IsConnectedToLaneDouble(lane.Line)) IniLanes.Add(lane);
                    else
                    {
                        if (IsConnectedToLane(lane.Line, false))
                            lane.Line = new LineSegment(lane.Line.P1, lane.Line.P0);
                        //如果只生成一个modulebox，宽度是7850，车位是5300，如果在后续生成车道的过程中有可能碰车位，这时应该缩短车道，作特殊处理
                        var modified_lane = lane.Line;
                        foreach (var box in CarModules)
                        {
                            var cond_dis = box.Box.Contains(lane.Line.P1) || box.Box.ClosestPoint(lane.Line.P1).Distance(box.Box.ClosestPoint(lane.Line.P1)) < 1;
                            var cond_character = box.IsSingleModule;
                            var cond_perp = IsPerpLine(box.Line, modified_lane);
                            if (cond_dis && cond_character && cond_perp)
                            {
                                var end = lane.Line.P1.Translation(-Vector(lane.Line).Normalize() * (DisVertCarLength - DisVertCarLengthBackBack));
                                modified_lane = new LineSegment(lane.Line.P0, end);
                            }
                        }
                        lane.Line = modified_lane;
                        IniLanes.Add(lane);
                    }
                }
            }
            if (paras.CarBoxesToAdd.Count > 0)
            {
                CarBoxes.AddRange(paras.CarBoxesToAdd);
                CarBoxesSpatialIndex.Update(paras.CarBoxesToAdd.Cast<Geometry>().ToList(), new List<Geometry>());
            }
            if (paras.CarBoxPlusToAdd.Count > 0) CarBoxesPlus.AddRange(paras.CarBoxPlusToAdd);
            if (paras.CarModulesToAdd.Count > 0) CarModules.AddRange(paras.CarModulesToAdd);
        }
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
                offsetlane = offsetlane.Translation(vec * (DisBackBackModulus + DisLaneWidth / 2));
            else
                offsetlane = offsetlane.Translation(vec * (DisModulus));
            offsetlane = offsetlane.Scale(20);
            //与边界相交
            var _splits = SplitLine(offsetlane, Boundary);
            var linesplitbounds =/* SplitLine(offsetlane, Boundary)*/
                _splits
                .Where(e =>
                {
                    return true;
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
            foreach (var xlinesplitbound in linesplitbounds)
            {
                var linesplitbound = xlinesplitbound;
                var locp = linesplitbound.MidPoint;
                linesplitbound.P0 = linesplitbound.P0.Translation(-Vector(linesplitbound).Normalize() * MaxLength);
                linesplitbound.P1 = linesplitbound.P1.Translation(Vector(linesplitbound).Normalize() * MaxLength);
                linesplitbound = SplitLine(linesplitbound, OutBoundary).OrderBy(e => e.ClosestPoint(locp).Distance(locp)).First();
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
                    var locp_k = k.MidPoint;
                    k.P0 = k.P0.Translation(-Vector(k).Normalize() * MaxLength);
                    k.P1 = k.P1.Translation(Vector(k).Normalize() * MaxLength);
                    k = SplitLine(k, OutBoundary).OrderBy(e => e.ClosestPoint(locp_k).Distance(locp_k)).First();
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
                            var locp_ep = ep.MidPoint;
                            ep.P0 = ep.P0.Translation(-Vector(ep).Normalize() * MaxLength);
                            ep.P1 = ep.P1.Translation(Vector(ep).Normalize() * MaxLength);
                            ep = SplitLine(ep, OutBoundary).OrderBy(t => t.ClosestPoint(locp_ep).Distance(locp_ep)).First();
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
                            tmpline.P0 = tmpline.P0.Translation(-Vector(tmpline).Normalize() * MaxLength);
                            tmpline.P1 = tmpline.P1.Translation(Vector(tmpline).Normalize() * MaxLength);
                            tmpline = SplitLine(tmpline, OutBoundary).OrderBy(t => t.ClosestPoint(tmpline.MidPoint).Distance(tmpline.MidPoint)).First();
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


                //var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                ////if (splits_bd.Count() == 0) continue;
                //splitori = splits_bd.First();
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
        private double GenerateAdjacentLanesOptimizedByRealLength(ref GenerateLaneParas paras)
        {
            return -1;
        }
        private double GenerateLaneBetweenTwoBuilds(ref GenerateLaneParas paras)
        {
            return -1;
        }
        private double GenerateLaneForLayoutingSingleVertModule(ref GenerateLaneParas paras)
        {
            return -1;
        }
    }
}
