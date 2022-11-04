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
        public void PostProcessPillars()
        {
            try
            {
                CarSpatialIndex = new MNTSSpatialIndex(Cars.Select(e => e.Polyline));
                Pillars = Pillars.Distinct().ToList();
                //删除孤立在车位旁边之外的柱子
                RemoveInvalidPillars();
                //在转角处，两个方向排布的车位柱子碰车位，将该柱子朝车道方向外偏一点————设计师是这么做的
                MovePillarInCorner();
                //删除与车位相交的柱子
                RemovePillarIntersectWithCar();
                //在指定的距离范围内，保留一个柱子（删除近似重复等）
                RemoveDuplicatePillarWithinTheSpecifiedRange();
                //删除在剪力墙附近的柱子
                RemovePillarNearBuildings();
            }
            catch { }
        }
        private void RemoveInvalidPillars()
        {
            List<Polygon> tmps = new List<Polygon>();
            foreach (var t in Pillars)
            {
                var clone = t.Clone();
                clone = clone.Scale(0.5);
                if (ClosestPointInCurveInAllowDistance(clone.Envelope.Centroid.Coordinate, CarSpots, DisPillarLength + DisHalfCarToPillar))
                {
                    tmps.Add(t);
                }
            }
            Pillars = tmps;
        }
        private void MovePillarInCorner()
        {
            var s = new List<Polygon>();
            for (int i = 0; i < Pillars.Count; i++)
            {
                var scfactor = 1.5;
                var pillar = Pillars[i].Clone();
                pillar = pillar.Scale(scfactor);
                var carcrossed = CarSpatialIndex.SelectCrossingGeometry(pillar);
                if (carcrossed.Count() != 2) continue;
                var car_a = Cars[CarSpatialIndex.SelectAll().IndexOf(carcrossed.First())];
                var car_b = Cars[CarSpatialIndex.SelectAll().IndexOf(carcrossed.Last())];
                if (car_a.Vector.IsParallel(car_b.Vector)) continue;
                if (IniLanes.Count == 0) continue;
                var line = IniLanes.Select(e => e.Line).OrderBy(e => e.ClosestPoint(pillar.Centroid.Coordinate).Distance(pillar.Centroid.Coordinate)).First();
                var dist = line.ClosestPoint(pillar.Centroid.Coordinate).Distance(pillar.Centroid.Coordinate);
                var cond_nearlane = Math.Abs(dist - DisPillarMoveDeeplyBackBack) < 10 + DisLaneWidth / 2 || Math.Abs(dist - DisPillarMoveDeeplySingle) < 10 + DisLaneWidth / 2;
                if (cond_nearlane)
                {
                    var car = line.ClosestPoint(car_a.Point).Distance(car_a.Point) < line.ClosestPoint(car_b.Point).Distance(car_b.Point) ? car_a : car_b;
                    var vec = -car.Vector;
                    var dist_x = Math.Abs(car.Point.X - pillar.Centroid.X);
                    var dist_y = Math.Abs(car.Point.Y - pillar.Centroid.Y);
                    var offset_dist = Math.Min(dist_y, dist_x);
                    if (offset_dist < DisPillarDepth / 2)
                    {
                        Pillars.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        Pillars[i] = Pillars[i].Translation(vec.Normalize() * (offset_dist - DisPillarDepth / 2));
                    }
                }
            }
        }
        private void RemovePillarIntersectWithCar()
        {
            for (int i = 0; i < Pillars.Count; i++)
            {
                var pillar = Pillars[i].Clone().Scale(ScareFactorForCollisionCheck);
                if (CarSpatialIndex.SelectCrossingGeometry(pillar).Count > 0)
                {
                    Pillars.RemoveAt(i--);
                }
            }
        }
        private void RemoveDuplicatePillarWithinTheSpecifiedRange()
        {
            //删掉近似重复的
            double tol = 2000;
            for (int i = 0; i < Pillars.Count; i++)
            {
                var list = Pillars.OrderBy(e => e.Centroid.Distance(Pillars[i].Centroid)).Take(10).ToList();
                for (int j = 1; j < list.Count; j++)
                {
                    if (list[j].Centroid.Distance(Pillars[i].Centroid) < tol)
                    {
                        Pillars.Remove(list[j]);
                    }
                    else break;
                }
            }
            //删掉一个车位一个车位并排：柱+车+柱+车+柱，中间一颗柱子
            tol = PillarSpacing / 2;
            for (int i = 0; i < Pillars.Count; i++)
            {
                var list = Pillars.OrderBy(e => e.Centroid.Distance(Pillars[i].Centroid)).Take(3).ToList();
                if (list[1].Centroid.Distance(Pillars[i].Centroid) < tol && list[2].Centroid.Distance(Pillars[i].Centroid) < tol)
                {
                    var pillar_a = list[1];
                    var pillar_b = list[2];
                    var line_ia = new LineSegment(Pillars[i].Centroid.Coordinate, pillar_a.Centroid.Coordinate);
                    var line_ib = new LineSegment(Pillars[i].Centroid.Coordinate, pillar_b.Centroid.Coordinate);
                    var intersects = CarSpatialIndex.SelectCrossingGeometry(line_ia.ToLineString()).Count > 0;
                    intersects = intersects && CarSpatialIndex.SelectCrossingGeometry(line_ib.ToLineString()).Count > 0;
                    var parallel = Vector(line_ia).IsParallel(Vector(line_ib));
                    if (intersects && parallel)
                    {
                        Pillars.RemoveAt(i--);
                    }
                }
            }
        }
        private void RemovePillarNearBuildings()
        {
            double tol = 3000;
            for (int i = 0; i < Pillars.Count; i++)
            {
                var ob = Obstacles.OrderBy(e => e.ClosestPoint(Pillars[i].Centroid.Coordinate).Distance(Pillars[i].Centroid.Coordinate)).First();
                var dist = ob.ClosestPoint(Pillars[i].Centroid.Coordinate).Distance(Pillars[i].Centroid.Coordinate);
                if (dist < tol)
                {
                    Pillars.RemoveAt(i--);
                }
            }
        }
    }
}
