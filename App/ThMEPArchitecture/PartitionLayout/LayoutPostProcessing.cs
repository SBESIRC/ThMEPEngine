﻿using Autodesk.AutoCAD.DatabaseServices;
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
        public static void DealWithCarsOntheEndofLanes(ref List<Polyline> cars,ref List<Polyline> pillars, List<Line> lanes
            , ThCADCoreNTSSpatialIndex obspacialindex,Polyline boundary, ParkingStallArrangementViewModel vm)
        {
            var carspacialindex=new ThCADCoreNTSSpatialIndex(cars.ToCollection());
            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                if (boundary.GetClosestPointTo(lane.StartPoint, false).DistanceTo(lane.StartPoint) < 10)
                {
                    var point = lane.StartPoint;
                    var pointest = point.TransformBy(Matrix3d.Displacement(CreateVector(lane).GetNormal() * 3000));
                    var vec = CreateVector(lane).GetNormal().GetPerpendicularVector();
                    var carLine = new Line();
                    if (CanAddCarSpots(lane, pointest, vec, carspacialindex, ref carLine))
                    {
                        generate_cars(lanes, lane, carLine, vec, boundary, obspacialindex,ref cars,ref pillars,vm);
                    }
                }
                if (boundary.GetClosestPointTo(lane.EndPoint, false).DistanceTo(lane.EndPoint) < 10)
                {
                    var point = lane.EndPoint;
                    var pointest = point.TransformBy(Matrix3d.Displacement(-CreateVector(lane).GetNormal() * 3000));
                    var vec = CreateVector(lane).GetNormal().GetPerpendicularVector();
                    var carLine = new Line();
                    if (CanAddCarSpots(lane, pointest, vec, carspacialindex, ref carLine))
                    {
                        generate_cars(lanes, lane, carLine, vec, boundary, obspacialindex, ref cars, ref pillars,vm, false);
                    }
                }
            }
        }
        private static bool CanAddCarSpots(Line line, Point3d pt, Vector3d vec, ThCADCoreNTSSpatialIndex carspacialindex, ref Line carLine)
        {
            var pta = pt.TransformBy(Matrix3d.Displacement(vec * 6000));
            var linea = new Line(pt, pta);
            var crossedcarsa = carspacialindex.SelectCrossingPolygon(linea.Buffer(10)).Cast<Polyline>()
                .OrderBy(e => e.GetCenter().DistanceTo(pt));
            var ptb = pt.TransformBy(Matrix3d.Displacement(-vec * 6000));
            var lineb = new Line(pt, ptb);
            var crossedcarsb = carspacialindex.SelectCrossingPolygon(lineb.Buffer(10)).Cast<Polyline>()
                .OrderBy(e => e.GetCenter().DistanceTo(pt));
            var cara = new Polyline();
            var carb = new Polyline();
            if (crossedcarsa.Count() > 0) cara = crossedcarsa.First();
            if (crossedcarsb.Count() > 0) carb = crossedcarsb.First();
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

        private static void generate_cars(List<Line> lanes, Line lane, Line carLine, Vector3d vec, Polyline boundary, ThCADCoreNTSSpatialIndex obspacialindex
            ,ref List<Polyline> cars,ref List<Polyline> pillars, ParkingStallArrangementViewModel vm, bool isstartpoint=true)
        {
            var partitionpro = new ParkingPartitionPro();
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
            var obcrossed = obspacialindex.SelectCrossingPolygon(ls.Buffer(10)).Cast<Polyline>().ToList();
            ls = SplitLine(ls, obcrossed).OrderBy(e => e.GetCenter().DistanceTo(ps)).First();
            ls = SplitLine(ls, lanes.Where(e => IsPerpLine(ls, e)).Where(e => e.GetClosestPointTo(ps, false).DistanceTo(ps) > 10).ToList())
                .OrderBy(e => e.GetCenter().DistanceTo(ps)).First();
            var le = CreateLine(ls);
            le.TransformBy(Matrix3d.Displacement(CreateVector(ps, pe)));
            var pl = CreatPolyFromLines(ls, le);
            cars = cars.Where(e => !pl.Contains(e.GetRecCentroid())).ToList();
            pillars = pillars.Where(e => !pl.Contains(e.GetRecCentroid())).ToList();
            if (ClosestPointInVertLines(ls.EndPoint, ls, lanes.ToArray()) < 10) ls.ReverseCurve();
            if (ClosestPointInVertLines(ls.StartPoint, ls, lanes.ToArray()) < 10)
                ls.StartPoint = ls.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(ls).GetNormal() * vm.RoadWidth/2));
            if (ClosestPointInVertLines(ls.EndPoint, ls, lanes.ToArray()) < 10)
                ls.EndPoint = ls.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(ls).GetNormal() * vm.RoadWidth / 2));
            var vecmove = CreateVector(ps, pe);
            ls.TransformBy(Matrix3d.Displacement(vecmove.GetNormal() * 10));
            ls.TransformBy(Matrix3d.Displacement(-vecmove.GetNormal() * vm.RoadWidth / 2));
            partitionpro.Boundary = boundary;
            partitionpro.ObstaclesSpatialIndex = obspacialindex;
            partitionpro.Obstacles=obspacialindex.SelectAll().Cast<Polyline>().ToList();
            partitionpro.IniLanes.Add(new Lane(ls, vecmove));
            var vertlanes = partitionpro.GeneratePerpModuleLanes(vm.RoadWidth / 2+ vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotLength : vm.VerticalSpotWidth,
                vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotWidth : vm.VerticalSpotLength, false);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.EndPoint, vl, lanes.ToArray()) < 10) ls.ReverseCurve();
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * vm.RoadWidth / 2));
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.GetNormal(), vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotWidth : vm.VerticalSpotLength,
                    vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotLength : vm.VerticalSpotWidth
                    , true, false, false, false, true, true, false);
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
            cars.AddRange(partitionpro.CarSpots);
            pillars.AddRange(partitionpro.Pillars);
        }

    }
}
