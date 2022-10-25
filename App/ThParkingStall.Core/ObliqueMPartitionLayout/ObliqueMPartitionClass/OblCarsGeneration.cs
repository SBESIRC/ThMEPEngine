using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.LaneDeformation;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public partial class ObliqueMPartition
    {
        public void GenerateCarsAndPillarsForEachLane(LineSegment line, Vector2D vec, double length_divided, double length_offset,
    ref LineSegment line_align_backback_rest,
bool add_to_car_spacialindex = true, bool judge_carmodulebox = true, bool adjust_pillar_edge = false, bool judge_modulebox = false,
bool gfirstpillar = true, bool allow_pillar_in_wall = false, bool align_back_to_back = true, bool align_backback_for_align_rest = false, bool judge_in_obstacles = false, bool glastpillar = true, bool judge_intersect_bound = false,
bool generate_middle_pillar = false, bool isin_backback = false, bool check_adj_collision = false,bool isSpecialTypeCrossShealWall=false)
        {
            int inipillar_count = Pillars.Count;
            bool isBackBackmodule=false;
            #region 允许柱子穿墙及背靠背对齐对车道线的起始位置调整
            //允许柱子穿墙
            if (allow_pillar_in_wall && GeneratePillars && Obstacles.Count > 0)
            {
                double dis_judge_under_building = 5000;
                var lineendpt_square = (new LineSegment(line.P0.Translation(Vector(line).Normalize() * 10), line.P0.Translation(-Vector(line).Normalize() * 10))).Buffer(10);
                lineendpt_square = lineendpt_square.Translation(vec.Normalize() * DisPillarMoveDeeplySingle / 2);
                var pillarSpatialIndex = new MNTSSpatialIndex(Pillars);
                var crossed_pillar = pillarSpatialIndex.SelectCrossingGeometry(lineendpt_square).Count > 0;
                var carcrossed = CarSpatialIndex.SelectCrossingGeometry(lineendpt_square);
                if (crossed_pillar)
                {
                    var dis = ClosestPointInVertCurves(line.P0, line, IniLanes.Select(e => e.Line).ToList());
                    if (dis >= DisLaneWidth + DisPillarLength - 1 && Math.Abs(dis - DisCarAndHalfLane) > 1)
                        line = new LineSegment(line.P0.Translation(-Vector(line).Normalize() * DisPillarLength), line.P1);
                    else if (line.Length < DisVertCarWidth * 4)
                        line = new LineSegment(line.P0.Translation(-Vector(line).Normalize() * DisPillarLength), line.P1);
                }
                else
                {
                    if (carcrossed.Count == 0)
                    {
                        if (ClosestPointInCurvesFast(line.P0, Obstacles.Select(e => new LineString(e.Coordinates)).ToList()) < dis_judge_under_building)
                        {
                            var dis = ClosestPointInVertCurves(line.P0, line, IniLanes.Select(e => e.Line).ToList());
                            if (dis >= DisLaneWidth + DisPillarLength - 1 && Math.Abs(dis - DisCarAndHalfLane) > 1)
                                line = new LineSegment(line.P0.Translation(-Vector(line).Normalize() * DisPillarLength), line.P1);
                            else if (line.Length < DisVertCarWidth * 4)
                                line = new LineSegment(line.P0.Translation(-Vector(line).Normalize() * DisPillarLength), line.P1);
                        }
                    }
                }
            }
            if (line.Length == 0) return;

            //背靠背对齐
            if (Math.Abs(length_divided - DisVertCarWidth) < 1 && align_back_to_back)
            {
                double dis_judge_in_backtoback = 20000;
                var pts = line.P0.Translation(vec.Normalize() * DisVertCarLength * 1.5);
                var pte = pts.Translation(Vector(line).Normalize() * dis_judge_in_backtoback);
                var tl = new LineSegment(pts, pte);
                var tlbf = tl.Buffer(1);
                var crosscars = CarSpatialIndex.SelectCrossingGeometry(tlbf).Cast<Polygon>().OrderBy(t => t.Envelope.Centroid.Coordinate.Distance(pts)).ToList();
                if (crosscars.Count() > 1)
                {
                    if (align_backback_for_align_rest)
                    {
                        var crossed_first_car = crosscars.OrderBy(car => car.ClosestPoint(line.P0).Distance(line.P0)).First();
                        var p = crossed_first_car.Coordinates.OrderBy(t => t.Distance(line.P0)).First();
                        var ponline_ex = line.ClosestPoint(p, true);
                        var dis = ponline_ex.Distance(line.P0) % (DisVertCarWidth * CountPillarDist + DisPillarLength);
                        line = new LineSegment(line.P0.Translation(Vector(line).Normalize() * (dis - DisPillarLength)), line.P1);
                    }
                    else
                    {
                        for (int i = 1; i < crosscars.Count(); i++)
                        {
                            if (Math.Abs(crosscars[i].Envelope.Centroid.Coordinate.Distance(crosscars[i - 1].Envelope.Centroid.Coordinate) - DisVertCarWidth - DisPillarLength) < 10)
                            {
                                var p = crosscars[i].Coordinates.OrderBy(t => t.Distance(line.P0)).First();
                                var ponline_ex = line.ClosestPoint(p, true);
                                if (line.ClosestPoint(ponline_ex, false).Distance(ponline_ex) > 1) continue;
                                var dis = ponline_ex.Distance(line.P0) % (DisVertCarWidth * CountPillarDist + DisPillarLength);
                                line_align_backback_rest = new LineSegment(line.P0, line.P0.Translation(Vector(line).Normalize() * (dis - DisPillarLength)));
                                line_align_backback_rest = line_align_backback_rest.Translation(-vec.Normalize() * DisLaneWidth / 2);
                                if (line_align_backback_rest.Length < length_divided) line_align_backback_rest = new LineSegment();
                                line = new LineSegment(line.P0.Translation(Vector(line).Normalize() * (dis - DisPillarLength)), line.P1);
                                break;
                            }
                        }
                    }
                }
            }
            #endregion
            var segobjs = new List<LineSegment>();
            LineSegment[] segs;
            if (!(isSpecialTypeCrossShealWall))
            {
                if (GeneratePillars)
                {
                    var dividecount = Math.Abs(length_divided - DisVertCarWidth) < 1 ? CountPillarDist : 1;
                    DivideCurveByKindsOfLength(line, ref segobjs, DisPillarLength, 1, DisHalfCarToPillar, 1,
                        length_divided, dividecount, DisHalfCarToPillar, 1);
                }
                else
                {
                    DivideCurveByLength(line, length_divided, ref segobjs);
                }
                segs = segobjs.Where(e => Math.Abs(e.Length - length_divided) < 1).ToArray();
            }
            else
            {
                if (GeneratePillars)
                {
                    var dividecount = 1;
                    DivideCurveByKindsOfLength(line, ref segobjs, DisPillarLength, 1, DisHalfCarToPillar, 1,
                        line.Length-DisPillarLength, dividecount, DisHalfCarToPillar, 1);
                }
                else
                {
                    DivideCurveByLength(line, line.Length, ref segobjs);
                }
                segs = segobjs.Where(e => Math.Abs(e.Length - DisPillarLength) > 1).ToArray();
            }

            Polygon precar = new Polygon(new LinearRing(new Coordinate[0]));
            int segscount = segs.Count();
            int c = 0;
            if (segscount == 0) line_align_backback_rest = new LineSegment();

            //data conversion set
            List<SingleParkingPlace> _SingleParkingPlaces = new List<SingleParkingPlace>();
            List<LDColumn> _LDColumns=new List<LDColumn>();
            var _addedcars =new List<InfoCar>(Cars);
            var _addpillars = new List<Polygon>(Pillars);

            foreach (var seg in segs)
            {
                c++;
                bool found_backback = false;
                var s = new LineSegment(seg);
                s = s.Translation(vec.Normalize() * (length_offset));
                
                #region 与障碍物进行相交判断-包含背靠背缩进的判断即车位调整
                var car = PolyFromPoints(new List<Coordinate>() { seg.P0, seg.P1, s.P1, s.P0 });
                var carsc = car.Clone();
                carsc = carsc.Scale(ScareFactorForCollisionCheck);
                var cond = ObstaclesSpatialIndex.SelectCrossingGeometry(carsc).Count == 0;
                if (judge_carmodulebox)
                {
                    cond = cond && (!IsInAnyPolys(carsc.Envelope.Centroid.Coordinate, CarBoxes))
                        && CarBoxesSpatialIndex.SelectCrossingGeometry(carsc).Count == 0;
                }
                else
                {
                    var crossedcarsc = CarSpatialIndex.SelectCrossingGeometry(carsc).Cast<Polygon>().ToList();
                    if (crossedcarsc.Count == 0) cond = true;
                    else
                    {
                        if (crossedcarsc.Count >= 1 && ScareEnabledForBackBackModule)
                        {
                            foreach (var crossed_back_car in crossedcarsc)
                            {
                                var g = NetTopologySuite.Operation.OverlayNG.OverlayNGRobust.Overlay(car, crossed_back_car, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
                                if (g is Polygon && g.Area > 1)
                                {
                                    var segs_g_shorts = ((Polygon)g).GetEdges().Where(e => IsPerpLine(e, seg));
                                    if (!segs_g_shorts.Any())
                                    {
                                        cond = false;
                                        break;
                                    }
                                    var segs_g_short = segs_g_shorts.First();
                                    if (Math.Round(segs_g_short.Length) > (DisVertCarLength - DisVertCarLengthBackBack) * 2)
                                    {
                                        cond = false;
                                        break;
                                    }
                                    var cond_area = Math.Abs((DisVertCarLength - DisVertCarLengthBackBack) * 2 * DisVertCarWidth - g.Area) < 1
                                        || g.Area < (DisVertCarLength - DisVertCarLengthBackBack) * 2 * DisVertCarWidth;
                                    var infos = Cars.Select(e => e.Polyline).ToList();
                                    var exist_index = infos.IndexOf(crossed_back_car);
                                    if (exist_index == -1)
                                    {
                                        cond = false;
                                        break;
                                    }
                                    if (Cars[exist_index].CarLayoutMode != 1 && cond_area)
                                    {
                                        found_backback = true;
                                        var car_exist_iniedge = crossed_back_car.GetEdges().OrderBy(e => e.Length).Take(2).OrderBy(sg => sg.MidPoint.Distance(Cars[exist_index].Point)).First();
                                        var car_exist_transform = PolyFromLines(car_exist_iniedge, car_exist_iniedge.Translation(Cars[exist_index].Vector.Normalize() * DisVertCarLengthBackBack));
                                        //当面前的车位已经是5100时 当前车位信息不需要更新
                                        if (Math.Abs(car_exist_transform.Area - crossed_back_car.Area) > 1 || Cars[exist_index].CarLayoutMode != 2)
                                        {
                                            Cars[exist_index].Polyline = car_exist_transform;
                                            Cars[exist_index].CarLayoutMode = 2;
                                            var carspots_index = CarSpots.IndexOf(crossed_back_car);
                                            CarSpots[carspots_index] = car_exist_transform;
                                            CarSpatialIndex.Update(new List<Polygon>() { car_exist_transform }, new List<Polygon>() { crossed_back_car });
                                        }
                                        isBackBackmodule = true;
                                        s = new LineSegment(seg);
                                        s = s.Translation(vec.Normalize() * (DisVertCarLengthBackBack));
                                        car = PolyFromPoints(new List<Coordinate>() { seg.P0, seg.P1, s.P1, s.P0 });
                                        carsc = car.Clone();
                                        carsc = carsc.Scale(ScareFactorForCollisionCheck);
                                    }
                                    else if (Cars[exist_index].CarLayoutMode == 1 || !cond_area) cond = false;
                                }
                            }
                        }
                        else cond = false;
                    }
                }
                #endregion

                #region 碰撞检查
                if (check_adj_collision)
                {
                    if (Math.Abs(car.Area - DisVertCarLength * DisVertCarWidth) < 1 || Math.Abs(car.Area - DisVertCarLengthBackBack * DisVertCarWidth) < 1)
                    {
                        var pl_checksc = ConvertVertCarToCollisionCar(seg, vec.Normalize());
                        var buffer_pl = pl_checksc.BufferPL(1);
                        if (buffer_pl is Polygon)
                        {
                            var buffers = ((Polygon)buffer_pl).Holes;
                            if (buffers.Count() > 0)
                            {
                                var buffer = new Polygon(buffers[0]);
                                if (ObstaclesSpatialIndex.SelectCrossingGeometry(buffer).Count > 0)
                                {
                                    cond = false;
                                }
                                Walls?.ForEach(wall =>
                                {
                                    if (wall.IntersectPoint(buffer).Count() > 0) cond = false;
                                });
                            }
                            else cond = false;
                        }
                        else cond = false;
                    }
                }
                if (judge_in_obstacles)
                    if (ObstaclesSpatialIndex.SelectCrossingGeometry(carsc).Count > 0) cond = false;
                #endregion
                if (cond)
                {
                    var thisCarTypeTag = CarTypeTag.Normal;//针对垂直式剪力墙，车门阻挡、结构转换等类型做特殊标记
                    var modiSeg = seg;
                    if (  isSpecialTypeCrossShealWall)
                    {
                        thisCarTypeTag = CarTypeTag.CollidedByStruct;
                        var collidedDoor = false;
                        var collidedStruct = false;
                        //往上偏移600 躲开可以剪掉的墙再做判断
                        double deepWallTol = 600;
                        var deepSeg = seg.Translation(vec.Normalize() * deepWallTol);
                        var deepSegExt = deepSeg.Scale(10);
                        var crossed_deepSegExt_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(deepSegExt.Buffer(1)).Cast<Polygon>().ToList();
                        var deepSegExt_crossedPts = new List<Coordinate>();
                        crossed_deepSegExt_obs.ForEach(e => deepSegExt_crossedPts.AddRange(e.IntersectPoint(deepSegExt.ToLineString())));
                        deepSegExt_crossedPts = SortAlongCurve(deepSegExt_crossedPts, deepSegExt.ToLineString());
                        var deepSegExt_split = SplitLine(deepSegExt, deepSegExt_crossedPts).OrderBy(e => e.ClosestPoint(deepSeg.MidPoint).Distance(deepSeg.MidPoint)).First();
                        var deepStart = deepSegExt_split.P0.Distance(deepSeg.MidPoint) < deepSegExt_split.P1.Distance(deepSeg.MidPoint) ? deepSegExt_split.P0 : deepSegExt_split.P1;
                        var deepEnd = deepStart.Distance(deepSegExt_split.P0) == 0 ? deepSegExt_split.P1 : deepSegExt_split.P0;
                        var deepDist = deepStart.Distance(deepEnd);
                        if (deepDist < DisVertCarWidth)
                            cond = false;
                        else
                        {
                            //分为车门阻挡和非车门阻挡
                            var collision = 300;//?后续做一个确认
                            var movedForCollision = false;
                            if (deepDist >= DisVertCarWidth + collision)
                            {
                                deepStart = deepStart.Translation(Vector2D.Create(deepStart, deepEnd).Normalize() * collision);
                                movedForCollision=true;
                            }
                            else
                            {
                                //thisCarTypeTag = CarTypeTag.CollidedByCarDoor;
                                //做不同标记
                                collidedDoor = true;
                            }
                            deepEnd= deepStart.Translation(Vector2D.Create(deepStart, deepEnd).Normalize() * DisVertCarWidth);
                            deepSeg = new LineSegment(deepStart, deepEnd);
                            var deepSeg_ini = deepSeg.Translation(-vec.Normalize() * deepWallTol);
                            car = PolyFromLines(deepSeg_ini, deepSeg_ini.Translation(vec.Normalize() * DisVertCarLength));
                            modiSeg = deepSeg_ini;
                            var crossed_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(car.Scale(ScareFactorForCollisionCheck)).Cast<Polygon>().ToList();
                            var _crossed_obs = crossed_obs.Where(e => e.ClosestPoint(seg.P0).Distance(seg.P0) > 1000 && e.ClosestPoint(seg.P1).Distance(seg.P1) > 1000).ToList();
                            if (_crossed_obs.Any())
                            {
                                cond = false;
                                //如果是前面挪了车位碰撞检测，得考虑把这个值挪回来再判断一下
                                if (movedForCollision)
                                {
                                    collidedDoor = true;
                                    car = car.Translation(-Vector(deepSeg).Normalize() * collision);
                                    modiSeg = modiSeg.Translation(-Vector(deepSeg).Normalize() * collision);
                                    crossed_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(car.Scale(ScareFactorForCollisionCheck)).Cast<Polygon>().ToList();
                                    _crossed_obs = crossed_obs.Where(e => e.ClosestPoint(seg.P0).Distance(seg.P0) > 1000 && e.ClosestPoint(seg.P1).Distance(seg.P1) > 1000).ToList();
                                    if (!crossed_obs.Any())
                                    {
                                        //thisCarTypeTag = CarTypeTag.CollidedByCarDoor;
                                        cond = true;
                                    }
                                    else if (crossed_obs.Any() &&!_crossed_obs.Any())
                                    {
                                        collidedStruct = true;
                                        //thisCarTypeTag = CarTypeTag.CollidedByCarDoorAndStruct;
                                        cond = true;
                                    }
                                }
                            }
                            else if (crossed_obs.Any())
                            {
                                collidedStruct = true;
                            }
                            if (cond)
                            {
                                if (collidedDoor) thisCarTypeTag = CarTypeTag.CollidedByCarDoor;
                                if (collidedStruct) thisCarTypeTag = CarTypeTag.CollidedByStruct;
                                if (collidedDoor && collidedStruct) thisCarTypeTag = CarTypeTag.CollidedByCarDoorAndStruct;
                            }
                        }
                        var car_crossed = CarSpatialIndex.SelectCrossingGeometry(car.Scale(ScareFactorForCollisionCheck)).ToList();
                        if (car_crossed.Any())
                            cond = false;
                    }
                    if (cond)
                    {
                        if (add_to_car_spacialindex)
                            AddToSpatialIndex(car, ref CarBoxesSpatialIndex);
                        AddToSpatialIndex(car, ref CarSpatialIndex);
                        CarSpots.Add(car);
                        var infocar = new InfoCar(car, modiSeg.MidPoint, vec.Normalize());
                        if (length_offset != DisVertCarLength) infocar.CarLayoutMode = ((int)CarLayoutMode.PARALLEL);
                        if (found_backback) infocar.CarLayoutMode = ((int)CarLayoutMode.VERTBACKBACK);
                        infocar.TypeTag = ((int)thisCarTypeTag);
                        Cars?.Add(infocar);
                        if (Pillars.Count > 0)
                        {
                            if (car.Envelope.Contains(Pillars[Pillars.Count - 1].Envelope.Centroid))
                            {
                                Pillars.RemoveAt(Pillars.Count - 1);
                            }
                        }
                        if (precar.Area == 0)
                        {
                            #region 对生成的为车道线第一个车位时，是否需要生成首柱子的场景处理
                            if (gfirstpillar && GeneratePillars)
                            {
                                var ed = modiSeg;
                                if (adjust_pillar_edge)
                                {
                                    ed = s;
                                    vec = -vec;
                                }
                                var pp = ed.P0.Translation(-Vector(ed).Normalize() * DisPillarLength);
                                var li = new LineSegment(pp, ed.P0);
                                var lf = new LineSegment(li);
                                lf = lf.Translation(vec.Normalize() * DisPillarDepth);
                                var pillar = PolyFromPoints(new List<Coordinate>() { li.P0, li.P1, lf.P1, lf.P0 });
                                pillar = pillar.Translation(-Vector(ed).Normalize() * DisHalfCarToPillar);
                                if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                                {
                                    bool condg = true;
                                    if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 2].IsPointInFast(pillar.Envelope.Centroid.Coordinate))
                                        condg = false;
                                    if (condg)
                                    {
                                        //AddToSpatialIndex(pillar, ref CarSpatialIndex);                           
                                        if (isin_backback)
                                            pillar = pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2));
                                        else
                                            pillar = pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2));
                                        Pillars.Add(pillar);
                                    }
                                }
                                if (adjust_pillar_edge)
                                {
                                    vec = -vec;
                                }
                            }
                            precar = car;
                            #endregion
                        }
                        else
                        {
                            var dist = car.Envelope.Centroid.Coordinate.Distance(precar.Envelope.Centroid.Coordinate);
                            if (Math.Abs(dist - length_divided - DisPillarLength - DisHalfCarToPillar * 2) < 1 && GeneratePillars)
                            {
                                var ed = modiSeg;
                                if (adjust_pillar_edge)
                                {
                                    ed = s;
                                    vec = -vec;
                                }
                                var pp = precar.ClosestPoint(ed.P0);
                                var li = new LineSegment(pp, ed.P0);
                                li.P1 = pp.Translation(Vector(li).Normalize() * DisPillarLength);
                                var lf = new LineSegment(li);
                                lf = lf.Translation(vec.Normalize() * DisPillarDepth);
                                var pillar = PolyFromPoints(new List<Coordinate>() { li.P0, li.P1, lf.P1, lf.P0 });
                                pillar = pillar.Translation(Vector(ed).Normalize() * DisHalfCarToPillar);
                                if (isin_backback)
                                    pillar = pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2));
                                else
                                    pillar = pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2));
                                if (Math.Abs(pillar.Area - DisPillarDepth * DisPillarLength) < 1)
                                {
                                    if (add_to_car_spacialindex)
                                        AddToSpatialIndex(pillar, ref CarBoxesSpatialIndex);
                                    Pillars.Add(pillar);
                                    AddToSpatialIndex(pillar, ref CarSpatialIndex);
                                }
                                if (adjust_pillar_edge)
                                {
                                    vec = -vec;
                                }
                            }
                            else { }
                            precar = car;
                        }
                        #region 对是否需要生成最后一颗柱子的场景处理
                        if (glastpillar && c == segscount && GeneratePillars)
                        {
                            var ed = modiSeg;
                            if (adjust_pillar_edge)
                            {
                                ed = s;
                                vec = -vec;
                            }
                            var pp = ed.P1.Translation(Vector(ed).Normalize() * DisPillarLength);
                            var li = new LineSegment(pp, ed.P1);
                            var lf = new LineSegment(li);
                            lf = lf.Translation(vec.Normalize() * DisPillarDepth);
                            var pillar = PolyFromPoints(new List<Coordinate>() { li.P0, li.P1, lf.P1, lf.P0 });
                            pillar = pillar.Translation(Vector(ed).Normalize() * DisHalfCarToPillar);
                            if (isin_backback)
                                pillar = pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplyBackBack - DisPillarDepth / 2));
                            else
                                pillar = pillar.Translation(Vector(new LineSegment(li.P0, lf.P0)).Normalize() * (DisPillarMoveDeeplySingle - DisPillarDepth / 2));
                            if (Math.Abs(pillar.Area - DisPillarLength * DisPillarDepth) < 1)
                            {
                                bool condg = true;
                                if (CarSpots.Count > 1 && CarSpots[CarSpots.Count - 1].IsPointInFast(pillar.Envelope.Centroid.Coordinate))
                                    condg = false;
                                if (condg)
                                {
                                    Pillars.Add(pillar);
                                }
                            }
                            if (adjust_pillar_edge)
                            {
                                vec = -vec;
                            }
                        }
                        #endregion
                    }
                }
            }

            #region data conversion set
            if (!QuickCalculate && AllowLaneDeformation)
            {
                _addedcars = Cars.Except(_addedcars).ToList();
                _addpillars = Pillars.Except(_addpillars).ToList();
                _SingleParkingPlaces = _addedcars.Select(e =>
                {
                    var sp = new SingleParkingPlace(e.Polyline, e.CarLayoutMode, new PureVector(e.Vector.X, e.Vector.Y), e.Point);
                    return sp;
                }).ToList();
                _LDColumns = _addpillars.Select(e =>
                {
                    var ldp = new LDColumn(e);
                    return ldp;
                }).ToList();
                //
                if (_SingleParkingPlaces.Count > 0)
                {
                    var iniLineToDataConversion = new LineSegment(line);
                    var coords_DataConversion = new List<Coordinate>();
                    if (_SingleParkingPlaces.Count > 1)
                    {
                        coords_DataConversion.AddRange(_SingleParkingPlaces[0].ParkingPlaceObb.Coordinates);
                        coords_DataConversion.AddRange(_SingleParkingPlaces[_SingleParkingPlaces.Count - 1].ParkingPlaceObb.Coordinates);
                    }
                    if (_LDColumns.Count > 1)
                    {
                        coords_DataConversion.AddRange(_LDColumns[0].ColunmnObb.Coordinates);
                        coords_DataConversion.AddRange(_LDColumns[_LDColumns.Count - 1].ColunmnObb.Coordinates);
                    }
                    coords_DataConversion = coords_DataConversion.Select(e => line.ClosestPoint(e)).OrderBy(e => line.P0.Distance(e)).ToList();
                    if (coords_DataConversion.Count >= 2)
                    {
                        iniLineToDataConversion = new LineSegment(coords_DataConversion[0], coords_DataConversion[coords_DataConversion.Count - 1]);
                    }
                    //
                    var sourceLine = iniLineToDataConversion.Translation(-vec.Normalize() * DisLaneWidth / 2);
                    var parkingPlaceBlockObb = PolyFromLines(iniLineToDataConversion, iniLineToDataConversion.Translation(vec.Normalize() * ((isBackBackmodule ? DisVertCarLengthBackBack : length_offset) + DisLaneWidth / 2)));
                    var parkingPlaceBlock = new ParkingPlaceBlock(sourceLine, parkingPlaceBlockObb, new PureVector(vec.Normalize().X, vec.Normalize().Y), _SingleParkingPlaces, _LDColumns);
                    ParkingPlaceBlocks.Add(parkingPlaceBlock);
                }
            } 
            #endregion

            #region 对生成中柱的场景处理
            if (generate_middle_pillar)
            {
                var middle_pillars = new List<Polygon>();
                for (int i = inipillar_count; i < Pillars.Count; i++)
                {
                    var p = Pillars[i].Clone();
                    double dist = DisVertCarLength - DisPillarMoveDeeplyBackBack;
                    if (ScareEnabledForBackBackModule) dist = DisVertCarLengthBackBack - DisPillarMoveDeeplyBackBack;
                    p = p.Translation(vec.Normalize() * dist);
                    var pp = p.Clone();
                    pp = pp.Translation(vec.Normalize() * DisPillarDepth);
                    var pisegs = pp.GetEdges();
                    var piseg = pisegs.Where(e => IsPerpVector(Vector(e), vec)).First();
                    piseg = piseg.Scale((DisHalfCarToPillar * 2 + DisPillarLength) / DisPillarLength + 1);
                    var buffer = piseg.Buffer(1);
                    if (CarSpatialIndex.SelectCrossingGeometry(buffer).Count > 0)
                        middle_pillars.Add(p);
                }
                Pillars.AddRange(middle_pillars);
            }
            #endregion
        }
    }
}
