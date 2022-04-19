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
        public static void PostProcessLanes(ref List<LineSegment> lanes, List<Polygon> cars, List<Polygon> inipillars, List<Coordinate> obsvertice)
        {
            //删除一些重复的、相邻很近可删除其中一条的车道线并进行合并处理
            RemoveDuplicatedAndInvalidLanes(ref lanes);
            //用Sindexes,Eindexes表示连接关系
            List<int> Sindexes = new List<int>();
            List<int> Eindexes = new List<int>();
            GetConnectedRelationship(lanes, ref Sindexes, ref Eindexes);
            //重定位车道线，居中处理
            foreach (var car in cars) obsvertice.AddRange(car.Coordinates);
            foreach (var pillar in inipillars) obsvertice.AddRange(pillar.Coordinates);
            RelocateLanes(ref lanes, obsvertice, Sindexes, Eindexes);
            //删除未连接车道线的末尾多余部分
            RemoveInvalidPartsOfEndLanes(ref lanes, cars, Sindexes, Eindexes);
            //交点打断
            lanes = GetLinesByInterruptingIntersections(lanes);
        }
        private static void RemoveDuplicatedAndInvalidLanes(ref List<LineSegment> lanes)
        {
            if (lanes.Count < 2) return;
            //删除部分共线的直线只保留一份
            for (int i = 0; i < lanes.Count - 1; i++)
            {
                for (int j = i + 1; j < lanes.Count; j++)
                {
                    if (IsParallelLine(lanes[i], lanes[j]) && lanes[j].ClosestPoint(lanes[i].MidPoint, true).Distance(lanes[i].MidPoint) < 0.001)
                    {
                        if (lanes[j].ClosestPoint(lanes[i].P0, false).Distance(lanes[i].P0) < 0.001
                            && lanes[j].ClosestPoint(lanes[i].P1, false).Distance(lanes[i].P1) > 0.001)
                        {
                            var p = lanes[j].P0.Distance(lanes[i].P1) <= lanes[j].P1.Distance(lanes[i].P1) ?
                                lanes[j].P0 : lanes[j].P1;
                            lanes[i] = new LineSegment(p, lanes[i].P1);
                        }
                        if (lanes[j].ClosestPoint(lanes[i].P1, false).Distance(lanes[i].P1) < 0.001
                            && lanes[j].ClosestPoint(lanes[i].P0, false).Distance(lanes[i].P0) > 0.001)
                        {
                            var p = lanes[j].P0.Distance(lanes[i].P0) <= lanes[j].P1.Distance(lanes[i].P0) ?
                                lanes[j].P0 : lanes[j].P1;
                            lanes[i] = new LineSegment(lanes[i].P0, p);
                        }
                    }
                }
            }
            //删除重复的子车道线
            lanes = lanes.OrderBy(e => e.Length).ToList();
            double tol = 2750;//近似重复
            for (int i = 0; i < lanes.Count - 1; i++)
            {
                for (int j = i + 1; j < lanes.Count; j++)
                {
                    if (IsSubLine(lanes[i], lanes[j]))
                    {
                        lanes.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            //合并车道线
            JoinLines(lanes);
            //删除近似重复的子车道线
            lanes = lanes.OrderBy(e => e.Length).ToList();
            for (int i = 0; i < lanes.Count - 1; i++)
            {
                for (int j = i + 1; j < lanes.Count; j++)
                {
                    bool isSimilarDuplicated = IsParallelLine(lanes[i], lanes[j]) && lanes[j].ClosestPoint(lanes[i].MidPoint, false).Distance(lanes[i].MidPoint) < tol
                        && Math.Abs(lanes[j].ClosestPoint(lanes[i].P0, false).Distance(lanes[i].P0) - lanes[j].ClosestPoint(lanes[i].P1, false).Distance(lanes[i].P1)) < 1;
                    if (isSimilarDuplicated)
                    {
                        lanes.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }
        private static void GetConnectedRelationship(List<LineSegment> lanes, ref List<int> Sindexes, ref List<int> Eindexes)
        {
            foreach (var e in lanes)
            {
                Sindexes.Add(-1);
                Eindexes.Add(-1);
            }
            for (int i = 0; i < lanes.Count; i++)
            {
                for (int j = 0; j < lanes.Count; j++)
                {
                    if (i != j)
                    {
                        if (lanes[j].ClosestPoint(lanes[i].P0, false).Distance(lanes[i].P0) < 0.001)
                        {
                            Sindexes[i] = j;
                            break;
                        }
                    }
                }
                for (int j = 0; j < lanes.Count; j++)
                {
                    if (i != j)
                    {
                        if (lanes[j].ClosestPoint(lanes[i].P1, false).Distance(lanes[i].P1) < 0.001)
                        {
                            Eindexes[i] = j;
                            break;
                        }
                    }
                }
            }
        }
        private static void RelocateLanes(ref List<LineSegment> lanes, List<Coordinate> obsvertice, List<int> Sindexes, List<int> Eindexes)
        {
            //重定位
            double dis = 5000;
            List<int> processedIndex = new List<int>();
            for (int i = 0; i < lanes.Count; i++)
            {
                var vec_a = Vector(lanes[i]).GetPerpendicularVector().Normalize();
                var vec_b = -vec_a;
                var lane = lanes[i];
                var l = new LineSegment(lanes[i]);
                l=l.Scale( (l.Length - 2) / l.Length);
                var lane_a = new LineSegment(l);
                var lane_b = new LineSegment(l);
                lane_a=lane_a.Translation(vec_a * dis);
                lane_b=lane_b.Translation(vec_b * dis);
                var pla = PolyFromLines(lanes[i], lane_a);
                var plb = PolyFromLines(lanes[i], lane_b);
                var ptsas = obsvertice.Where(p => pla.Contains(p)).OrderBy(p => lane.ClosestPoint(p, false).Distance(p));
                var ptsbs = obsvertice.Where(p => plb.Contains(p)).OrderBy(p => lane.ClosestPoint(p, false).Distance(p));
                if (ptsas.Count() == 0 || ptsbs.Count() == 0) continue;
                var ptsa = ptsas.First();
                var ptsb = ptsbs.First();
                double disa = lanes[i].ClosestPoint(ptsa, false).Distance(ptsa);
                double disb = lanes[i].ClosestPoint(ptsb, false).Distance(ptsb);
                var d = (disa - disb) / 2;
                if (d != 0)
                {
                    lanes[i]=lanes[i].Translation(vec_a * d);
                    processedIndex.Add(i);
                }
            }
            //根据连接关系修复断线
            for (int i = 0; i < processedIndex.Count; i++)
            {
                var index = processedIndex[i];
                var sindex = Sindexes[index];
                var eindex = Eindexes[index];
                if (sindex != -1)
                    lanes[index].P0 = lanes[sindex].ClosestPoint(lanes[index].P0, false);
                if (eindex != -1)
                    lanes[index].P1 = lanes[eindex].ClosestPoint(lanes[index].P1, false);
                for (int j = 0; j < Sindexes.Count; j++)
                {
                    if (Sindexes[j] == index)
                        lanes[j].P0 = lanes[index].ClosestPoint(lanes[j].P0, false);
                    if (Eindexes[j] == index)
                        lanes[j].P1 = lanes[index].ClosestPoint(lanes[j].P1, false);
                }
            }
        }
        private static void RemoveInvalidPartsOfEndLanes(ref List<LineSegment> lanes, List<Polygon> cars, List<int> Sindexes, List<int> Eindexes)
        {
            double dis = 5000;
            var points = new List<Coordinate>();
            foreach (var car in cars) points.AddRange(car.Coordinates);
            for (int i = 0; i < lanes.Count; i++)
            {
                if (Sindexes[i] == -1)
                {
                    var l = lanes[i];
                    var lane = new LineSegment(lanes[i]);
                    lane=lane.Scale((lane.Length - 2) / lane.Length);
                    var buffer = lane.Buffer(dis);
                    var ps = points.Where(p => buffer.Contains(p)).OrderByDescending(p => l.ClosestPoint(p, false).Distance(l.P1)).First();
                    ps = l.ClosestPoint(ps, false);
                    lanes[i].P0 = ps;
                }
                else if (Eindexes[i] == -1)
                {
                    var l = lanes[i];
                    var lane = new LineSegment(lanes[i]);
                    lane=lane.Scale((lane.Length - 2) / lane.Length);
                    var buffer = lane.Buffer(dis);
                    var pe = points.Where(p => buffer.Contains(p)).OrderByDescending(p => l.ClosestPoint(p, false).Distance(l.P0)).First();
                    pe = l.ClosestPoint(pe, false);
                    lanes[i].P1 = pe;
                }
            }
        }
    }
}
