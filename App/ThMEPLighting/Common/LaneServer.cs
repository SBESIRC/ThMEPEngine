using System;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Linq;

namespace ThMEPLighting.Common
{
    public class LaneServer
    {
        public static List<List<Line>> getMergedOrderedLane(List<List<Line>> mainLanes, List<List<Line>> secondaryLanes)
        {

            //车道线顺序
            List<ThLightEdge> edges = new List<ThLightEdge>();

            mainLanes.ForEach(ls => ls.ForEach(l => edges.Add(new ThLightEdge(l))));
            secondaryLanes.ForEach(ls => ls.ForEach(l => edges.Add(new ThLightEdge(l))));

            List<List<Line>> orderedMergedLanes = new List<List<Line>>();
            orderedMergedLanes = MergedOrderedLane(edges);

            return orderedMergedLanes;
        }
        public static Point3d getMergedOrderedLane(List<ThLightEdge> edges)
        {
            Point3d startPoint = new Point3d();

            var orderedMergedLanes = MergedOrderedLane(edges);
            if (orderedMergedLanes.Count > 0 && orderedMergedLanes[0].Count > 0)
            {
                startPoint = orderedMergedLanes[0][0].StartPoint;
            }

            return startPoint;
        }

        private static List<List<Line>> MergedOrderedLane(List<ThLightEdge> edges)
        {

            //找起点
            Dictionary<Point3d, List<ThLightEdge>> nodeCollection = LaneServer.FindStartPoint(edges);

            Point3d startPoint = new Point3d();
            List<List<Line>> orderedMergedLanes = new List<List<Line>>();
            bool debug = true;

            while (debug == true && isAllTraversed(edges) == false)
            {
                int minOutdegree = nodeCollection.Where(x => isAllTraversed(x.Value) == false).Min(x => x.Value.Count);

                //debug = false;
                foreach (var ptOnce in nodeCollection)
                {
                    //if (ptOnce.Value.Count == 1)
                    if (ptOnce.Value.Count == minOutdegree)
                    {
                        startPoint = ptOnce.Key;
                        break;
                    }
                }

                //排序
                ThLightGraphService orderedLane = ThLightGraphService.Build(edges, startPoint);
                //按顺序排布车道线点并合并同一条线的车道线
                var orderedMergedLanesPart = mergeOrderedLane(orderedLane);
                //找这一组里面的最优解

                var optimalOrderedMergedLanes = findOptimalLanes(orderedMergedLanesPart, nodeCollection, startPoint, minOutdegree);

                foreach (var path in optimalOrderedMergedLanes)
                {
                    nodeCollection.Remove(path.First().StartPoint);
                    nodeCollection.Remove(path.Last().EndPoint);
                }
                orderedMergedLanes.AddRange(optimalOrderedMergedLanes);
            }

            return orderedMergedLanes;
        }

        /// <summary>
        /// 按顺序排布车道线点并合并同一条线的车道线
        /// </summary>
        /// <returns></returns>
        private static List<List<Line>> mergeOrderedLane(ThLightGraphService LightEdgeService)
        {

            List<List<Line>> OrderedMergedLane = new List<List<Line>>();

            for (int i = 0; i < LightEdgeService.Links.Count; i++)
            {

                List<Line> tempLine = new List<Line>();
                OrderedMergedLane.Add(tempLine);

                for (int j = 0; j < LightEdgeService.Links[i].Path.Count; j++)
                {
                    if (j == 0)
                    {
                        if (LightEdgeService.Links[i].Path[j].Edge.StartPoint != LightEdgeService.Links[i].Start)
                        {
                            LightEdgeService.Links[i].Path[j].Edge.ReverseCurve();


                        }
                        tempLine.Add(LightEdgeService.Links[i].Path[j].Edge);

                    }
                    else
                    {
                        if (LightEdgeService.Links[i].Path[j].Edge.StartPoint != LightEdgeService.Links[i].Path[j - 1].Edge.EndPoint)
                        {
                            LightEdgeService.Links[i].Path[j].Edge.ReverseCurve();
                        }
                        var nowEdge = (LightEdgeService.Links[i].Path[j].Edge.EndPoint - LightEdgeService.Links[i].Path[j].Edge.StartPoint).GetNormal();
                        var PreEdge = (LightEdgeService.Links[i].Path[j - 1].Edge.EndPoint - LightEdgeService.Links[i].Path[j - 1].Edge.StartPoint).GetNormal();
                        bool bAngle = Math.Abs(nowEdge.DotProduct(PreEdge)) / (nowEdge.Length * PreEdge.Length) < Math.Abs(Math.Cos(45 * Math.PI / 180));
                        if (bAngle)
                        {

                            tempLine = new List<Line>();
                            OrderedMergedLane.Add(tempLine);
                        }

                        tempLine.Add(LightEdgeService.Links[i].Path[j].Edge);
                    }
                }
            }

            return OrderedMergedLane;

        }

        private static Dictionary<Point3d, List<ThLightEdge>> FindStartPoint(List<ThLightEdge> edges)
        {

            Dictionary<Point3d, List<ThLightEdge>> startEndPtCollect = new Dictionary<Point3d, List<ThLightEdge>>();

            foreach (ThLightEdge edge in edges)
            {
                if (startEndPtCollect.ContainsKey(edge.Edge.StartPoint) == false)
                {
                    List<ThLightEdge> EdgeList = new List<ThLightEdge>();
                    EdgeList.Add(edge);
                    startEndPtCollect.Add(edge.Edge.StartPoint, EdgeList);
                }
                else
                {
                    startEndPtCollect[edge.Edge.StartPoint].Add(edge);
                }

                if (startEndPtCollect.ContainsKey(edge.Edge.EndPoint) == false)
                {
                    List<ThLightEdge> EdgeList = new List<ThLightEdge>();
                    EdgeList.Add(edge);
                    startEndPtCollect.Add(edge.Edge.EndPoint, EdgeList);
                }
                else
                {
                    startEndPtCollect[edge.Edge.EndPoint].Add(edge);
                }
            }


            return startEndPtCollect;
        }

        private static bool isAllTraversed(List<ThLightEdge> edges)

        {
            bool bReturn = true;
            foreach (var edge in edges)
            {
                if (edge.IsTraversed == false)
                {
                    bReturn = false;
                    break;
                }
            }

            return bReturn;
        }

        private static List<List<Line>> findOptimalLanes(List<List<Line>> orderedMergedLanesPart, Dictionary<Point3d, List<ThLightEdge>> nodeCollection, Point3d startPoint,int minOutdegree)
        {
            List<List<List<Line>>> allOrderedMergedLanes = new List<List<List<Line>>>();

            allOrderedMergedLanes.Add(orderedMergedLanesPart);

            //找到各线段终点并重新计算
            foreach (var path in orderedMergedLanesPart)
            {
               // if (nodeCollection[path.Last().EndPoint].Count == 1)
                    if (nodeCollection[path.Last().EndPoint].Count == minOutdegree)
                {
                    List<ThLightEdge> repeatEdge = new List<ThLightEdge>();
                    orderedMergedLanesPart.ForEach(ls => ls.ForEach(l => repeatEdge.Add(new ThLightEdge((Line)l.Clone()))));

                    startPoint = path.Last().EndPoint;
                    //排序
                    ThLightGraphService orderedLane = ThLightGraphService.Build(repeatEdge, startPoint);
                    //按顺序排布车道线点并合并同一条线的车道线
                    var LanesPart = mergeOrderedLane(orderedLane);
                    allOrderedMergedLanes.Add(LanesPart);
                }
            }

            //找到车道线分段最少的为最优解
            var minCount = allOrderedMergedLanes[0].Count;
            var minIndex = 0;
            for (int i = 0; i < allOrderedMergedLanes.Count; i++)
            {
                if (allOrderedMergedLanes[i].Count < minCount)
                {
                    minCount = allOrderedMergedLanes[i].Count;
                    minIndex = i;
                }
            }

            //var ba = allOrderedMergedLanes.Where(a => a.Count == (allOrderedMergedLanes.Select(x => x.Count).Min())).ToList()[0];

            return allOrderedMergedLanes[minIndex];
        }
    }
}
