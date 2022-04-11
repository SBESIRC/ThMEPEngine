using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Data;
using ThMEPElectrical.EarthingGrid.Generator.Utils;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class DownConductor
    {
        public static void AddDownConductorToEarthGrid(PreProcess preProcessData, ref Dictionary<Point3d, HashSet<Point3d>> earthGrid)
        {
            //1、获取点组
            var ptGroups = new List<HashSet<Point3d>>();
            GetPointsGroups(ref ptGroups, preProcessData.conductorGraph);

            //2、对每个组连接最近线
            ConnectWithEarthGrid(ptGroups,ref earthGrid);
        }

        //生成点集组
        public static void GetPointsGroups(ref List<HashSet<Point3d>> ptGroups, Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            HashSet<Point3d> ptVisted = new HashSet<Point3d>();
            foreach (var curPt in graph.Keys)
            {
                if (!ptVisted.Contains(curPt))
                {
                    HashSet<Point3d> onePtsGroup = new HashSet<Point3d>();
                    BFS(curPt, ref onePtsGroup, ref ptVisted, graph);
                    ptGroups.Add(onePtsGroup);
                }
            }
        }

        //广度遍历
        public static void BFS(Point3d basePt, ref HashSet<Point3d> onePtsGroup, ref HashSet<Point3d> ptVisted, Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            Queue<Point3d> queue = new Queue<Point3d>();
            queue.Enqueue(basePt);
            if (!onePtsGroup.Contains(basePt))
            {
                onePtsGroup.Add(basePt);
            }
            ptVisted.Add(basePt);
            while (queue.Count > 0)
            {
                Point3d topPt = queue.Dequeue();
                foreach (var pt in graph[topPt])
                {
                    if (!ptVisted.Contains(pt))
                    {
                        ptVisted.Add(pt);
                        queue.Enqueue(pt);
                        onePtsGroup.Add(pt);
                    }
                }
            }
        }

        private static void ConnectWithEarthGrid(List<HashSet<Point3d>> ptGroups, ref Dictionary<Point3d, HashSet<Point3d>> earthGrid)
        {
            var lines = LineDealer.Graph2Lines(earthGrid);
            foreach (var ptGroup in ptGroups)
            {
                double minDis = double.MaxValue;
                Point3d cloestPt = new Point3d();
                Point3d cloestBasePt = new Point3d();
                foreach(var pt in ptGroup)
                {
                    foreach(var line in lines)
                    {
                        Line tmpLine = new Line(line.Item1, line.Item2);
                        Point3d curCloestPt = tmpLine.GetClosestPointTo(pt, false);
                        double curDis = curCloestPt.DistanceTo(pt);
                        if (curDis < minDis)
                        {
                            minDis = curDis;
                            cloestPt = curCloestPt;
                            cloestBasePt = pt;
                        }
                    }
                }
                if(cloestBasePt != new Point3d() && cloestPt != new Point3d())
                {
                    GraphDealer.AddLineToGraph(cloestBasePt, cloestPt, ref earthGrid);
                }
            }
        }
    }
}
