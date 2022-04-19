using NetTopologySuite.Geometries;
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
            for (int i = 0; i < iniLanes.Count; i++)
            {
                var line = iniLanes[i];
                var pl = line.Buffer(DisLaneWidth / 2 - 1);
                var points = new List<Coordinate>();
                foreach (var obj in Obstacles)
                {
                    points.AddRange(obj.Coordinates);
                    points.AddRange(obj.IntersectPoint(pl));
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
            //if (RampList.Count > 0)
            //{
            //    var ramp = RampList[0];
            //    var pt = ramp.InsertPt;
            //    var pl = ramp.Ramp;
            //    var segobjs = new DBObjectCollection();
            //    pl.Explode(segobjs);
            //    var seg = segobjs.Cast<Line>().OrderByDescending(t => t.Length).First();
            //    var vec = CreateVector(seg).GetNormal();
            //    var ptest = pt.TransformBy(Matrix3d.Displacement(vec));
            //    if (pl.Contains(ptest)) vec = -vec;
            //    var rampline = CreateLineFromStartPtAndVector(pt, vec, MaxLength);
            //    rampline = SplitLine(rampline, IniLanes.Select(e => e.Line).ToList()).OrderBy(t => t.GetClosestPointTo(pt, false).DistanceTo(pt)).First();
            //    var prepvec = vec.GetPerpendicularVector();
            //    IniLanes.Add(new Lane(rampline, prepvec));
            //    IniLanes.Add(new Lane(rampline, -prepvec));
            //    OriginalLanes.Add(rampline);
            //    IniLaneBoxes.Add(rampline.Buffer(DisLaneWidth / 2));
            //    for (int i = 0; i < IniLanes.Count; i++)
            //    {
            //        var line = IniLanes[i].Line;
            //        var nvec = IniLanes[i].Vec;
            //        var splits = SplitLine(line, rampline);
            //        if (splits.Count() > 1)
            //        {
            //            IniLanes.RemoveAt(i);
            //            IniLanes.Add(new Lane(splits[0], nvec));
            //            IniLanes.Add(new Lane(splits[1], nvec));
            //            break;
            //        }
            //    }
            //}
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

                var length_integral_modules = ((int)GenerateIntegralModuleLanesOptimizedByRealLength(ref paras_integral_modules, true));
                var length_adj_lanes = ((int)GenerateAdjacentLanesOptimizedByRealLength(ref paras_adj_lanes));
                var length_between_two_builds = ((int)GenerateLaneBetweenTwoBuilds(ref paras_between_two_builds));
                var max = Math.Max(Math.Max(length_integral_modules, length_adj_lanes), Math.Max(length_adj_lanes, length_between_two_builds));
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
                    else
                    {
                        RealizeGenerateLaneParas(paras_between_two_builds);
                    }
                }
                else
                {
                    break;
                }
            }
        }
        public int Process(ref BlockingCollection<InfoCar> cars,ref BlockingCollection<Polygon> pillars,ref BlockingCollection<LineSegment> lanes,ref BlockingCollection<Polygon> inipillars)
        {
            GenerateParkingSpaces();
            for (int i = 0; i < Cars.Count; i++)
                cars.Add(Cars[i]);
            for (int i = 0; i < Pillars.Count; i++)
                pillars.Add(Pillars[i]);
            for (int i = 0; i < IniPillar.Count; i++)
                inipillars.Add(IniPillar[i]);
            for (int i = 0; i < IniLanes.Count; i++)
                lanes.Add(new LineSegment(IniLanes[i].Line));
            return CarSpots.Count;
        }
        public void GeneratePerpModules()
        {
            double mindistance = DisLaneWidth / 2 + DisVertCarWidth * 4;
            var lanes = GeneratePerpModuleLanes(mindistance, DisModulus, true, null, true);
            GeneratePerpModuleBoxes(lanes);
        }
        public void GenerateCarsInModules()
        {
            var lanes = new List<Lane>();
            CarModules.ForEach(e => lanes.Add(new Lane(e.Line, e.Vec)));
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
                GenerateCarsAndPillarsForEachLane(line, lanes[i].Vec, DisVertCarWidth, DisVertCarLength, false, false, false, false, true, false, true, false, true, true, generate_middle_pillar, isin_backback,true);
            }
        }
        public void GenerateCarsOnRestLanes()
        {
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false,null,true);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
                    , true, false, false, false, true, true, false,false,true,false,false,false,true);
            }
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                    , true, false, false, false, true, true, false);
            }
        }
        public void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
            ReDefinePillarDimensions();
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
                            if (IsHorizontalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsHorizontalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsHorizontalLine(IniLanes[i].Line)) isCurDirection = true;
                            break;
                        }
                    case 2:
                        {
                            if (IsVerticalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsVerticalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsVerticalLine(IniLanes[i].Line)) isCurDirection = true;
                            break;
                        }
                }
            }
            generate_lane_length = max_length;
            return generate_lane_length;
        }
        private double GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i, bool allow_through_build = true)
        {
            double generate_lane_length = -1;
            var lane = IniLanes[i].Line;
            var vec = IniLanes[i].Vec;
            if (!IniLanes[i].CanBeMoved) return generate_lane_length;
            if (lane.Length < LengthCanGIntegralModulesConnectSingle) return generate_lane_length;
            var offsetlane = new LineSegment(lane);
            offsetlane = offsetlane.Translation(vec * (DisModulus + DisLaneWidth / 2));
            offsetlane=offsetlane.Scale(20);
            //与边界相交

            var splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
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
            foreach (var linesplitbound in linesplitbounds)
            {
                //与车道模块相交
                var linesplitboundback = new LineSegment(linesplitbound);
                linesplitboundback = linesplitboundback.Translation((-vec * (DisVertCarLength + DisLaneWidth / 2)));
                var plcarbox = PolyFromLines(linesplitbound, linesplitboundback);
                plcarbox=plcarbox.Scale(ScareFactorForCollisionCheck);
                var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false)
                    .Where(e => !IsInAnyBoxes(e.MidPoint/*.TransformBy(Matrix3d.Displacement(-vec.GetNormal())) * 200*/, CarBoxes, true))
                    //.Where(e =>
                    //{
                    //    return !IsInAnyBoxes(AveragePoint(e.GetCenter(), linesplitboundback.GetClosestPointTo(e.GetCenter(), true)), CarBoxes);
                    //})
                    .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
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
                    plcarboxfix=plcarboxfix.Scale(ScareFactorForCollisionCheck);
                    fixlinesplitcarboxes.AddRange(SplitLineBySpacialIndexInPoly(k, plcarboxfix, spindex, false)
                        .Where(e => !IsInAnyBoxes(e.MidPoint, boxs, true))
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
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
                    plbound=plbound.Scale(ScareFactorForCollisionCheck);
                    if (!allow_through_build)
                    {
                        if (SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false).Count > 1) continue;
                    }
                    //与障碍物相交
                    linesplit = linesplit.Translation(vec * DisLaneWidth / 2);
                    plbound = PolyFromLines(linesplit, offsetback);
                    plbound=plbound.Scale(ScareFactorForCollisionCheck);
                    var obsplits = SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false)
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        .Where(e => !IsInAnyPolys(e.MidPoint, Obstacles))
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
                        splitback = splitback.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                        var splitori = new LineSegment(splitback);
                        splitori = splitori.Translation(-vec * (DisVertCarLength + DisLaneWidth));
                        var ploritolane = PolyFromLines(splitback, splitori);
                        splitori = splitori.Translation(vec * DisLaneWidth / 2);
                        if (((lane.ClosestPoint(splitori.P0).Distance(splitori.P0) >/* 5000*/splitori.Length / 3
                            || lane.ClosestPoint(splitori.P1).Distance(splitori.P1) > splitori.Length / 3)
                            && ObstaclesSpatialIndex.SelectCrossingGeometry(ploritolane).Cast<Polygon>().Where(e => Boundary.Contains(e.Envelope.Centroid) || Boundary.IntersectPoint(e).Count() > 0).Count() > 0)
                            || IsInAnyBoxes(splitori.MidPoint, CarBoxes))
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
                            Lane lan = new Lane(splitori, vec);
                            paras.LanesToAdd.Add(lan);
                            paras.LanesToAdd.Add(new Lane(splitori, -vec));
                            paras.CarBoxesToAdd.Add(PolyFromLine(splitori));
                            quitcycle = true;
                            generate = true;
                            break;
                        }
                        if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                        if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;
                        paras.SetNotBeMoved = i;
                        var pl = PolyFromLines(split, splitback);
                        var plback = pl.Clone();
                        plback = plback.Translation(-vec * DisCarAndHalfLane);
                        var split_splitori_points = plback.IntersectPoint(Boundary).Select(e => splitori.ClosestPoint(e)).ToList();
                        var mod = new CarModule(plback, splitori, vec);
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
                        paras.CarBoxPlusToAdd.Add(new CarBoxPlus(plback));
                        paras.CarBoxesToAdd.Add(plback);
                        generate = true;
                        //generate_lane_length = split.Length;
                        double dis_to_move = 0;
                        LineSegment perpLine = new LineSegment(new Coordinate(0, 0), new Coordinate(0, 0));
                        if (HasParallelLaneForwardExisted(split, vec, 28700 - 15700, /*19000 - 15700*//*0*/3000, ref dis_to_move, ref perpLine))
                        {
                            paras.CarBoxPlusToAdd[paras.CarBoxPlusToAdd.Count - 1].IsSingleForParallelExist = true;
                            var existBoxes = CarBoxesPlus.Where(e => e.IsSingleForParallelExist).Select(e => e.Box);
                            foreach (var box in existBoxes)
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
                            paras.CarBoxesToAdd.Add(pl);
                            CarModule module = new CarModule(pl, split, -vec);
                            module.IsInBackBackModule = true;
                            paras.CarModulesToAdd.Add(module);
                            Lane ln = new Lane(split, vec);
                            paras.LanesToAdd.Add(ln);
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
                            if (IsVerticalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsVerticalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsVerticalLine(IniLanes[i].Line)) isCurDirection = true;
                            break;
                        }
                    case 2:
                        {
                            if (IsHorizontalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsHorizontalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsHorizontalLine(IniLanes[i].Line)) isCurDirection = true;
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
            if (CloseToWall(lane.Line.P0) && !lane.GStartAdjLine)
            {
                var generated = GenerateAdjacentLanesFunc(ref paras, lane, i, true);
                if (generated != -1)
                {
                    return generated;
                }
            }
            else if (CloseToWall(lane.Line.P1) && !lane.GEndAdjLine)
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
                ps = pt.Translation(Vector(lane.Line).Normalize() * (DisCarAndHalfLane+CollisionD));
            }
            else
            {
                pt = lane.Line.P1;
                ps = pt.Translation(-Vector(lane.Line).Normalize() * (DisCarAndHalfLane+ CollisionD));
            }
            var line = LineSegmentSDL(ps, lane.Vec, MaxLength);
            var tmpline = SplitLine(line, Boundary).Where(e => e.Length > 1).First();
            if (Boundary.Contains(tmpline.MidPoint))
                line = tmpline;
            else return generate_lane_length;
            var gvec = Vector(line).GetPerpendicularVector().Normalize();
            var ptestvec = ps.Translation(gvec);
            if (ptestvec.Distance(pt) < (DisCarAndHalfLane + CollisionD - CollisionTOP)) gvec = -gvec;
            var distnearbuilding = IsEssentialToCloseToBuilding(line, gvec);
            if (distnearbuilding != -1)
            {
                //贴近建筑物生成
                line=line.Translation(gvec * distnearbuilding);
                //与车道模块相交
                var linesplitcarboxes = SplitLine(line, CarBoxes).Where(e => e.Length > 1).First();
                if (IsInAnyBoxes(linesplitcarboxes.MidPoint, CarBoxes) || linesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
                    return generate_lane_length;
                //与障碍物相交
                var plsplitbox = linesplitcarboxes.Buffer(DisLaneWidth / 2);
                plsplitbox=plsplitbox.Scale(ScareFactorForCollisionCheck);
                var obsplit = SplitLineBySpacialIndexInPoly(linesplitcarboxes, plsplitbox, ObstaclesSpatialIndex, false)
                    .Where(e => e.Length > 1).First();
                if (IsInAnyPolys(obsplit.MidPoint, Obstacles) || obsplit.Length < LengthCanGAdjLaneConnectSingle)
                    return generate_lane_length;

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
            if (IsInAnyBoxes(inilinesplitcarboxes.MidPoint, CarBoxes) || inilinesplitcarboxes.Length < LengthCanGAdjLaneConnectSingle)
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
            iniplsplitbox=iniplsplitbox.Scale(ScareFactorForCollisionCheck);
            var iniobsplit = SplitLineBySpacialIndexInPoly(inilinesplitcarboxes, iniplsplitbox, ObstaclesSpatialIndex, false)
                .Where(e => e.Length > 1).First();
            if (IsInAnyPolys(iniobsplit.MidPoint, Obstacles) || iniobsplit.Length < LengthCanGAdjLaneConnectSingle)
                return generate_lane_length;
            double dis_to_move = 0;
            var perpLine = new LineSegment();
            if (HasParallelLaneForwardExisted(iniobsplit, gvec, DisModulus, 1, ref dis_to_move, ref perpLine)) return generate_lane_length;
            if (IsConnectedToLaneDouble(iniobsplit) && iniobsplit.Length < LengthCanGAdjLaneConnectDouble) return generate_lane_length;
            var offsetline = new LineSegment(iniobsplit);
            offsetline.Translation(-gvec * DisCarAndHalfLane);
            var pl = PolyFromLines(iniobsplit, offsetline);
            if (IsInAnyBoxes(pl.Envelope.Centroid.Coordinate, CarBoxes)) return generate_lane_length;
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
                    var lines = SplitLine(line_ij, BuildingBoxes).Where(e => !IsInAnyBoxes(e.MidPoint, BuildingBoxes));
                    line_ij = ChangeLineToBeOrthogonal(line_ij);
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
                        ps = line.P0.Translation(Vector(line).Normalize() * DisCarAndHalfLane);
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
                    glines = SplitLine(gline, CarBoxes).Where(e => !IsInAnyBoxes(e.MidPoint, CarBoxes))
                        .Where(e => e.Length > 1)
                        .OrderBy(e => e.ClosestPoint(ps).Distance(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    if (ClosestPointInCurves(gline.MidPoint, IniLanes.Select(e => e.Line.ToLineString()).ToList()) < 1)
                        continue;
                    if (gline.Length < LengthCanGAdjLaneConnectSingle) continue;
                    if (!IsConnectedToLane(gline)) continue;
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
                    paras.LanesToAdd.Add(new Lane(gline, Vector(line).Normalize()));
                    paras.LanesToAdd.Add(new Lane(gline, -Vector(line).Normalize()));
                    paras.CarBoxesToAdd.Add(PolyFromLine(gline));
                    generate_lane_length = gline.Length;
                }
            }
            return generate_lane_length;
        }
        private void RealizeGenerateLaneParas(GenerateLaneParas paras)
        {
            if (paras.SetNotBeMoved != -1) IniLanes[paras.SetNotBeMoved].CanBeMoved = false;
            if (paras.SetGStartAdjLane != -1) IniLanes[paras.SetGStartAdjLane].GStartAdjLine = true;
            if (paras.SetGEndAdjLane != -1) IniLanes[paras.SetGEndAdjLane].GEndAdjLine = true;
            if (paras.LanesToAdd.Count > 0) IniLanes.AddRange(paras.LanesToAdd);
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
