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
    public static partial class LayoutPostProcessing
    {
        public static void PostProcessLanes(ref List<Line> lanes, List<Polyline> cars, List<Polyline> inipillars, List<Point3d> obsvertice)
        {
            //删除一些重复的、相邻很近可删除其中一条的车道线并进行合并处理
            RemoveDuplicatedAndInvalidLanes(ref lanes);
            //用Sindexes,Eindexes表示连接关系
            List<int> Sindexes = new List<int>();
            List<int> Eindexes = new List<int>();
            GetConnectedRelationship(lanes, ref Sindexes, ref Eindexes);
            //重定位车道线，居中处理
            foreach (var car in cars) obsvertice.AddRange(car.Vertices().Cast<Point3d>());
            foreach (var pillar in inipillars) obsvertice.AddRange(pillar.Vertices().Cast<Point3d>());
            RelocateLanes(ref lanes, obsvertice, Sindexes, Eindexes);
            //删除未连接车道线的末尾多余部分
            RemoveInvalidPartsOfEndLanes(ref lanes, cars, Sindexes, Eindexes);
            //交点打断
            lanes = GetLinesByInterruptingIntersections(lanes);
        }
        private static void RemoveDuplicatedAndInvalidLanes(ref List<Line> lanes)
        {
            if (lanes.Count < 2) return;
            //删除部分共线的直线只保留一份
            for (int i = 0; i < lanes.Count - 1; i++)
            {
                for (int j = i + 1; j < lanes.Count; j++)
                {
                    if (IsParallelLine(lanes[i], lanes[j]) && lanes[j].GetClosestPointTo(lanes[i].GetCenter(), true).DistanceTo(lanes[i].GetCenter()) < 0.001)
                    {
                        if (lanes[j].GetClosestPointTo(lanes[i].StartPoint, false).DistanceTo(lanes[i].StartPoint) < 0.001
                            && lanes[j].GetClosestPointTo(lanes[i].EndPoint, false).DistanceTo(lanes[i].EndPoint) > 0.001)
                        {
                            var p = lanes[j].StartPoint.DistanceTo(lanes[i].EndPoint) <= lanes[j].EndPoint.DistanceTo(lanes[i].EndPoint) ?
                                lanes[j].StartPoint : lanes[j].EndPoint;
                            lanes[i] = new Line(p, lanes[i].EndPoint);
                        }
                        if (lanes[j].GetClosestPointTo(lanes[i].EndPoint, false).DistanceTo(lanes[i].EndPoint) < 0.001
                            && lanes[j].GetClosestPointTo(lanes[i].StartPoint, false).DistanceTo(lanes[i].StartPoint) > 0.001)
                        {
                            var p = lanes[j].StartPoint.DistanceTo(lanes[i].StartPoint) <= lanes[j].EndPoint.DistanceTo(lanes[i].StartPoint) ?
                                lanes[j].StartPoint : lanes[j].EndPoint;
                            lanes[i] = new Line(lanes[i].StartPoint, p);
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
                    bool isSimilarDuplicated = IsParallelLine(lanes[i], lanes[j]) && lanes[j].GetClosestPointTo(lanes[i].GetCenter(), false).DistanceTo(lanes[i].GetCenter()) < tol
                        && Math.Abs(lanes[j].GetClosestPointTo(lanes[i].StartPoint, false).DistanceTo(lanes[i].StartPoint) - lanes[j].GetClosestPointTo(lanes[i].EndPoint, false).DistanceTo(lanes[i].EndPoint)) < 1;
                    if (isSimilarDuplicated)
                    {
                        lanes.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }
        private static void GetConnectedRelationship(List<Line> lanes, ref List<int> Sindexes, ref List<int> Eindexes)
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
                        if (lanes[j].GetClosestPointTo(lanes[i].StartPoint, false).DistanceTo(lanes[i].StartPoint) < 0.001)
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
                        if (lanes[j].GetClosestPointTo(lanes[i].EndPoint, false).DistanceTo(lanes[i].EndPoint) < 0.001)
                        {
                            Eindexes[i] = j;
                            break;
                        }
                    }
                }
            }
        }
        private static void RelocateLanes(ref List<Line> lanes, List<Point3d> obsvertice, List<int> Sindexes, List<int> Eindexes)
        {
            //重定位
            double dis = 5000;
            List<int> processedIndex = new List<int>();
            for (int i = 0; i < lanes.Count; i++)
            {
                var vec_a = CreateVector(lanes[i]).GetPerpendicularVector().GetNormal();
                var vec_b = -vec_a;
                var lane = lanes[i];
                var l = CreateLine(lanes[i]);
                l.Scale(l.GetCenter(), (l.Length - 2) / l.Length);
                var lane_a = CreateLine(l);
                var lane_b = CreateLine(l);
                lane_a.TransformBy(Matrix3d.Displacement(vec_a * dis));
                lane_b.TransformBy(Matrix3d.Displacement(vec_b * dis));
                var pla = CreatPolyFromLines(lanes[i], lane_a);
                var plb = CreatPolyFromLines(lanes[i], lane_b);
                var ptsas = obsvertice.Where(p => pla.Contains(p)).OrderBy(p => lane.GetClosestPointTo(p, false).DistanceTo(p));
                var ptsbs = obsvertice.Where(p => plb.Contains(p)).OrderBy(p => lane.GetClosestPointTo(p, false).DistanceTo(p));
                if (ptsas.Count() == 0 || ptsbs.Count() == 0) continue;
                var ptsa = ptsas.First();
                var ptsb = ptsbs.First();
                double disa = lanes[i].GetClosestPointTo(ptsa, false).DistanceTo(ptsa);
                double disb = lanes[i].GetClosestPointTo(ptsb, false).DistanceTo(ptsb);
                var d = (disa - disb) / 2;
                if (d != 0)
                {
                    lanes[i].TransformBy(Matrix3d.Displacement(vec_a * d));
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
                    lanes[index].StartPoint = lanes[sindex].GetClosestPointTo(lanes[index].StartPoint, false);
                if (eindex != -1)
                    lanes[index].EndPoint = lanes[eindex].GetClosestPointTo(lanes[index].EndPoint, false);
                for (int j = 0; j < Sindexes.Count; j++)
                {
                    if (Sindexes[j] == index)
                        lanes[j].StartPoint = lanes[index].GetClosestPointTo(lanes[j].StartPoint, false);
                    if (Eindexes[j] == index)
                        lanes[j].EndPoint = lanes[index].GetClosestPointTo(lanes[j].EndPoint, false);
                }
            }
        }
        private static void RemoveInvalidPartsOfEndLanes(ref List<Line> lanes, List<Polyline> cars, List<int> Sindexes, List<int> Eindexes)
        {
            double dis = 5000;
            var points = new List<Point3d>();
            foreach (var car in cars) points.AddRange(car.Vertices().Cast<Point3d>());
            for (int i = 0; i < lanes.Count; i++)
            {
                if (Sindexes[i] == -1)
                {
                    var l = lanes[i];
                    var lane = CreateLine(lanes[i]);
                    lane.Scale(lane.GetCenter(), (lane.Length - 2) / lane.Length);
                    var buffer = lane.Buffer(dis);
                    var ps = points.Where(p => buffer.Contains(p)).OrderByDescending(p => l.GetClosestPointTo(p, false).DistanceTo(l.EndPoint)).First();
                    ps = l.GetClosestPointTo(ps, false);
                    lanes[i].StartPoint = ps;
                }
                else if (Eindexes[i] == -1)
                {
                    var l = lanes[i];
                    var lane = CreateLine(lanes[i]);
                    lane.Scale(lane.GetCenter(), (lane.Length - 2) / lane.Length);
                    var buffer = lane.Buffer(dis);
                    var pe = points.Where(p => buffer.Contains(p)).OrderByDescending(p => l.GetClosestPointTo(p, false).DistanceTo(l.StartPoint)).First();
                    pe = l.GetClosestPointTo(pe, false);
                    lanes[i].EndPoint = pe;
                }
            }
        }
    }
}
