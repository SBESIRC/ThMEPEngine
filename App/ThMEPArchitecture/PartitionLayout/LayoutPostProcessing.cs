using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public static class LayoutPostProcessing
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
        public static void DealWithCarsOntheEndofLanes(ref List<InfoCar> cars, ref List<Polyline> pillars, List<Line> lanes
            , List<Polyline> Walls, ThCADCoreNTSSpatialIndex obspacialindex, Polyline boundary, ParkingStallArrangementViewModel vm)
        {
            var carspacialindex = new ThCADCoreNTSSpatialIndex(cars.Select(e => e.Polyline).ToCollection());
            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                if (boundary.GetClosestPointTo(lane.StartPoint, false).DistanceTo(lane.StartPoint) < 10)
                {
                    var point = lane.StartPoint;
                    var pointest = point.TransformBy(Matrix3d.Displacement(CreateVector(lane).GetNormal() * 2000));
                    var vec = CreateVector(lane).GetNormal().GetPerpendicularVector();
                    var carLine = new Line();
                    if (CanAddCarSpots(lane, CreateVector(lane).GetNormal(), pointest, vec, carspacialindex, ref carLine))
                    {
                        generate_cars(Walls, lanes, lane, carLine, vec, boundary, obspacialindex, carspacialindex, ref cars, ref pillars, vm);
                    }
                }
                if (boundary.GetClosestPointTo(lane.EndPoint, false).DistanceTo(lane.EndPoint) < 10)
                {
                    var point = lane.EndPoint;
                    var pointest = point.TransformBy(Matrix3d.Displacement(-CreateVector(lane).GetNormal() * 2000));
                    var vec = CreateVector(lane).GetNormal().GetPerpendicularVector();
                    var carLine = new Line();
                    if (CanAddCarSpots(lane, -CreateVector(lane).GetNormal(), pointest, vec, carspacialindex, ref carLine))
                    {
                        generate_cars(Walls, lanes, lane, carLine, vec, boundary, obspacialindex, carspacialindex, ref cars, ref pillars, vm, false);
                    }
                }
            }
        }
        private static bool CanAddCarSpots(Line line, Vector3d linevec, Point3d pt, Vector3d vec, ThCADCoreNTSSpatialIndex carspacialindex, ref Line carLine)
        {
            var pta = pt.TransformBy(Matrix3d.Displacement(vec * 6000));
            var linea = new Line(pt, pta);
            var ptb = pt.TransformBy(Matrix3d.Displacement(-vec * 6000));
            var lineb = new Line(pt, ptb);
            var carsa = new List<Polyline>();
            var carsb = new List<Polyline>();
            for (int i = 0; i < 3; i++)
            {
                double step = 1000;
                linea.TransformBy(Matrix3d.Displacement(linevec * i * step));
                lineb.TransformBy(Matrix3d.Displacement(linevec * i * step));
                var crossedcarsa = carspacialindex.SelectCrossingPolygon(linea.Buffer(10)).Cast<Polyline>()
             .OrderBy(e => e.GetCenter().DistanceTo(pt));
                var crossedcarsb = carspacialindex.SelectCrossingPolygon(lineb.Buffer(10)).Cast<Polyline>()
                    .OrderBy(e => e.GetCenter().DistanceTo(pt));
                if (crossedcarsa.Count() > 0) carsa.Add(crossedcarsa.First());
                if (crossedcarsb.Count() > 0) carsb.Add(crossedcarsb.First());
            }
            var cara = new Polyline();
            var carb = new Polyline();
            cara = carsa.Count > 0 ? carsa[0] : cara;
            carb = carsb.Count > 0 ? carsb[0] : carb;
            var hasparallel = false;
            var hasvert = false;
            if (cara.Area > 0)
            {
                var segs = new DBObjectCollection();
                cara.Explode(segs);
                var seg = segs.Cast<Line>().OrderByDescending(e => e.Length).First();
                if (IsParallelLine(seg, line))
                {
                    hasparallel = true;
                    carLine = seg;
                }
                else if (IsPerpLine(seg, line)) hasvert = true;
            }
            if (carb.Area > 0)
            {
                var segs = new DBObjectCollection();
                carb.Explode(segs);
                var seg = segs.Cast<Line>().OrderByDescending(e => e.Length).First();
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

        private static void generate_cars(List<Polyline> Walls, List<Line> lanes, Line lane, Line carLine, Vector3d vec, Polyline boundary, ThCADCoreNTSSpatialIndex obspacialindex
            , ThCADCoreNTSSpatialIndex carspacialindex, ref List<InfoCar> cars, ref List<Polyline> pillars, ParkingStallArrangementViewModel vm, bool isstartpoint = true)
        {
            var partitionpro = new ParkingPartitionPro();
            partitionpro.Walls = Walls;
            var ps = lane.GetClosestPointTo(carLine.StartPoint, false);
            var pe = lane.GetClosestPointTo(carLine.EndPoint, false);
            ps = ps.TransformBy(Matrix3d.Displacement(CreateVector(pe, ps).GetNormal() * 10));
            pe = pe.TransformBy(Matrix3d.Displacement(CreateVector(ps, pe).GetNormal() * 10));
            var point = isstartpoint ? lane.StartPoint : lane.EndPoint;
            if (ps.DistanceTo(point) < pe.DistanceTo(point))
            {
                var p = pe;
                pe = ps;
                ps = p;
            }
            var ls = CreateLineFromStartPtAndVector(ps, vec.GetNormal(), 999999);
            ls.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * ls.Length / 2));
            ls = SplitLine(ls, boundary).OrderBy(e => e.GetCenter().DistanceTo(ps)).First();
            var buffer = ls.Buffer(10);
            var obcrossed = obspacialindex.SelectCrossingPolygon(buffer).Cast<Polyline>().ToList();
            var splits = SplitLine(ls, obcrossed)
                .Where(e => !IsInAnyPolys(e.GetCenter(), obcrossed))
                .OrderBy(e => e.GetCenter().DistanceTo(ps));
            if (splits.Count() > 0) ls = splits.First();
            else return;
            var carcrossded = carspacialindex.SelectCrossingPolygon(buffer).Cast<Polyline>().ToList();
            ls = SplitLine(ls, carcrossded).OrderBy(e => e.GetCenter().DistanceTo(ps)).First();
            var perplanes = lanes.Where(e => IsPerpLine(ls, e)).Where(e => e.GetClosestPointTo(ps, false).DistanceTo(ps) > 10).ToList();
            ls = SplitLine(ls, perplanes)
                .OrderBy(e => e.GetCenter().DistanceTo(ps)).First();
            var le = CreateLine(ls);
            le.TransformBy(Matrix3d.Displacement(CreateVector(ps, pe)));
            var pl = CreatPolyFromLines(ls, le);

            if (ClosestPointInVertLines(ls.EndPoint, ls, lanes.ToArray()) < 10) ls.ReverseCurve();
            if (ClosestPointInVertLines(ls.StartPoint, ls, lanes.ToArray()) < 10)
                ls.StartPoint = ls.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(ls).GetNormal() * vm.RoadWidth / 2));
            if (ClosestPointInVertLines(ls.EndPoint, ls, lanes.ToArray()) < 10)
                ls.EndPoint = ls.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(ls).GetNormal() * vm.RoadWidth / 2));
            var vecmove = CreateVector(ps, pe);
            ls.TransformBy(Matrix3d.Displacement(vecmove.GetNormal() * 10));
            ls.TransformBy(Matrix3d.Displacement(-vecmove.GetNormal() * vm.RoadWidth / 2));
            partitionpro.Boundary = boundary;
            partitionpro.ObstaclesSpatialIndex = obspacialindex;
            partitionpro.Obstacles = obspacialindex.SelectAll().Cast<Polyline>().ToList();
            partitionpro.IniLanes.Add(new Lane(ls, vecmove));
            Polyline lsbuffer = new Polyline();
            try
            {
                lsbuffer = ls.Buffer(ParkingPartitionPro.DisLaneWidth / 2 - 10);
            }
            catch { return; }
            if (carspacialindex.SelectCrossingPolygon(lsbuffer).Count > 0) return;
            cars = cars.Where(e => !pl.Contains(e.Polyline.GetRecCentroid())).ToList();
            pillars = pillars.Where(e => !pl.Contains(e.GetRecCentroid())).ToList();
            var vertlanes = partitionpro.GeneratePerpModuleLanes(vm.RoadWidth / 2 + vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotLength : vm.VerticalSpotWidth,
                vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotWidth : vm.VerticalSpotLength, false, null, true);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.EndPoint, vl, lanes.ToArray()) < 10) ls.ReverseCurve();
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * vm.RoadWidth / 2));
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.GetNormal(), vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotWidth : vm.VerticalSpotLength,
                    vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotLength : vm.VerticalSpotWidth
                    , true, false, false, false, true, true, false, false, true, false, false, false, true);
            }
            vertlanes = partitionpro.GeneratePerpModuleLanes(vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotWidth : vm.ParallelSpotLength
                + vm.RoadWidth / 2,
                vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotLength : vm.ParallelSpotWidth,
                false);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.EndPoint, vl, lanes.ToArray()) < 10) ls.ReverseCurve();
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * 2750));
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec,
                    vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotLength : vm.ParallelSpotWidth,
                    vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotWidth : vm.ParallelSpotLength
                    , true, false, false, false, true, true, false);
            }
            cars.AddRange(partitionpro.Cars);
            pillars.AddRange(partitionpro.Pillars);
        }
    }
}
