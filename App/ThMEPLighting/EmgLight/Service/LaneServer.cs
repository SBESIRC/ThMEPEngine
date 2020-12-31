using System;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.EmgLight.Service
{
    public class LaneServer
 {
        public static List<List<Line>> getMergedOrderedLane(List<List<Line>> mainLanes, List<List<Line>> secondaryLanes)
        {

            //车道线顺序
            List<ThLightEdge> edges = new List<ThLightEdge>();

            mainLanes.ForEach(ls => ls.ForEach(l => edges.Add(new ThLightEdge(l))));
            secondaryLanes.ForEach(ls => ls.ForEach(l => edges.Add(new ThLightEdge(l))));

            //for (int i = 0; i < edges.Count; i++)
            //{
            //    InsertLightService.ShowGeometry(edges[i].Edge.StartPoint, string.Format("edge {0}-start", i), 40);
            //    InsertLightService.ShowGeometry(edges[i].Edge.EndPoint, string.Format("edge{0}-end", i), 40);
            //}

            //找起点
            Point3d startPoint = LaneServer.FindStartPoint(edges);

            //排序
            ThLightGraphService OrderedLane = ThLightGraphService.Build(edges, startPoint);
            InsertLightService.ShowGeometry(startPoint, "Start", 20);

            //按顺序排布车道线点并合并同一条线的车道线
            List<List<Line>> OrderedMergedLane = mergeOrderedLane(OrderedLane);



            //debug
            //for (int i = 0; i < OrderedLane.Links.Count; i++)
            //{
            //    for (int j = 0; j < OrderedLane.Links[i].Path.Count; j++)
            //    {
            //        InsertLightService.ShowGeometry(OrderedLane.Links[i].Path[j].Edge.StartPoint, string.Format("ordered{0}-{1}-start", i, j), 20);
            //        InsertLightService.ShowGeometry(OrderedLane.Links[i].Path[j].Edge.EndPoint, string.Format("ordered{0}-{1}-end", i, j), 20);
            //    }
            //}

            for (int i = 0; i < OrderedMergedLane.Count; i++)
            {
                for (int j = 0; j < OrderedMergedLane[i].Count; j++)
                {
                    InsertLightService.ShowGeometry(OrderedMergedLane[i][j].StartPoint, string.Format("orderM {0}-{1}-start", i, j), 161);
                    InsertLightService.ShowGeometry(OrderedMergedLane[i][j].EndPoint, string.Format("orderM {0}-{1}-end", i, j), 161);
                }
            }

            return OrderedMergedLane;
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

        private static Point3d FindStartPoint(List<ThLightEdge> edges)
        {
            Point3d startPt =new Point3d ();

            Dictionary <Point3d,int> startEndPtCollect = new Dictionary<Point3d,int>();

            foreach (ThLightEdge edge in edges)
            {
                if (startEndPtCollect .ContainsKey (edge.Edge.StartPoint) == false )
                {
                    startEndPtCollect.Add(edge.Edge.StartPoint, 1);
                }
                else
                {
                    startEndPtCollect[edge.Edge.StartPoint] += 1;
                }

                if (startEndPtCollect.ContainsKey(edge.Edge.EndPoint) == false)
                {
                    startEndPtCollect.Add(edge.Edge.EndPoint, 1);
                }
                else
                {
                    startEndPtCollect[edge.Edge.EndPoint] += 1;
                }
            }

            foreach (var ptOnce in startEndPtCollect)
            {
                if (ptOnce.Value ==1)
                {
                    startPt = ptOnce.Key;
                    break;
                }
            }

            return startPt;
        }
    }
}
