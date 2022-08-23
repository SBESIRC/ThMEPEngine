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
        public void GeneratePerpModules()
        {
            double mindistance = DisLaneWidth / 2 + DisVertCarWidth * 4;
            var lanes = GeneratePerpModuleLanes(mindistance, DisBackBackModulus, true, null, true, null, true);
            GeneratePerpModuleBoxes(lanes);
        }

        public void GenerateCarsInModules()
        {
            var lanes = new List<Lane>();
            CarModules.Where(e => !e.IsInVertUnsureModule).ToList().ForEach(e => lanes.Add(new Lane(e.Line, e.Vec)));
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
                line = SplitLine(line, IniLaneBoxes).OrderBy(e => e.MidPoint.Distance(line.MidPoint)).First();

                if (ClosestPointInLines(line.P0, line, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line))) < 10)
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * DisLaneWidth / 2);
                if (ClosestPointInLines(line.P1, line, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, line))) < 10)
                    line.P1 = line.P1.Translation(-Vector(line).Normalize() * (DisLaneWidth / 2 + DisPillarLength));

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

        public List<Lane> GeneratePerpModuleLanes(double mindistance, double minlength, bool judge_cross_carbox = true
, Lane specialLane = null, bool check_adj_collision = false, List<Lane> rlanes = null, bool transform_start_edge_for_perp_module = false)
        {
            var pillarSpatialIndex = new MNTSSpatialIndex(Pillars);
            var lanes = new List<Lane>();
            var Lanes = IniLanes;
            if (rlanes != null)
                Lanes = rlanes;
            if (specialLane != null)
                Lanes = new List<Lane>() { specialLane };
            foreach (var lane in Lanes)
            {
                #region 与边界做一次相交判断处理 车道线位置：预生成位置
                var line = new LineSegment(lane.Line);
                var linetest = new LineSegment(line);
                linetest = TranslateReservedConnection(linetest, lane.Vec.Normalize() * mindistance, false);
                var bdpl = PolyFromLines(line, linetest);
                bdpl = bdpl.Scale(ScareFactorForCollisionCheck);
                var bdpoints = Boundary.Coordinates.Where(p => !(line.ClosestPoint(p).Distance(p) < 1 && line.P0.Distance(p) > 1 && line.P1.Distance(p) > 1)).ToList();

                bdpoints.AddRange(Boundary.IntersectPoint(bdpl));
                bdpl = PolyFromLines(line, linetest);
                bdpoints = bdpoints.Where(p => bdpl.IsPointInFast(p)).Select(p => linetest.ClosestPoint(p)).ToList();
                var bdsplits = SplitLine(linetest, bdpoints).Where(e => Boundary.Contains(e.MidPoint) || Boundary.ClosestPoint(e.MidPoint).Distance(e.MidPoint) < 1).Where(e => e.Length >= minlength);
                #endregion
                foreach (var bsplit in bdsplits)
                {
                    var bdsplit = bsplit;
                    //车道回到原位
                    bdsplit = TranslateReservedConnection(bdsplit, -lane.Vec.Normalize() * mindistance, false);

                    #region 与障碍物做一次相交判断处理，车道线位置：预生成位置
                    var bdsplittest = new LineSegment(bdsplit);
                    bdsplittest = TranslateReservedConnection(bdsplittest, lane.Vec.Normalize() * mindistance, false);
                    var obpl = PolyFromLines(bdsplit, bdsplittest);
                    obpl = obpl.Scale(ScareFactorForCollisionCheck);
                    var obcrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(obpl).Cast<Polygon>().ToList();
                    var obpoints = new List<Coordinate>();
                    foreach (var cross in obcrossed)
                    {
                        obpoints.AddRange(cross.Coordinates);
                        obpoints.AddRange(cross.IntersectPoint(obpl));
                    }
                    var pillarcrossed = pillarSpatialIndex.SelectCrossingGeometry(obpl).Cast<Polygon>().ToList();
                    foreach (var cross in pillarcrossed)
                    {
                        obpoints.AddRange(cross.Coordinates);
                        obpoints.AddRange(cross.IntersectPoint(obpl));
                    }
                    obpl = obpl.Scale(1 / (ScareFactorForCollisionCheck - 0.01));
                    obpoints = obpoints.Where(p => obpl.IsPointInFast(p)).Select(p => bdsplittest.ClosestPoint(p)).ToList();
                    var boxsplits = SplitLine(bdsplittest, obpoints).Where(e =>
                    {
                        var box = PolyFromLines(e.Scale(ScareFactorForCollisionCheck), e.Scale(ScareFactorForCollisionCheck).Translation(-lane.Vec.Normalize() * mindistance));
                        foreach (var cross in obcrossed)
                        {
                            if (cross.IntersectPoint(box).Count() > 0) return false;
                        }
                        return !IsInAnyPolys(e.MidPoint, obcrossed);
                    }).Where(e => e.Length >= minlength);
                    #endregion

                    foreach (var bxsplit in boxsplits)
                    {
                        var boxsplit = bxsplit;
                        boxsplit = TranslateReservedConnection(boxsplit, -lane.Vec.Normalize() * mindistance, false);
                        #region 与车道线模块的相交判断处理
                        var boxsplittest = new LineSegment(boxsplit);
                        boxsplittest = TranslateReservedConnection(boxsplittest, lane.Vec.Normalize() * mindistance, false);
                        var boxpl = PolyFromLines(boxsplit, boxsplittest);
                        boxpl = boxpl.Scale(ScareFactorForCollisionCheck);
                        var boxcrossed = new List<Polygon>();
                        if (judge_cross_carbox)
                            boxcrossed = CarBoxesSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                        else
                        {
                            //修改，有的背靠背模块第二模块没有生成carmoudle只生成车道线，对这种背靠背车位的过滤
                            var carcrossed = CarSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>().ToList();
                            if (mindistance == DisCarAndHalfLaneBackBack && ScareEnabledForBackBackModule)
                            {
                                var iniboxpl = PolyFromLines(boxsplit, boxsplittest);
                                //针对背靠背缩进的情况
                                foreach (var car_cross in carcrossed)
                                {
                                    var g = NetTopologySuite.Operation.OverlayNG.OverlayNGRobust.Overlay(car_cross, iniboxpl, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
                                    if (g is Polygon)
                                    {
                                        var cond_area = Math.Round(g.Area) <= (DisVertCarLength - DisVertCarLengthBackBack) * DisVertCarWidth;
                                        if (!cond_area)
                                        {
                                            boxcrossed.Add(car_cross);
                                        }
                                    }
                                    else boxcrossed.Add(car_cross);
                                }
                            }
                            else
                                boxcrossed = carcrossed;
                        }
                        #endregion

                        #region 与车道线自身的Buffer做一次相交判断处理
                        boxpl = PolyFromLines(boxsplit.Translation(lane.Vec.Normalize() * DisLaneWidth / 2), boxsplittest);
                        boxpl = boxpl.Scale(ScareFactorForCollisionCheck);
                        boxcrossed.AddRange(LaneSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>());
                        var lnbox = LaneBufferSpatialIndex.SelectCrossingGeometry(boxpl).Cast<Polygon>();
                        foreach (var bx in lnbox)
                        {
                            var pts = bx.Coordinates.ToList();
                            var pta = AveragePoint(pts[0], pts[3]);
                            var ptb = AveragePoint(pts[1], pts[2]);
                            var lin = new LineSegment(pta, ptb);
                            boxcrossed.Add(BufferReservedConnection(lin, DisLaneWidth / 2));
                        }
                        boxsplittest = TranslateReservedConnection(boxsplittest, lane.Vec.Normalize() * DisLaneWidth / 2, false);
                        var boxpoints = new List<Coordinate>();
                        foreach (var cross in boxcrossed.Select(e => e.Scale(ScareFactorForCollisionCheck)))
                        {
                            boxpoints.AddRange(cross.Coordinates);
                            boxpoints.AddRange(cross.IntersectPoint(boxpl));
                        }
                        boxpl = boxpl.Scale(1 / (ScareFactorForCollisionCheck - 0.01));
                        boxpoints = boxpoints.Where(p => boxpl.IsPointInFast(p)).Select(p => boxsplittest.ClosestPoint(p)).ToList();
                        var splits = SplitLine(boxsplittest, boxpoints).Where(e => e.Length >= minlength)
                            .Where(e =>
                            {
                                var k = new LineSegment(e);
                                k = TranslateReservedConnection(k, -lane.Vec.Normalize() * DisLaneWidth / 2/*(minlength < DisLaneWidth / 2? minlength : DisLaneWidth / 2)*/, false);
                                var k_pl = PolyFromLines(k, TranslateReservedConnection(k, -lane.Vec.Normalize() * (mindistance - DisLaneWidth / 2)));
                                k_pl = k_pl.Scale(ScareFactorForCollisionCheck);
                                foreach (var lbox in lnbox)
                                {
                                    if (lbox.IntersectPoint(k_pl).Count() > 0) return false;
                                }
                                return true;
                            });
                        if (judge_cross_carbox)
                        {
                            splits = splits.Where(e => !IsInAnyBoxes(e.MidPoint, CarBoxes, true));
                        }
                        #endregion

                        foreach (var slit in splits)
                        {
                            var split = slit;
                            split = TranslateReservedConnection(split, -lane.Vec.Normalize() * DisLaneWidth / 2, false);
                            split = TranslateReservedConnection(split, -lane.Vec.Normalize() * mindistance, false);
                            //回到原位
                            //如果与车道相连接，剔除头尾不能排车位的部分
                            if (transform_start_edge_for_perp_module && split.Length > DisLaneWidth / 2 + DisVertCarLength)
                            {
                                if (ClosestPointInLines(split.P0, split, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, split))) < 10)
                                    split.P0 = split.P0.Translation(Vector(split).Normalize() * (DisLaneWidth / 2 + DisVertCarLength));
                            }
                            if (ClosestPointInLines(split.P0, split, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, split))) < 10)
                                split.P0 = split.P0.Translation(Vector(split).Normalize() * DisLaneWidth / 2);
                            if (ClosestPointInLines(split.P1, split, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, split))) < 10)
                                split.P1 = split.P1.Translation(-Vector(split).Normalize() * DisLaneWidth / 2);
                            //
                            var splitnw = new LineSegment(split);
                            splitnw = TranslateReservedConnection(splitnw, lane.Vec.Normalize() * DisLaneWidth / 2, false);
                            if (check_adj_collision)
                            {
                                #region 车位碰撞检查和车道线的缩短变换
                                var pls = ConvertSpecialCollisionCheckRegionForLane(splitnw, lane.Vec.Normalize());
                                var plsc = pls.Clone();
                                plsc = plsc.Scale(ScareFactorForCollisionCheck);
                                var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(plsc).Cast<Polygon>().ToList();
                                var crossedstring = new List<LineString>();
                                crossed.ForEach(e => crossedstring.Add(new LineString(e.Coordinates)));
                                Obstacles.ForEach(o =>
                                {
                                    if (o.Contains(plsc.Envelope.Centroid)) crossedstring.Add(new LineString(o.Coordinates));
                                });
                                Walls?.ForEach(wall =>
                                {
                                        if (plsc.IntersectPoint(wall).Count() > 0)
                                            crossedstring.Add(wall);
                                });
                                var points = new List<Coordinate>();
                                foreach (var cross in crossedstring)
                                {
                                    points.AddRange(cross.Coordinates.Where(pt => pls.Contains(pt)));
                                    points.AddRange(cross.IntersectPoint(pls));
                                }
                                var diss = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P0));
                                if (diss.Count() > 0)
                                {
                                    var collisionD = CollisionD;
                                    CollisionD = 300;
                                    var dis = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P0)).First().Distance(splitnw.P0);
                                    var disc = CollisionD - dis >= 0 ? CollisionD - dis : 0;
                                    splitnw = new LineSegment(splitnw.P0.Translation(Vector(splitnw).Normalize() * disc), splitnw.P1);
                                    CollisionD = collisionD;
                                }

                                pls = ConvertSpecialCollisionCheckRegionForLane(splitnw, lane.Vec.Normalize(), false);
                                plsc = pls.Clone();
                                plsc = plsc.Scale(ScareFactorForCollisionCheck);
                                crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(plsc).Cast<Polygon>().ToList();
                                crossedstring = new List<LineString>();
                                crossed.ForEach(e => crossedstring.Add(new LineString(e.Coordinates)));
                                Obstacles.ForEach(o =>
                                {
                                    if (o.Contains(plsc.Envelope.Centroid)) crossedstring.Add(new LineString(o.Coordinates));
                                });
                                Walls?.ForEach(wall =>
                                {
                                    if (plsc.IntersectPoint(wall).Count() > 0)
                                        crossedstring.Add(wall);
                                });
                                points = new List<Coordinate>();
                                foreach (var cross in crossedstring)
                                {
                                    points.AddRange(cross.Coordinates.Where(pt => pls.Contains(pt)));
                                    points.AddRange(cross.IntersectPoint(pls));
                                }
                                diss = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P1));
                                if (diss.Count() > 0)
                                {
                                    var collisionD = CollisionD;
                                    CollisionD = 300;
                                    var dis = points.Select(pt => splitnw.ClosestPoint(pt, true)).OrderBy(pt => pt.Distance(splitnw.P1)).First().Distance(splitnw.P1);
                                    var disc = CollisionD - dis >= 0 ? CollisionD - dis : 0;
                                    splitnw = new LineSegment(splitnw.P0, splitnw.P1.Translation(-Vector(splitnw).Normalize() * disc));
                                    CollisionD = collisionD;
                                }
                                #endregion
                            }
                            if (splitnw.Length < minlength) continue;
                            splitnw = TranslateReservedConnection(splitnw, -lane.Vec.Normalize() * DisLaneWidth / 2, false);
                            Lane ln = new Lane(splitnw, lane.Vec);
                            lanes.Add(ln);
                        }
                    }
                }
            }
            return lanes;
        }

        private void GeneratePerpModuleBoxes(List<Lane> lanes)
        {
            SortLaneByDirection(lanes, LayoutMode,Vector2D.Zero);
            foreach (var lane in lanes)
            {
                var line = new LineSegment(lane.Line);
                List<LineSegment> ilanes = new List<LineSegment>();
                var segs = new List<LineSegment>();
                var dis = DisVertCarLength - DisVertCarLengthBackBack;
                var point_near_start = line.P0.Translation(Vector(line).Normalize() * DisVertCarLengthBackBack);
                var line_near_start = LineSegmentSDL(point_near_start, lane.Vec, MaxLength);
                line_near_start = SplitLine(line_near_start, Boundary).First();
                var buffer_near_start = PolyFromLines(line_near_start, line_near_start.Translation(-Vector(line).Normalize() * DisVertCarLengthBackBack));
                buffer_near_start = buffer_near_start.Scale(ScareFactorForCollisionCheck);
                var crossedpoints = new List<Coordinate>();
                crossedpoints.AddRange(Boundary.IntersectPoint(buffer_near_start));
                crossedpoints.AddRange(Boundary.Coordinates);
                crossedpoints = crossedpoints.Where(p => buffer_near_start.Contains(p)).OrderBy(p => line_near_start.ClosestPoint(p).Distance(p)).ToList();
                if (crossedpoints.Count > 0)
                {
                    var point_dis = line.ClosestPoint(crossedpoints[0]).Distance(line.P0);
                    dis += point_dis;
                    //当车道贴墙需偏移的时候才偏移
                    line.P0 = line.P0.Translation(Vector(line).Normalize() * dis);
                }
                DivideCurveByLength(line, DisBackBackModulus, ref segs);

                ilanes.AddRange(segs.Where(t => Math.Abs(t.Length - DisBackBackModulus) < 1));
                int modulecount = ilanes.Count;
                int vertcount = ((int)Math.Floor((line.Length - modulecount * DisBackBackModulus) / DisVertCarWidth));
                PerpModlues perpModlue = ConstructPerpModules(lane.Vec, ilanes);
                if (!QuickCalculate)
                {
                    int step = 1;
                    for (int i = 0; i < vertcount; i++)
                    {
                        var test = UpdataPerpModlues(perpModlue, step);
                        if (test.Count >= perpModlue.Count)
                        {
                            perpModlue = test;
                            step = 1;
                        }
                        else
                        {
                            step++;
                            continue;
                        }
                    }
                }              
                foreach (var pl in perpModlue.Bounds)
                {
                    var a = pl.GetEdges()[1];
                    var b = pl.GetEdges()[3];
                    var vec = new Vector2D(a.MidPoint, b.ClosestPoint(a.MidPoint, true));
                    IniLanes.Add(new Lane(a, vec.Normalize()));
                    CarModule module = new CarModule();
                    module.Box = pl;
                    module.Line = a;
                    module.Vec = vec;
                    //注：当模块两端连接车道时，调高排布的优先级；但优先级的设置IsInVertUnsureModule最初不是为了这种case.
                    module.IsInVertUnsureModule = perpModlue.IsInVertUnsureModule;
                    CarModules.Add(module);
                }
                CarBoxes.AddRange(perpModlue.Bounds);
                CarBoxesSpatialIndex.Update(perpModlue.Bounds, new List<Polygon>());
            }
        }

        private PerpModlues UpdataPerpModlues(PerpModlues perpModlues, int step)
        {
            PerpModlues result;
            int minindex = perpModlues.Mminindex;
            List<LineSegment> ilanes = new List<LineSegment>(perpModlues.Lanes);
            if (perpModlues.Lanes.Count == 0) return perpModlues;
            var vec_move = Vector(perpModlues.Lanes[0]).Normalize() * DisVertCarWidth * step;
            for (int i = 0; i < ilanes.Count; i++)
            {
                if (i >= minindex)
                {
                    ilanes[i] = ilanes[i].Translation(vec_move);
                }
            }
            result = ConstructPerpModules(perpModlues.Vec, ilanes);
            return result;
        }

        private PerpModlues ConstructPerpModules(Vector2D vec, List<LineSegment> ilanes)
        {
            PerpModlues result = new PerpModlues();
            int count = 0;
            int minindex = 0;
            int mincount = 9999;
            List<Polygon> plys = new List<Polygon>();
            vec = vec.Normalize() * MaxLength;
            var isInVertUnsureModule = true;
            for (int i = 0; i < ilanes.Count; i++)
            {
                int mintotalcount = 7;
                var unitbase = ilanes[i];
                int generatedcount = 0;
                var tmpplycount = plys.Count;
                var curcount = GenerateUsefulModules(unitbase, vec, plys, ref generatedcount, ref isInVertUnsureModule);
                if (plys.Count > tmpplycount)
                {
                    var pl = plys[plys.Count - 1];
                    var lane = pl.GetEdges()[3];

                    if (ClosestPointInLines(lane.P0, lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, lane))) <= DisLaneWidth / 2
                        && ClosestPointInLines(lane.P1, lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, lane))) <= DisLaneWidth / 2)
                        mintotalcount = 16;
                }
                if (curcount < mintotalcount)
                {
                    ilanes.RemoveAt(i);
                    i--;
                    for (int j = 0; j < generatedcount; j++)
                    {
                        plys.RemoveAt(plys.Count - 1);
                    }
                    continue;
                }
                if (curcount < mincount)
                {
                    mincount = curcount;
                    minindex = i;
                }
                count += curcount;
            }
            result.Bounds = plys;
            result.Count = count;
            result.Lanes = ilanes;
            result.Mminindex = minindex;
            result.Vec = vec;
            result.IsInVertUnsureModule = isInVertUnsureModule;
            return result;
        }

        private int GenerateUsefulModules(LineSegment lane, Vector2D vec, List<Polygon> plys, ref int generatedcount, ref bool isInVertUnsureModule)
        {
            int count = 0;
            var unittest = new LineSegment(lane);
            unittest = unittest.Translation(vec.Normalize() * MaxLength);
            var pltest = PolyFromPoints(new List<Coordinate>() { lane.P0, lane.P1, unittest.P1, unittest.P0 });
            var pltestsc = pltest.Clone();
            pltestsc = pltestsc.Scale(ScareFactorForCollisionCheck);
            var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(pltestsc).Cast<Polygon>().ToList();
            crossed.AddRange(CarBoxesSpatialIndex.SelectCrossingGeometry(pltestsc).Cast<Polygon>());
            crossed.Add(Boundary);
            List<Coordinate> points = new List<Coordinate>();
            foreach (var o in crossed)
            {
                points.AddRange(o.Coordinates);
                //多线程：在单核模式中为pltest，调试出bug，临时改为pltestsc
                //points.AddRange(o.IntersectPoint(pltest));
                points.AddRange(o.IntersectPoint(pltestsc));
                var splitselves = new List<LineString>();
                try
                {
                    //points.AddRange(SplitCurve(o, pltestsc).Select(e => e.GetPointAtParameter(e.EndParam / 2)));
                    splitselves = SplitCurve(o, pltestsc).Where(f =>
                    {
                        if (f.Coordinates.Count() == 2) return pltestsc.Contains(f.GetMidPoint());
                        else if (f.Coordinates.Count() > 2) return pltestsc.Contains(f.Envelope.Centroid);
                        else return false;
                    }).ToList();
                }
                catch
                {
                }
                foreach (var sp in splitselves)
                {
                    if (sp.Coordinates.Count() == 2) points.Add(sp.GetMidPoint());
                    else if (sp.Coordinates.Count() > 2)
                    {
                        points.AddRange(sp.GetEdges().Select(f => f.MidPoint));
                    }
                }
            }
            points = points.Where(e => pltest.IsPointInFast(e) || pltest.ClosestPoint(e).Distance(e) < 1).Distinct().ToList();
            LineSegment edgea = new LineSegment(lane.P0, unittest.P0);
            LineSegment edgeb = new LineSegment(lane.P1, unittest.P1);
            var pointsa = points.Where(e => edgea.ClosestPoint(e).Distance(e) <
                    DisVertCarLengthBackBack + DisLaneWidth).OrderBy(p => edgea.ClosestPoint(p).Distance(lane.P0)).ToList();
            var pointsb = points.Where(e => edgeb.ClosestPoint(e).Distance(e) <
                      DisVertCarLengthBackBack + DisLaneWidth).OrderBy(p => edgeb.ClosestPoint(p).Distance(lane.P1)).ToList();
            for (int i = 0; i < pointsa.Count - 1; i++)
            {
                if (edgea.ClosestPoint(pointsa[i]).Distance(pointsa[i]) < 1)
                {
                    pointsa.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < pointsb.Count - 1; i++)
            {
                if (edgeb.ClosestPoint(pointsb[i]).Distance(pointsb[i]) < 1)
                {
                    pointsb.RemoveAt(i);
                    i--;
                }
            }
            //测试：返回只要满足车道宽度的车道——不以满足整个模块宽度的为车道线——避免的的情况在线后半部分依然能排布的情况
            var pointsa_lane = pointsa.Where(p =>
            {
                var dis = edgea.ClosestPoint(p).Distance(p);
                return dis < DisVertCarLengthBackBack + DisLaneWidth && dis > DisVertCarLengthBackBack;
            }).Select(p => edgea.ClosestPoint(p)).ToList();
            var pointsb_lane = pointsb.Where(p =>
            {
                var dis = edgeb.ClosestPoint(p).Distance(p);
                return dis < DisVertCarLengthBackBack + DisLaneWidth && dis > DisVertCarLengthBackBack;
            }).Select(p => edgeb.ClosestPoint(p)).ToList();
            Coordinate pta_lane;
            Coordinate ptb_lane;
            pointsa_lane = pointsa_lane.Where(e => e.Distance(lane.P0) > 1).OrderBy(e => e.Distance(lane.P0)).ToList();
            pointsb_lane = pointsb_lane.Where(e => e.Distance(lane.P1) > 1).OrderBy(e => e.Distance(lane.P1)).ToList();
            if (pointsa_lane.ToArray().Length == 0) pta_lane = lane.P0;
            else pta_lane = pointsa_lane.First();
            if (pointsb_lane.ToArray().Length == 0) ptb_lane = lane.P0;
            else ptb_lane = pointsb_lane.First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.ClosestPoint(pta_lane).Distance(pta_lane);
                if (disa < DisLaneWidth / 2)
                {
                    pta_lane = pta_lane.Translation((new Vector2D(pta_lane, lane.P0)).Normalize() * (DisLaneWidth / 2 - disa));
                }
                var disb = la.Line.ClosestPoint(ptb_lane).Distance(ptb_lane);
                if (disb < DisLaneWidth / 2)
                {
                    ptb_lane = ptb_lane.Translation(new Vector2D(ptb_lane, lane.P1).Normalize() * (DisLaneWidth / 2 - disb));
                }
            }
            LineSegment eb_lane = new LineSegment(lane.P1, ptb_lane);
            LineSegment ea_lane = new LineSegment(lane.P0, pta_lane);
            var pa_lane = PolyFromPoints(new List<Coordinate>() { lane.P0, lane.P0.Translation(new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack),
                pta_lane.Translation(new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack), pta_lane });
            if (pa_lane.Area > 0)
            {
                if (ClosestPointInLines(ea_lane.P0, ea_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, ea_lane)).ToList()) < 1 &&
                    Math.Abs(ClosestPointInLines(ea_lane.P1, ea_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, ea_lane)).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
                    ea_lane.Length < DisLaneWidth / 2 + DisVertCarWidth * 4)
                {
                    count = 0;
                }
                else
                {
                    plys.Add(pa_lane);
                    generatedcount++;
                }
            }
            var pb_lane = PolyFromPoints(new List<Coordinate>() { lane.P1, lane.P1.Translation(-new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack),
                 ptb_lane.Translation(-new Vector2D(lane.P0,lane.P1).Normalize()*DisCarAndHalfLaneBackBack),ptb_lane});
            if (pb_lane.Area > 0)
            {
                if (ClosestPointInLines(eb_lane.P0, eb_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, eb_lane)).ToList()) < 1 &&
                    Math.Abs(ClosestPointInLines(eb_lane.P1, eb_lane, IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(e, eb_lane)).ToList()) - DisLaneWidth / 2) < DisVertCarWidth &&
    eb_lane.Length < DisLaneWidth / 2 + DisVertCarWidth * 5)
                {
                    count = 0;
                }
                else
                {
                    plys.Add(pb_lane);
                    generatedcount++;
                }
            }
            //以上为测试代码
            pointsa = pointsa.Select(e => edgea.ClosestPoint(e)).ToList();
            pointsb = pointsb.Select(e => edgeb.ClosestPoint(e)).ToList();
            Coordinate pta;
            Coordinate ptb;
            pointsa = pointsa.Where(e => e.Distance(lane.P0) > 1).OrderBy(e => e.Distance(lane.P0)).ToList();
            pointsb = pointsb.Where(e => e.Distance(lane.P1) > 1).OrderBy(e => e.Distance(lane.P1)).ToList();
            if (pointsa.ToArray().Length == 0) pta = lane.P0;
            else pta = pointsa.First();
            if (pointsb.ToArray().Length == 0) ptb = lane.P0;
            else ptb = pointsb.First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.ClosestPoint(pta).Distance(pta);
                if (disa < DisLaneWidth / 2)
                {
                    pta = pta.Translation((new Vector2D(pta, lane.P0)).Normalize() * (DisLaneWidth / 2 - disa));
                    isInVertUnsureModule = false;
                }
                var disb = la.Line.ClosestPoint(ptb).Distance(ptb);
                if (disb < DisLaneWidth / 2)
                {
                    ptb = ptb.Translation(new Vector2D(ptb, lane.P1).Normalize() * (DisLaneWidth / 2 - disb));
                    isInVertUnsureModule = false;
                }
            }
            LineSegment eb = new LineSegment(lane.P1, ptb);
            LineSegment ea = new LineSegment(lane.P0, pta);
            count += ((int)Math.Floor((ea.Length - DisLaneWidth / 2 - DisPillarLength * 2) / DisVertCarWidth));
            count += ((int)Math.Floor((eb.Length - DisLaneWidth / 2 - DisPillarLength * 2) / DisVertCarWidth));

            return count;
        }

    }
}
