using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public static partial class MLayoutPostProcessing
    {
        /// <summary>
        /// 全局判断识别，对于分隔线尽端如果能生成车位的情况，重新排布局部区域
        /// </summary>
        /// <param name="cars"></param>
        /// <param name="pillars"></param>
        /// <param name="lanes"></param>
        /// <param name="obspacialindex"></param>
        /// <param name="boundary"></param>
        /// <param name="vm"></param>
        public static void GenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref List<InfoCar> cars, ref List<Polygon> pillars, ref List<LineSegment> lanes
            , List<LineString> Walls, MNTSSpatialIndex obspacialindex, Polygon boundary)
        {
            var carspacialindex = new MNTSSpatialIndex(cars.Select(e => e.Polyline).ToList());
            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                if (boundary.ClosestPoint(lane.P0).Distance(lane.P0) < 10)
                {
                    var point = lane.P0;
                    var pointest = point.Translation(Vector(lane).Normalize() * 2000);
                    var vec = Vector(lane).Normalize().GetPerpendicularVector();
                    var carLine = new LineSegment();
                    if (CanAddCarSpots(lane, Vector(lane).Normalize(), pointest, vec, carspacialindex, ref carLine))
                    {
                        generate_cars(Walls, lanes, lane, carLine, vec, boundary, obspacialindex, carspacialindex, ref cars, ref pillars);
                        var pe = lane.ClosestPoint(carLine.P0);
                        var ps = pe.Translation(-Vector(carLine).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                        var l = SplitLine(lane, new List<Coordinate>() { ps }).OrderByDescending(p => p.MidPoint.Distance(carLine.MidPoint)).First();
                        lane = l;
                        lanes[i] = l;
                    }
                }
                if (boundary.ClosestPoint(lane.P1).Distance(lane.P1) < 10)
                {
                    var point = lane.P1;
                    var pointest = point.Translation(-Vector(lane).Normalize() * 2000);
                    var vec = Vector(lane).Normalize().GetPerpendicularVector();
                    var carLine = new LineSegment();
                    if (CanAddCarSpots(lane, -Vector(lane).Normalize(), pointest, vec, carspacialindex, ref carLine))
                    {
                        generate_cars(Walls, lanes, lane, carLine, vec, boundary, obspacialindex, carspacialindex, ref cars, ref pillars, false);
                        var pe = lane.ClosestPoint(carLine.P0);
                        var ps = pe.Translation(-Vector(carLine).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                        var l = SplitLine(lane, new List<Coordinate>() { ps }).OrderByDescending(p => p.MidPoint.Distance(carLine.MidPoint)).First();
                        lane = l;
                        lanes[i] = l;
                    }
                }
            }
        }
        private static bool CanAddCarSpots(LineSegment line, Vector2D linevec, Coordinate pt, Vector2D vec, MNTSSpatialIndex carspacialindex, ref LineSegment carLine)
        {
            var pta = pt.Translation(vec * 6000);
            var linea = new LineSegment(pt, pta);
            var ptb = pt.Translation(-vec * 6000);
            var lineb = new LineSegment(pt, ptb);
            var carsa = new List<Polygon>();
            var carsb = new List<Polygon>();
            for (int i = 0; i < 3; i++)
            {
                double step = 1000;
                linea=linea.Translation(linevec * i * step);
                lineb=lineb.Translation(linevec * i * step);
                var crossedcarsa = carspacialindex.SelectCrossingGeometry(linea.Buffer(10)).Cast<Polygon>()
             .OrderBy(e => e.Centroid.Coordinate.Distance(pt));
                var crossedcarsb = carspacialindex.SelectCrossingGeometry(lineb.Buffer(10)).Cast<Polygon>()
                    .OrderBy(e => e.Centroid.Coordinate.Distance(pt));
                if (crossedcarsa.Count() > 0) carsa.Add(crossedcarsa.First());
                if (crossedcarsb.Count() > 0) carsb.Add(crossedcarsb.First());
            }
            var cara = new Polygon(new LinearRing(new Coordinate[0]));
            var carb = new Polygon(new LinearRing(new Coordinate[0]));
            cara = carsa.Count > 0 ? carsa[0] : cara;
            carb = carsb.Count > 0 ? carsb[0] : carb;
            var hasparallel = false;
            var hasvert = false;
            if (cara.Area > 0)
            {
                var segs = cara.GetEdges();
                var seg = segs.OrderByDescending(e => e.Length).First();
                if (IsParallelLine(seg, line))
                {
                    hasparallel = true;
                    carLine = seg;
                }
                else if (IsPerpLine(seg, line)) hasvert = true;
            }
            if (carb.Area > 0)
            {
                var segs = carb.GetEdges();
                var seg = segs.OrderByDescending(e => e.Length).First();
                if (IsParallelLine(seg, line))
                {
                    hasparallel = true;
                    carLine = seg;
                }
                else if (IsPerpLine(seg, line)) hasvert = true;
            }
            if (hasparallel && !hasvert)
            {
                return true;
            }
            return false;
        }

        private static void generate_cars(List<LineString> Walls, List<LineSegment> lanes, LineSegment lane, LineSegment carLine, Vector2D vec, Polygon boundary, MNTSSpatialIndex obspacialindex
            , MNTSSpatialIndex carspacialindex, ref List<InfoCar> cars, ref List<Polygon> pillars, bool isstartpoint = true)
        {
            var partitionpro = new MParkingPartitionPro();
            partitionpro.Walls = Walls;
            var ps = lane.ClosestPoint(carLine.P0, false);
            var pe = lane.ClosestPoint(carLine.P1, false);
            ps = ps.Translation(new Vector2D(pe, ps).Normalize() * 10);
            pe = pe.Translation(new Vector2D(ps, pe).Normalize() * 10);
            var point = isstartpoint ? lane.P0 : lane.P1;
            if (ps.Distance(point) < pe.Distance(point))
            {
                var p = pe;
                pe = ps;
                ps = p;
            }
            var ls = LineSegmentSDL(ps, vec.Normalize(), 999999);
            ls = ls.Translation(-vec.Normalize() * ls.Length / 2);
            ls = SplitLine(ls, boundary).OrderBy(e => e.MidPoint.Distance(ps)).First();
            var buffer = ls.Buffer(10);
            var obcrossed = obspacialindex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
            var splits = SplitLine(ls, obcrossed)
                .Where(e => !IsInAnyPolys(e.MidPoint, obcrossed))
                .OrderBy(e => e.MidPoint.Distance(ps));
            if (splits.Count() > 0) ls = splits.First();
            else return;
            var carcrossded = carspacialindex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
            ls = SplitLine(ls, carcrossded, 1, true).OrderBy(e => e.MidPoint.Distance(ps)).First();
            var perplanes = lanes.Where(e => IsPerpLine(ls, e)).Where(e => e.ClosestPoint(ps, false).Distance(ps) > 10).ToList();
            ls = SplitLine(ls, perplanes)
                .OrderBy(e => e.MidPoint.Distance(ps)).First();
            var le = new LineSegment(ls);
            le = le.Translation(new Vector2D(ps, pe));
            var pl = PolyFromLines(ls, le);

            if (ClosestPointInVertLines(ls.P1, ls, lanes.ToArray()) < 10) ls = new LineSegment(ls.P1, ls.P0);
            if (ClosestPointInVertLines(ls.P0, ls, lanes.ToArray()) < 10)
                ls.P0 = ls.P0.Translation(Vector(ls).Normalize() * VMStock.RoadWidth / 2);
            if (ClosestPointInVertLines(ls.P1, ls, lanes.ToArray()) < 10)
                ls.P1 = ls.P1.Translation(-Vector(ls).Normalize() * VMStock.RoadWidth / 2);
            var vecmove = new Vector2D(ps, pe);
            ls = ls.Translation(vecmove.Normalize() * 10);
            ls = ls.Translation(-vecmove.Normalize() * VMStock.RoadWidth / 2);
            partitionpro.Boundary = boundary;
            partitionpro.ObstaclesSpatialIndex = obspacialindex;
            partitionpro.Obstacles = obspacialindex.SelectAll().Cast<Polygon>().ToList();
            //partitionpro.IniLanes.Add(new Lane(ls, vecmove));
            var lsbuffer = new Polygon(new LinearRing(new Coordinate[0]));
            try
            {
                lsbuffer = ls.Buffer(MParkingPartitionPro.DisLaneWidth / 2 - 10);
            }
            catch { return; }
            lsbuffer = lsbuffer.Scale(0.99999);
            if (carspacialindex.SelectCrossingGeometry(lsbuffer).Count > 0) return;
            cars = cars.Where(e => !pl.Contains(e.Polyline.Envelope.Centroid)).ToList();
            pillars = pillars.Where(e => !pl.Contains(e.Envelope.Centroid)).ToList();
            //重排
            var inilanesex = new List<LineSegment>();
            var lanes_copy = lanes.Select(e => e).ToList();
            for (int i = 0; i < lanes_copy.Count; i++)
            {
                if (lanes_copy[i].ClosestPoint(ls.P0).Distance(ls.P0) < 1)
                {
                    inilanesex.Add(lanes_copy[i]);
                    i--;
                    lanes_copy.RemoveAt(i);
                }
                else if (lanes_copy[i].ClosestPoint(ls.P1).Distance(ls.P1) < 1)
                {
                    inilanesex.Add(lanes_copy[i]);
                    i--;
                    lanes_copy.RemoveAt(i);
                }
            }
            var tlanes = JoinCurves(new List<LineString>(), inilanesex).OrderByDescending(e => e.Length);
            if (tlanes.Count()>=2)
            {
                var tlane = new LineSegment(tlanes.First().StartPoint.Coordinate, tlanes.First().EndPoint.Coordinate);
                var tlane_depth = tlane.Translation(vecmove.Normalize() * (MParkingPartitionPro.DisVertCarLength+MParkingPartitionPro.DisLaneWidth/2));
                var tlane_rec = PolyFromLines(tlane, tlane_depth);
                cars = cars.Where(e => !tlane_rec.Contains(e.Polyline.Envelope.Centroid)).ToList();
                pillars = pillars.Where(e => !tlane_rec.Contains(e.Envelope.Centroid)).ToList();
                partitionpro.IniLanes.Add(new Lane(tlane, vecmove));
            }
            else
            {
                partitionpro.IniLanes.Add(new Lane(ls, vecmove));
            }
            var vertlanes = partitionpro.GeneratePerpModuleLanes(VMStock.RoadWidth / 2 + VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth,
                VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength, false, null, true);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10) ls = new LineSegment(ls.P1, ls.P0);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * VMStock.RoadWidth / 2);
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.Normalize(), VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength,
                    VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth
                    , true, false, false, false, true, true, false, false, true, false, false, false, true);
            }
            vertlanes = partitionpro.GeneratePerpModuleLanes(VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth : VMStock.ParallelSpotLength
                + VMStock.RoadWidth / 2,
                VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth,
                false);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10) ls = new LineSegment(ls.P1, ls.P0);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * 2750);
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec,
                    VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth,
                    VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth : VMStock.ParallelSpotLength
                    , true, false, false, false, true, true, false);
            }
            cars.AddRange(partitionpro.Cars);
            pillars.AddRange(partitionpro.Pillars);
        }

        public static void GenerateCarsOntheEndofLanesByFillTheEndDistrict(ref List<InfoCar> cars, ref List<Polygon> pillars, ref List<LineSegment> lanes,
            List<LineString> Walls, MNTSSpatialIndex obspacialindex, Polygon boundary)
        {
            var carspacialindex = new MNTSSpatialIndex(cars.Select(e => e.Polyline));
            var recoglines = new List<LineSegment>();
            for (int i = 0; i < lanes.Count; i++)
            {
                if (ClosestPointInVertLines(lanes[i].P0, lanes[i], lanes) > 1)
                    recoglines.Add(new LineSegment(lanes[i].P1, lanes[i].P0));
                else if (ClosestPointInVertLines(lanes[i].P1, lanes[i], lanes) > 1)
                    recoglines.Add(new LineSegment(lanes[i]));
            }
            foreach (var lane in recoglines)
            {
                var line = LineSegmentSDL(lane.P1, Vector(lane).Normalize().GetPerpendicularVector(), MParkingPartitionPro.DisLaneWidth / 2);
                line.P0 = line.P0.Translation(-Vector(line).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                var line_depth = line.Translation(Vector(lane).Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.CollisionD));
                var rec = PolyFromLines(line, line_depth);
                var rec_sc = rec.Scale(0.9999);
                if (carspacialindex.SelectCrossingGeometry(rec_sc).Count() > 0) continue;
                var inherit_line = new LineSegment(line.MidPoint, line_depth.MidPoint);
                var points = new List<Coordinate>();
                var obs_crossed=obspacialindex.SelectCrossingGeometry(rec_sc).Cast<Polygon>();
                foreach (var obj in obs_crossed)
                {
                    points.AddRange(obj.IntersectPoint(rec));
                    points.AddRange(obj.Coordinates.Where(p => rec.Contains(p)));
                }
                points.AddRange(boundary.IntersectPoint(rec));
                points.AddRange(boundary.Coordinates.Where(p => rec.Contains(p)));
                //var inherit_line_points = points.Select(p => inherit_line.ClosestPoint(p)).ToList();
                //var splits_inherit_lines = SplitLine(inherit_line, inherit_line_points);
                //var ini_inherit_line_length = inherit_line.Length;
                //if (splits_inherit_lines.Count() > 0) inherit_line = splits_inherit_lines.First();
                //if (inherit_line.Length < ini_inherit_line_length) continue;
                var points_line = points.Select(p => line.ClosestPoint(p)).ToList();
                var splits_line = SplitLine(line, points).Where(e => e.Length >= MParkingPartitionPro.DisVertCarWidth);
                foreach (var split in splits_line)
                {
                    var split_depth = split.Translation(Vector(lane).Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.CollisionD));
                    var split_rec = PolyFromLines(split, split_depth);
                    var split_rec_sc = split_rec.Scale(0.999);
                    if (obspacialindex.SelectCrossingGeometry(split_rec_sc).Count() > 0) continue;
                    if (boundary.IntersectPoint(split_rec_sc).Count() > 0) continue;
                    MParkingPartitionPro tmpro=new MParkingPartitionPro();
                    tmpro.IniLanes.Add(new Lane(split, Vector(inherit_line).Normalize()));
                    tmpro.Obstacles = new List<Polygon>();
                    tmpro.ObstaclesSpatialIndex=new MNTSSpatialIndex(tmpro.Obstacles);
                    tmpro.GenerateCarsAndPillarsForEachLane(split, Vector(inherit_line).Normalize(), VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength,
                                   VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth
                                   , true, false, false, false, true, true, false, false, true, false, false, false, true);
                    var tmpcars = tmpro.Cars;
                    cars.AddRange(tmpro.Cars.Where(e => boundary.Contains(e.Polyline.Centroid.Coordinate)));
                }
            }
        }
    }
}
