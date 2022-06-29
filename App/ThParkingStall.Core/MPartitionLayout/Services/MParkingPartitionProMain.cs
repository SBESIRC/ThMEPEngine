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
        /// <summary>
        /// 数据预处理
        /// 1. 如果车道线穿过建筑物了（靠近边界的情况），分割该车道线取第一段
        /// 2. 如果区域内含有坡道，从出入点到边界生成一条车道线
        /// </summary>
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

            ProcessLanes(ref IniLanes, true);
            for (int i = 0; i < Walls.Count; i++)
            {
                for (int j = 0; j < Walls[i].Coordinates.Count() - 1; j++)
                {
                    var line = new LineSegment(Walls[i].Coordinates[j], Walls[i].Coordinates[j + 1]);
                    var anglex = Math.Abs(Vector(line).AngleTo(new Vector2D(1, 0)));
                    var angley = Math.Abs(Vector(line).AngleTo(new Vector2D(0, 1)));
                    anglex = Math.Min(anglex, Math.PI - anglex);
                    angley = Math.Min(angley, Math.PI - angley);
                    if (anglex == 0 || angley == 0) continue;
                    if (Math.Min(angley, anglex) > Math.PI / 18) continue;
                    var egdes = new Polygon(new LinearRing(line.ToLineString().Envelope.Coordinates)).GetEdges().OrderByDescending(e => e.Length).Take(2).ToList();
                    var edge = egdes[0];
                    edge = edge.Scale(ScareFactorForCollisionCheck);
                    if (edge.IntersectPoint(Boundary).Count() > 0 || !Boundary.Contains(edge.P0) || !Boundary.Contains(edge.P1))
                        edge = egdes[1];
                    else edge = egdes[0];
                    if (edge.P0.Distance(Walls[i].Coordinates[j]) < edge.P1.Distance(Walls[i].Coordinates[j]))
                    {
                        Walls[i].Coordinates[j] = edge.P0;
                        Walls[i].Coordinates[j + 1] = edge.P1;
                    }
                    else
                    {
                        Walls[i].Coordinates[j] = edge.P1;
                        Walls[i].Coordinates[j + 1] = edge.P0;
                    }
                }
            }
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i].Line;
                var splits = SplitLine(lane, Walls);
                if (splits.Count() >= 1)
                {
                    IniLanes[i].Line = splits.OrderByDescending(e => e.Length).First();
                }
            }
            try
            {
                Boundary = new Polygon(new LinearRing(JoinCurves(Walls, IniLanes.Select(e => e.Line).ToList()).OrderByDescending(e => e.Length).First().Coordinates));
            }
            catch (Exception ex) { }

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
            double mindistance = DisLaneWidth / 2 + DisVertCarWidth * 4;
            var lanes = GeneratePerpModuleLanes(mindistance, DisBackBackModulus, true, null, true);
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
            List<double>lengths=new List<double>();
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
                line = SplitLine(line, IniLaneBoxes).OrderBy(e => e.MidPoint.Distance(line.MidPoint)).First();
                if (ClosestPointInVertLines(line.P0, line, IniLanes.Select(e => e.Line)) < 10)
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * DisLaneWidth / 2);
                if (ClosestPointInVertLines(line.P1, line, IniLanes.Select(e => e.Line)) < 10)
                    line.P1 = line.P1.Translation(-Vector(line).Normalize() * (DisLaneWidth / 2 + DisPillarLength));
                //if (line.Intersect(Boundary, Intersect.OnBothOperands).Count > 0)
                //{
                //    var lines = SplitLine(line, Boundary).Where(e => e.Length > 1)
                //        .Where(e => Boundary.Contains(e.GetCenter()) || ClosestPointInCurves(line.GetCenter(), OriginalLanes) == 0);
                //    if (lines.Count() > 0) line = lines.First();
                //    else continue;
                //}
                line = line.Translation(lanes[i].Vec.Normalize() * DisLaneWidth / 2);
                var dis_start = ClosestPointInVertLines(line.P0, line, IniLanes.Select(e => e.Line));
                if (dis_start < DisLaneWidth / 2)
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * (DisLaneWidth / 2 - dis_start));
                var dis_end = ClosestPointInVertLines(line.P1, line, IniLanes.Select(e => e.Line));
                if (dis_end < DisLaneWidth / 2 + DisPillarLength)
                    line.P1 = line.P1.Translation(-Vector(line).Normalize() * (DisLaneWidth / 2 + DisPillarLength - dis_end));
                var judge_in_obstacles = false;
                if (lengths[i] != lanes[i].Line.Length) judge_in_obstacles = true;
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, lanes[i].Vec, DisVertCarWidth, DisVertCarLength, ref line_align_backback_rest, false, false, false, false, true, false, true,false, judge_in_obstacles, true, true, generate_middle_pillar, isin_backback, true);
                align_backback_for_align_rest = false;
                if (line_align_backback_rest.Length > 0)
                {
                    lanes.Insert(i + 1, new Lane(line_align_backback_rest, lanes[i].Vec));
                    var mod = new CarModule(PolyFromLines(line_align_backback_rest, line_align_backback_rest.Translation(lanes[i].Vec.Normalize()* DisVertCarLength)), line_align_backback_rest, lanes[i].Vec.Normalize());
                    mod.IsInBackBackModule = CarModules[i].IsInBackBackModule;
                    CarModules.Insert(i + 1, mod);
                    lengths.Insert(i + 1, line_align_backback_rest.Length);
                    align_backback_for_align_rest = true;
                }
            }
        }
        public void ProcessLanes(ref List<Lane> lanes, bool preprocess = false)
        {
            for (int i = 0; i < lanes.Count; i++)
            {
                if (IsConnectedToLaneDouble(lanes[i].Line)) continue;
                if (IsConnectedToLane(lanes[i].Line, false))
                    lanes[i].Line = new LineSegment(lanes[i].Line.P1, lanes[i].Line.P0);
                var endp = lanes[i].Line.P1;
                var laneSdl = LineSegmentSDL(endp, Vector(lanes[i].Line).Normalize(), 10000);
                var laneSdlbuffer = laneSdl.Buffer(DisLaneWidth / 2);
                var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>().ToList();
                var car_crossed= CarSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>().ToList();
                var next_to_obs = false;
                foreach (var cross in obscrossed)
                {
                    if (cross.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1 || cross.Contains(lanes[i].Line.P1))
                    {
                        next_to_obs = true;
                        break;
                    }
                }
                foreach (var cross in car_crossed)
                {
                    if (cross.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1 || cross.Contains(lanes[i].Line.P1))
                    {
                        next_to_obs = true;
                        break;
                    }
                }
                if (next_to_obs) continue;
                var points = new List<Coordinate>();
                foreach (var cross in obscrossed)
                {
                    points.AddRange(cross.Coordinates);
                    points.AddRange(cross.IntersectPoint(laneSdlbuffer));
                }
                foreach (var cross in CarSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>())
                {
                    points.AddRange(cross.Coordinates);
                    points.AddRange(cross.IntersectPoint(laneSdlbuffer));
                }
                points = points.Where(p => laneSdlbuffer.Contains(p) || laneSdlbuffer.ClosestPoint(p).Distance(p)<1).ToList();
                points.AddRange(Boundary.IntersectPoint(laneSdlbuffer));
                points = points.Select(p => laneSdl.ClosestPoint(p)).ToList();
                if (preprocess)
                {
                    points = new List<Coordinate>();
                    points.AddRange(Boundary.IntersectPoint(laneSdl.ToLineString()));
                }
                var splits = SplitLine(laneSdl, points);
                if (splits.Count > 0)
                {
                    var split = splits.First();
                    if (/*split.Length > 10 && */split.Length < 10000)
                    {
                        lanes[i].Line = new LineSegment(lanes[i].Line.P0, split.P1);
                    }
                }
            }
        }
        public void GenerateCarsOnRestLanes()
        {
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLengthBackBack + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            SortLaneByDirection(vertlanes, LayoutMode);
            var align_backback_for_align_rest = false;
            for (int i = 0; i < vertlanes.Count; i++)
            {
                var k = vertlanes[i];
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
                    , ref line_align_backback_rest, true, false, false, false,true, true, true, align_backback_for_align_rest, false, true, false,true, false, true);
                align_backback_for_align_rest = false;
                if (line_align_backback_rest.Length > 0)
                {
                    vertlanes.Insert(i + 1, new Lane(line_align_backback_rest, k.Vec));
                    align_backback_for_align_rest = true;
                }
            }

            //var generated = false;
            //while (true)
            //{
            //    UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            //    var _vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            //    if (_vertlanes.Count == 0) break;
            //    SortLaneByDirection(_vertlanes, LayoutMode);
            //    foreach (var k in _vertlanes)
            //    {
            //        generated = true;
            //        var vl = k.Line;
            //        UnifyLaneDirection(ref vl, IniLanes);
            //        var line = new LineSegment(vl);
            //        line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
            //        var car_count = Cars.Count;
            //        GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
            //            , true, false, false, false, true, true, false, false, true, false, false, false, true);
            //        if (Cars.Count == car_count) generated = false;
            //        break;
            //    }
            //    if (!generated) break;
            //}

            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
            SortLaneByDirection(vertlanes, LayoutMode);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                    ,ref line_align_backback_rest, true, false, false, false, true, true, false);
            }
        }
        public void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
            ReDefinePillarDimensions();
            InsuredForTheCaseOfoncaveBoundary();
            if (!DisplayFinal)
                ClassifyLanesForLayoutFurther();
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
        private double GenerateIntegralModuleLanesOptimizedByRealLength(ref GenerateLaneParas paras, bool allow_through_build = true)
        {
            double generate_lane_length;
            double max_length = -1;
            var isCurDirection = false;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras = new GenerateLaneParas();
                var length = GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref _paras, i, true);
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
                            //if (IsHorizontalLine(IniLanes[i].Line) && !isCurDirection)
                            //{
                            //    max_length = length;
                            //    paras = _paras;
                            //}
                            //else if (!IsHorizontalLine(IniLanes[i].Line) && isCurDirection) { }
                            //else
                            //{
                            //    if (length > max_length)
                            //    {
                            //        max_length = length;
                            //        paras = _paras;
                            //    }
                            //}
                            //if (IsHorizontalLine(IniLanes[i].Line)) isCurDirection = true;
                            //break;
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
        private double CalculateModuleLanes(ref GenerateLaneParas paras, int i, bool allow_through_build = true, bool isBackBackModule = true)
        {
            double generate_lane_length = -1;
            var lane = IniLanes[i].Line;
            var vec = IniLanes[i].Vec;
            if (!IniLanes[i].CanBeMoved) return generate_lane_length;
            if (lane.Length < LengthCanGIntegralModulesConnectSingle) return generate_lane_length;
            var offsetlane = new LineSegment(lane);
            if(isBackBackModule)
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
                    foreach (var wl in Walls)
                    {
                        if (bf.IntersectPoint(wl).Count() > 0)
                        {
                            result = false;
                            break;
                        }
                    }
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
                if(isBackBackModule)
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
                    if(isBackBackModule)
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
                        CalculateBackBackLength(obsplits, lane, vec, carBoxesStrTree, ref generate_lane_length, i, ref paras, ref quitcycle, ref generate);
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
        private bool CalculateSingleVertModule(IEnumerable<LineSegment> obsplits, LineSegment lane,LineSegment linesplit, Vector2D vec
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
                        , ref line_align_backback_rest, true, false, false, false, true, true, false, false,false, true, false, false, false, true);
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
                        ,ref line_align_backback_rest, true, false, false, false, true, true, false);
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
            , ref double generate_lane_length, int i, ref GenerateLaneParas paras, ref bool quitcycle, ref bool generate)
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
                    dis_connected_double = DisVertCarLengthBackBack+DisLaneWidth/2;
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
                    if (HasParallelLaneForwardExisted(split, vec, /*28700 - 15700*/DisLaneWidth / 2 + DisVertCarWidth * 2, /*19000 - 15700*//*0*/3000, ref dis_to_move, ref perpLine))
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
        private double GenerateSingleModuleLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i, bool allow_through_build = true)
        {
            //return -1;
            return CalculateModuleLanes(ref paras,i,allow_through_build,false);
            double generate_lane_length = -1;
            var lane = IniLanes[i].Line;
            var vec = IniLanes[i].Vec;
            if (!IniLanes[i].CanBeMoved) return generate_lane_length;
            if (lane.Length < LengthCanGIntegralModulesConnectSingle) return generate_lane_length;
            var offsetlane = new LineSegment(lane);
            offsetlane = offsetlane.Translation(vec * (DisModulus + DisLaneWidth / 2));
            offsetlane = offsetlane.Scale(20);
            //与边界相交

            var _splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
            var splits = new List<LineSegment>();
            foreach (var s in _splits)
            {
                var k = s.Translation(-vec * DisLaneWidth / 2);
                splits.AddRange(SplitBufferLineByPoly(k, DisLaneWidth / 2, Boundary)
                    .Select(e => e.Translation(vec * DisLaneWidth / 2)));
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
                    foreach (var wl in Walls)
                    {
                        if (bf.IntersectPoint(wl).Count() > 0)
                        {
                            result = false;
                            break;
                        }
                    }
                    return result;
                })
                .Where(e => Boundary.Contains(e.MidPoint))
                .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                .Select(e =>
                {
                    e = e.Translation(-vec * (DisLaneWidth / 2));
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
                            if (ptonlane.Distance(ptone) - DisModulus - DisLaneWidth / 2 > 1) return false;
                            else return true;
                        });
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
                        if (quit_repeat) continue;
                        splitback = splitback.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                        var splitori = new LineSegment(splitback);
                        splitori = splitori.Translation(-vec * (DisVertCarLength + DisLaneWidth));


                        var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                        if (splits_bd.Count() == 0) continue;
                        splitori = splits_bd.First();
                        splitback = splitori.Translation(vec * (DisVertCarLength + DisLaneWidth));
                        if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;

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
                            continue;
                        }
                        double dis_connected_double = 0;
                        if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                        if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false))
                        {
                            dis_connected_double = DisCarAndHalfLane;
                        }
                        if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;
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
                        if (ObstaclesSpatialIndex.SelectCrossingGeometry(plbacksc).Count() > 0) continue;
                        var addlane = splitori.Translation(vec * (DisCarAndHalfLane + DisLaneWidth / 2));
                        var addlane_buffer = addlane.Buffer(DisLaneWidth / 2);
                        addlane_buffer = addlane_buffer.Scale(ScareFactorForCollisionCheck);
                        if (ObstaclesSpatialIndex.SelectCrossingGeometry(addlane_buffer).Count() > 0) continue;

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
                                ,ref line_align_backback_rest, true, false, false, false, true, true, false,false, false, true, false, false, false, true);
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
                                ,ref line_align_backback_rest, true, false, false, false, true, true, false);
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
                        if (generatecars_count > estimated_cars_count && tmpro.Cars.Count >= estimated_this_fullcount / 3 + 2)
                        {
                            mod.IsInBackBackModule = true;
                            paras.CarBoxesToAdd.Add(plback);
                            paras.CarModulesToAdd.Add(mod);
                            Lane ln = new Lane(addlane, vec);
                            paras.LanesToAdd.Add(ln);
                            generate_lane_length += splitori.Length;
                        }
                    }
                }
            }
            return generate_lane_length;
        }
        private double GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i, bool allow_through_build = true)
        {
            return CalculateModuleLanes(ref paras, i, allow_through_build);
            double generate_lane_length = -1;
            var lane = IniLanes[i].Line;
            var vec = IniLanes[i].Vec;
            if (!IniLanes[i].CanBeMoved) return generate_lane_length;
            if (lane.Length < LengthCanGIntegralModulesConnectSingle) return generate_lane_length;
            var offsetlane = new LineSegment(lane);
            offsetlane = offsetlane.Translation(vec * (DisModulus + DisLaneWidth / 2));
            offsetlane = offsetlane.Scale(20);
            //与边界相交

            var _splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
            var splits = new List<LineSegment>();
            foreach (var s in _splits)
            {
                var k = s.Translation(-vec * DisLaneWidth / 2);
                splits.AddRange(SplitBufferLineByPoly(k, DisLaneWidth / 2, Boundary)
                    .Select(e => e.Translation(vec * DisLaneWidth / 2)));
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
                    foreach (var wl in Walls)
                    {
                        if (bf.IntersectPoint(wl).Count() > 0)
                        {
                            result = false;
                            break;
                        }
                    }
                    return result;
                })
                .Where(e => Boundary.Contains(e.MidPoint))
                .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                .Select(e =>
                {
                    e = e.Translation(-vec * (DisLaneWidth / 2));
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
                            if (ptonlane.Distance(ptone) - DisModulus - DisLaneWidth / 2 > 1) return false;
                            else return true;
                        });

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
                        splitback = splitback.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                        var splitori = new LineSegment(splitback);
                        splitori = splitori.Translation(-vec * (DisVertCarLength + DisLaneWidth));


                        var splits_bd = SplitLine(splitori, OutBoundary).Where(e => OutBoundary.Contains(e.MidPoint));
                        if (splits_bd.Count() == 0) continue;
                        splitori = splits_bd.First();
                        splitback = splitori.Translation(vec * (DisVertCarLength + DisLaneWidth));
                        if (splitori.Length < LengthCanGIntegralModulesConnectSingle) continue;

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
                            dis_connected_double = DisCarAndHalfLane;
                        }
                        if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;
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
                        if (/*splitori.Length / 3 > lane.Length*/false)
                        {
                            var lane_ini_replace = splitori.Translation(vec * (DisCarAndHalfLane + DisLaneWidth / 2));
                            var lane_ini_replace_pair = lane_ini_replace.Translation(-vec * DisCarAndHalfLane);
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
                            if (splitori.Length > lane.Length)
                            {
                                var lane_ini_pair = lane.Translation(vec * DisCarAndHalfLane);
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
                            if (HasParallelLaneForwardExisted(split, vec, /*28700 - 15700*/DisLaneWidth / 2 + DisVertCarWidth * 2, /*19000 - 15700*//*0*/3000, ref dis_to_move, ref perpLine))
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
                        }


                    }
                    if (quitcycle) break;
                }
                if (quitcycle) break;
            }
            return generate_lane_length;
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
            var distnearbuilding = IsEssentialToCloseToBuilding(line, gvec);
            STRtree<Polygon> carBoxesStrTree = new STRtree<Polygon>();
            CarBoxes.ForEach(polygon => carBoxesStrTree.Insert(polygon.EnvelopeInternal, polygon));
            if (distnearbuilding != -1)
            {
                //贴近建筑物生成
                line = line.Translation(gvec * distnearbuilding);
                //与车道模块相交
                var linesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
                if (IsInAnyBoxes(linesplitcarboxes.MidPoint, carBoxesStrTree) || linesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                    return generate_lane_length;
                //与障碍物相交
                var plsplitbox = linesplitcarboxes.Buffer(DisLaneWidth / 2);
                plsplitbox = plsplitbox.Scale(ScareFactorForCollisionCheck);
                var obsplit = SplitLineBySpacialIndexInPoly(linesplitcarboxes, plsplitbox, ObstaclesSpatialIndex, false)
                    .Where(e => e.Length > 1).First();
                if (obsplit.Length < LengthCanGAdjLaneConnectSingle)
                    return generate_lane_length;
                //if (IsInAnyPolys(obsplit.MidPoint, Obstacles))
                //    return generate_lane_length;
                var _tmpobs = ObstaclesSpatialIndex.SelectCrossingGeometry(new Point(obsplit.MidPoint)).Cast<Polygon>().ToList();
                if (IsInAnyPolys(obsplit.MidPoint, _tmpobs))
                    return generate_lane_length;

                //解决车道线靠墙的方向有车道线的情况
                var _line_to_wall = line.Translation(-gvec.Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
                var _wall_buffer = _line_to_wall.Buffer(/*DisLaneWidth / 2 - 1*/DisModulus + DisLaneWidth);
                var _wall_crossed_lanes_points = new List<Coordinate>();
                foreach (var lane_to_wall in IniLanes.Where(e => IsParallelLine(e.Line, line)).Select(e => e.Line.Buffer(DisLaneWidth / 2 - 1)))
                {
                    _wall_crossed_lanes_points.AddRange(lane_to_wall.IntersectPoint(_wall_buffer));
                }
                _wall_crossed_lanes_points = _wall_crossed_lanes_points.Select(p => line.ClosestPoint(p)).ToList();
                _wall_crossed_lanes_points = SortAlongCurve(_wall_crossed_lanes_points, line.ToLineString());
                _wall_crossed_lanes_points = RemoveDuplicatePts(_wall_crossed_lanes_points);
                if (_wall_crossed_lanes_points.Count > 0)
                {
                    if (_wall_crossed_lanes_points.Count == 2
                        && Math.Abs(new LineSegment(_wall_crossed_lanes_points.First(), _wall_crossed_lanes_points.Last()).Length - obsplit.Length) < 1)
                        return generate_lane_length;
                    var line_to_wall_split = SplitLine(line, _wall_crossed_lanes_points).First();
                    if (line_to_wall_split.Length < obsplit.Length)
                        obsplit = line_to_wall_split;
                }
                if (obsplit.Length < DisVertCarLength) return generate_lane_length;

                if (isStart) paras.SetGStartAdjLane = index;
                else paras.SetGEndAdjLane = index;
                Lane lan = new Lane(obsplit, gvec);
                paras.LanesToAdd.Add(lan);
                paras.LanesToAdd.Add(new Lane(obsplit, -gvec));
                paras.CarBoxesToAdd.Add(PolyFromLine(obsplit));
                generate_lane_length = obsplit.Length;

                return generate_lane_length;
            }
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

            if (IsInAnyBoxes(inilinesplitcarboxes.MidPoint, carBoxesStrTree) || inilinesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            var inilinesplitcarboxesaction = new LineSegment(inilinesplitcarboxes);
            inilinesplitcarboxesaction.Translation(-gvec.Normalize() * (DisVertCarLength + DisLaneWidth));
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
            if (HasParallelLaneForwardExisted(iniobsplit, gvec, DisModulus, 1, ref dis_to_move, ref perpLine)) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit) && iniobsplit.Length < LengthCanGAdjLaneConnectDouble) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit))
            {
                dis_connected_double = DisCarAndHalfLane;
            }
            var offsetline = new LineSegment(iniobsplit);
            offsetline = offsetline.Translation(-gvec * DisCarAndHalfLane);
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
            double generate_lane_length = -1;
            if (BuildingBoxes.Count <= 1) return generate_lane_length;
            for (int i = 0; i < BuildingBoxes.Count - 1; i++)
            {
                for (int j = i + 1; j < BuildingBoxes.Count; j++)
                {
                    var pcenter_i = BuildingBoxes[i].Centroid.Coordinate;
                    var pcenter_j = BuildingBoxes[j].Centroid.Coordinate;
                    var line_ij = new LineSegment(pcenter_i, pcenter_j);
                    var degree = Math.Abs(line_ij.Angle) / Math.PI * 180;
                    degree = Math.Min(degree, Math.Abs(90 - degree));
                    if (degree > 10) continue;
                    var lines = SplitLine(line_ij, BuildingBoxes).Where(e => !IsInAnyBoxes(e.MidPoint, BuildingBoxes));
                    line_ij = ChangeLineToBeOrthogonal(line_ij);
                    if (line_ij.Length < 1) continue;
                    if (BuildingBoxes.Count > 2)
                    {
                        bool quitcycle = false;
                        for (int k = 0; k < BuildingBoxes.Count; k++)
                        {
                            if (k != i && k != j)
                            {
                                var p = line_ij.MidPoint;
                                var lt = new LineSegment(p.Translation(Vector(line_ij).Normalize().GetPerpendicularVector() * MaxLength),
                                    p.Translation(-Vector(line_ij).Normalize().GetPerpendicularVector() * MaxLength));
                                var bf = lt.Buffer(line_ij.Length / 2);
                                if (bf.IntersectPoint(BuildingBoxes[k]).Count() > 0 || bf.Contains(BuildingBoxes[k].Envelope.Centroid.Coordinate))
                                {
                                    quitcycle = true;
                                    break;
                                }
                            }
                        }
                        if (quitcycle) continue;
                    }
                    if (lines.Count() == 0) continue;
                    var line = lines.First();
                    line = ChangeLineToBeOrthogonal(line);
                    if (line.Length < DisCarAndHalfLane) continue;
                    Coordinate ps = new Coordinate();
                    if (Math.Abs(line.P0.X - line.P1.X) > 1)
                    {
                        if (line.P0.X < line.P1.X) line = new LineSegment(line.P1, line.P0);
                        ps = line.P0.Translation(Vector(line).Normalize() * (DisCarAndHalfLane + CollisionD - CollisionTOP));
                    }
                    else if (Math.Abs(line.P0.Y - line.P1.Y) > 1)
                    {
                        if (line.P0.Y < line.P1.Y) line = new LineSegment(line.P1, line.P0);
                        ps = line.P0.Translation(Vector(line).Normalize() * DisLaneWidth / 2);
                    }
                    var vec = Vector(line).GetPerpendicularVector().Normalize();
                    var gline = new LineSegment(ps.Translation(vec * MaxLength), ps.Translation(-vec * MaxLength));
                    var glines = SplitLine(gline, Boundary).Where(e => Boundary.Contains(e.MidPoint))
                        .Where(e => e.Length > 1)
                        .OrderBy(e => e.ClosestPoint(ps).Distance(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    glines = SplitLine(gline, CarBoxes)
                        .Where(e => e.Length > 1)
                        .Where(e => !IsInAnyBoxes(e.MidPoint, CarBoxes))
                        .OrderBy(e => e.ClosestPoint(ps).Distance(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    if (ClosestPointInCurves(gline.MidPoint, IniLanes.Select(e => e.Line.ToLineString()).ToList()) < 1)
                        continue;
                    if (gline.Length < LengthCanGAdjLaneConnectSingle) continue;
                    if (!IsConnectedToLane(gline)) continue;
                    double dis_connected_double = 0;
                    if (IsConnectedToLaneDouble(gline)) dis_connected_double = DisCarAndHalfLane;
                    bool quit = false;
                    foreach (var box in BuildingBoxes)
                    {
                        if (gline.IntersectPoint(box).Count() > 0)
                        {
                            quit = true;
                            break;
                        }
                    }
                    if (quit) continue;
                    var gline_buffer = gline.Buffer(DisLaneWidth / 2);
                    gline_buffer = gline_buffer.Scale(ScareFactorForCollisionCheck);
                    var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(gline_buffer).Cast<Polygon>().ToList();
                    gline_buffer = gline.Buffer(DisLaneWidth / 2);
                    var points = new List<Coordinate>();
                    foreach (var cross in obscrossed)
                    {
                        points.AddRange(cross.Coordinates);
                        points.AddRange(cross.IntersectPoint(gline_buffer));
                    }
                    points = points.Where(p => gline_buffer.Contains(p)).Select(p => gline.ClosestPoint(p)).Where(p => p.Distance(gline.P0) > 1).ToList();
                    var gline_splits = SplitLine(gline, points);
                    if (gline_splits.Count > 0) gline = gline_splits[0];
                    paras.LanesToAdd.Add(new Lane(gline, Vector(line).Normalize()));
                    paras.LanesToAdd.Add(new Lane(gline, -Vector(line).Normalize()));
                    paras.CarBoxesToAdd.Add(PolyFromLine(gline));
                    generate_lane_length = gline.Length;
                    if (generate_lane_length - dis_connected_double > 0)
                        generate_lane_length -= dis_connected_double;
                }
            }
            if (paras.LanesToAdd.Count> 0)
            {
                switch (LayoutMode)
                {
                    case 0:
                        {
                            break;
                        }
                    case 1:
                        {
                            if (generate_lane_length > 0 && !IsHorizontalLine(paras.LanesToAdd[0].Line))
                            {
                                generate_lane_length *= LayoutScareFactor_betweenBuilds;
                            }
                            break;
                        }
                    case 2:
                        {
                            if (generate_lane_length > 0 && !IsVerticalLine(paras.LanesToAdd[0].Line))
                            {
                                generate_lane_length *= LayoutScareFactor_betweenBuilds;
                            }
                            break;
                        }
                }
            }
            return generate_lane_length;
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
                    if(IsConnectedToLaneDouble(lane.Line))IniLanes.Add(lane);
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
                        lane.Line=modified_lane;
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
    }
}
