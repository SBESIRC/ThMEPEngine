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
        public void GeneratePerpModules()
        {
            double mindistance = DisLaneWidth / 2 + DisVertCarWidth * 4;
            var lanes = GeneratePerpModuleLanes(mindistance, DisBackBackModulus, true, null, true, null, true);
            GeneratePerpModuleBoxes(lanes);
        }
        public void GenerateCarsInModules()
        {

            //UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            //var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            //SortLaneByDirection(vertlanes, LayoutMode);

            var lanes = new List<Lane>();
            CarModules.Where(e => !e.IsInVertUnsureModule).ToList().ForEach(e => lanes.Add(new Lane(e.Line, e.Vec)));
            //lanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, null, true, lanes);
            List<double> lengths = new List<double>();
            lanes.ForEach(e => lengths.Add(e.Line.Length));
            ProcessLanes(ref lanes);
            var align_backback_for_align_rest = false;
            for (int i = 0; i < lanes.Count; i++)
            {
                var vl = lanes[i].Line;
                var generate_middle_pillar = CarModules[i].IsInBackBackModule;
                var isin_backback = CarModules[i].IsInBackBackModule;
                if (!GenerateMiddlePillars) generate_middle_pillar = false;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);

                //var start_onlane = new Coordinate();
                //double start_angle = Math.PI / 2;
                //foreach (var ln in IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line)))
                //{
                //    if (ln.ClosestPoint(line.P0).Distance(line.P0) < 10)
                //    {
                //        start_onlane = ln.ClosestPoint(line.P0);
                //        var move_onlane = start_onlane.Translation(lanes[i].Vec.Normalize()*100);
                //        var near_onlane = start_onlane.Translation(Vector(ln).Normalize());
                //        if(move_onlane.Distance(start_onlane)<move_onlane.Distance(near_onlane))
                //            near_onlane= start_onlane.Translation(-Vector(ln).Normalize());
                //        start_angle = new Vector2D(start_onlane, near_onlane).AngleTo(Vector(vl));
                //        start_angle = Math.Min(start_angle, Math.PI - start_angle);
                //        break;
                //    }
                //}

                line = SplitLine(line, IniLaneBoxes).OrderBy(e => e.MidPoint.Distance(line.MidPoint)).First();

                if (ClosestPointInLines(line.P0, line, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line))) < 10)
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * DisLaneWidth / 2);
                if (ClosestPointInLines(line.P1, line, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line))) < 10)
                    line.P1 = line.P1.Translation(-Vector(line).Normalize() * (DisLaneWidth / 2 + DisPillarLength));



                //if (line.Intersect(Boundary, Intersect.OnBothOperands).Count > 0)
                //{
                //    var lines = SplitLine(line, Boundary).Where(e => e.Length > 1)
                //        .Where(e => Boundary.Contains(e.GetCenter()) || ClosestPointInCurves(line.GetCenter(), OriginalLanes) == 0);
                //    if (lines.Count() > 0) line = lines.First();
                //    else continue;
                //}
                line = TranslateReservedConnection(line, lanes[i].Vec.Normalize() * DisLaneWidth / 2, false);
                var dis_start = ClosestPointInLines(line.P0, line, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line)));
                if (dis_start < DisLaneWidth / 2)
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * (DisLaneWidth / 2 - dis_start));
                var dis_end = ClosestPointInLines(line.P1, line, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line)));
                if (dis_end < DisLaneWidth / 2 + DisPillarLength)
                    line.P1 = line.P1.Translation(-Vector(line).Normalize() * (DisLaneWidth / 2 + DisPillarLength - dis_end));

                var lnbox = PolyFromLines(line, TranslateReservedConnection(line, lanes[i].Vec.Normalize() * DisVertCarLength, false));
                var intersects_lanbox = new List<Coordinate>();
                foreach (var box in IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line)).Select(e => BufferReservedConnection(e, DisLaneWidth / 2)))
                {
                    intersects_lanbox.AddRange(box.IntersectPoint(lnbox));
                }
                intersects_lanbox = intersects_lanbox.Select(e => line.ClosestPoint(e)).ToList();
                line = SplitLine(line, intersects_lanbox).OrderByDescending(e => e.Length).First();

                var judge_in_obstacles = false;
                if (lengths[i] != lanes[i].Line.Length) judge_in_obstacles = true;
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, lanes[i].Vec, DisVertCarWidth, DisVertCarLength, ref line_align_backback_rest, false, false, false, false, true, false, true, false, judge_in_obstacles, true, true, generate_middle_pillar, isin_backback, true);
                align_backback_for_align_rest = false;
                if (line_align_backback_rest.Length > 0)
                {
                    lanes.Insert(i + 1, new Lane(line_align_backback_rest, lanes[i].Vec));
                    var mod = new CarModule(PolyFromLines(line_align_backback_rest, line_align_backback_rest.Translation(lanes[i].Vec.Normalize() * DisVertCarLength)), line_align_backback_rest, lanes[i].Vec.Normalize());
                    mod.IsInBackBackModule = CarModules[i].IsInBackBackModule;
                    CarModules.Insert(i + 1, mod);
                    lengths.Insert(i + 1, line_align_backback_rest.Length);
                    align_backback_for_align_rest = true;
                }
            }
        }
        public void ProcessLanes(ref List<Lane> Lanes, bool preprocess = false)
        {

        }
        public void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
            ReDefinePillarDimensions();
            InsuredForTheCaseOfoncaveBoundary();
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
                var k= TranslateReservedConnection(s, -vec * DisLaneWidth / 2);
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
                    l = TranslateReservedConnection(l,-vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    var bf = BufferReservedConnection(l,DisLaneWidth / 2 - 1);
                    bf = bf.Scale(ScareFactorForCollisionCheck);
                    var result = bf.IntersectPoint(Boundary).Count() == 0;
                    //var result = true;
                    l = TranslateReservedConnection(l,vec * DisLaneWidth / 2);
                    l.P0 = l.P0.Translation(Vector(l).Normalize() * 10);
                    l.P1 = l.P1.Translation(-Vector(l).Normalize() * 10);
                    bf = BufferReservedConnection(l,DisLaneWidth / 2 - 1);
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
                        e = TranslateReservedConnection(e,-vec * (DisLaneWidth / 2));
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
                    linesplitboundback = TranslateReservedConnection(linesplitboundback,(-vec * (DisVertCarLengthBackBack + DisLaneWidth / 2)));
                else
                    linesplitboundback = TranslateReservedConnection(linesplitboundback,(-vec * (DisVertCarLength + DisLaneWidth / 2)));
                var plcarbox = PolyFromLines(linesplitbound, linesplitboundback);
                plcarbox = plcarbox.Scale(ScareFactorForCollisionCheck);
                var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false,true)
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
                    k = TranslateReservedConnection(k,vec * DisLaneWidth / 2);
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
                    fixlinesplitcarboxes.AddRange(SplitLineBySpacialIndexInPoly(k, plcarboxfix, spindex, false,true)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        .Where(e => !IsInAnyBoxes(e.MidPoint, boxs, true))
                        .Where(e =>
                        {
                            var ep = new LineSegment(e);
                            ep = TranslateReservedConnection(ep,-vec * DisLaneWidth / 2);
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
                        offsetback = TranslateReservedConnection(offsetback ,- vec * (DisVertCarLengthBackBack + DisLaneWidth));
                    else
                        offsetback = TranslateReservedConnection(offsetback ,- vec * (DisVertCarLength + DisLaneWidth / 2));
                    var plbound = PolyFromLines(linesplit, offsetback);
                    plbound = plbound.Scale(ScareFactorForCollisionCheck);
                    if (!allow_through_build)
                    {
                        if (SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false).Count > 1) continue;
                    }
                    //与障碍物相交
                    linesplit = TranslateReservedConnection(linesplit,vec * DisLaneWidth / 2);
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
                            tmpline = TranslateReservedConnection(tmpline ,- vec * DisLaneWidth / 2);
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
                split = TranslateReservedConnection(split,-vec * DisLaneWidth / 2);
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
                splitback = TranslateReservedConnection(splitback ,- vec * (DisVertCarLengthBackBack + DisLaneWidth));
                var splitori = new LineSegment(splitback);
                splitori = TranslateReservedConnection(splitori ,- vec * (DisVertCarLengthBackBack + DisLaneWidth));

                //temp20220711
                //var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                //if (splits_bd.Count() == 0) continue;
                //splitori = splits_bd.First();
                splitback = TranslateReservedConnection(splitori,vec * (DisVertCarLengthBackBack + DisLaneWidth));
                if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;

                var ploritolane = PolyFromLines(splitback, splitori);
                splitori = TranslateReservedConnection(splitori,vec * DisLaneWidth / 2);
                var splitori_buffer = BufferReservedConnection(splitori,DisLaneWidth / 2);
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
                splitback = TranslateReservedConnection(splitback ,- vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
                var splitbackbuffer = BufferReservedConnection(splitback,DisLaneWidth / 2);
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
                    var lane_ini_replace = TranslateReservedConnection(splitori,vec * ((DisVertCarLengthBackBack + DisLaneWidth / 2) + DisLaneWidth / 2));
                    var lane_ini_replace_pair = TranslateReservedConnection(lane_ini_replace ,- vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
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
                        var lane_ini_pair = TranslateReservedConnection(lane,vec * (DisVertCarLengthBackBack + DisLaneWidth / 2));
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
            //拿邻近wall的方向
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
                            if (wl.P0.Distance(pt) < wl.P1.Distance(pt))
                            {
                                if (angle < 45)
                                {
                                    var ptest = ps.Translation(Vector(wl).Normalize() * (ps.Distance(pt)));
                                    var vec_a = new Vector2D(ps, pt);
                                    var vec_b = new Vector2D(ps, ptest);
                                    if (vec_a.Dot(vec_b) < 0) continue;
                                }
                                found = true;
                                vec = Vector(wl);
                            }
                            else
                            {
                                if (angle < 45)
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
            var pt_closest_onwall = _nearwall_seg.ClosestPoint(ps, true);
            var angle_pspt_pswall = new Vector2D(ps, pt).AngleTo(new Vector2D(ps, pt_closest_onwall));
            var length= (DisCarAndHalfLane + CollisionD - CollisionTOP)/ Math.Cos(angle_pspt_pswall);
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

            var line = LineSegmentSDL(ps, vec, MaxLength);
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
            //与车道模块相交
            var inilinesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
            //解决车道线靠墙的方向有车道线的情况
            var line_to_wall = TranslateReservedConnection(line,-gvec.Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
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

            if (IsInAnyBoxes(inilinesplitcarboxes.MidPoint, carBoxesStrTree) || inilinesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            var inilinesplitcarboxesaction = new LineSegment(inilinesplitcarboxes);
            inilinesplitcarboxesaction=TranslateReservedConnection(inilinesplitcarboxesaction ,- gvec.Normalize() * (DisVertCarLength + DisLaneWidth));
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
            //与障碍物相交
            var iniplsplitbox = inilinesplitcarboxes.Buffer(DisLaneWidth / 2);
            iniplsplitbox = iniplsplitbox.Scale(ScareFactorForCollisionCheck);
            var iniobsplit = SplitLineBySpacialIndexInPoly(inilinesplitcarboxes, iniplsplitbox, ObstaclesSpatialIndex, false)
                .Where(e => e.Length > 1).First();
            if (iniobsplit.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            //if (IsInAnyPolys(iniobsplit.MidPoint, Obstacles))
            //    return generate_lane_length;
            var tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(iniobsplit.MidPoint)).Cast<Polygon>().ToList();
            if (IsInAnyPolys(iniobsplit.MidPoint, tmpobs))
                return generate_lane_length;

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
            offsetline = TranslateReservedConnection(offsetline ,- gvec * DisCarAndHalfLane);
            var pl = PolyFromLines(iniobsplit, offsetline);
            if (IsInAnyBoxes(pl.Envelope.Centroid.Coordinate, carBoxesStrTree)) return generate_lane_length;
            if (isStart) paras.SetGStartAdjLane = index;
            else paras.SetGEndAdjLane = index;
            Lane inilan = new Lane(iniobsplit, gvec);
            paras.LanesToAdd.Add(inilan);
            Lane inilanopposite = new Lane(iniobsplit, -gvec);
            paras.LanesToAdd.Add(inilanopposite);
            paras.CarBoxesToAdd.Add(pl);
            //CarModule module = new CarModule(pl, iniobsplit, -gvec);
            //paras.CarModulesToAdd.Add(module);
            generate_lane_length = iniobsplit.Length;
            if (generate_lane_length - dis_connected_double > 0)
                generate_lane_length -= dis_connected_double;
            return generate_lane_length;
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
