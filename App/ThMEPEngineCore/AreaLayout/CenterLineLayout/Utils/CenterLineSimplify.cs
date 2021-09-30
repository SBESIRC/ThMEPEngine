using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.Utils
{
    public static class CenterLineSimplify
    {
        public static List<Line> CLSimplify(MPolygon mPolygon, double interpolationDistance = 300)
        {
            //1.init
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), interpolationDistance);
            Dictionary<Point3d, List<Point3d>> pt_edges = new Dictionary<Point3d, List<Point3d>>();
            Dictionary<Point3d, int> degree = new Dictionary<Point3d, int>();
            //points on centerline 1:has this point 2:watched and ready to delete 3:can not backtrace from now on
            Dictionary<Point3d, int> edgPts = new Dictionary<Point3d, int>();
            List<Point3d> leaves = new List<Point3d>();
            Point3d tmpStPt = new Point3d();
            Point3d tmpEdPt = new Point3d();
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    tmpStPt = line.StartPoint;
                    tmpEdPt = line.EndPoint;
                    degree[tmpStPt] = 0;
                    degree[tmpEdPt] = 0;
                    pt_edges[tmpStPt] = new List<Point3d>();
                }
            }
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    tmpStPt = line.StartPoint;
                    tmpEdPt = line.EndPoint;
                    pt_edges[tmpStPt].Add(tmpEdPt);
                    ++degree[tmpStPt];
                    ++degree[tmpEdPt];
                    //record the pole of a line
                    edgPts[tmpStPt] = 1;
                    edgPts[tmpEdPt] = 1;
                }
            }
            //abortable
            int bigPtCnt = 0;
            Queue<Point3d> que = new Queue<Point3d>();
            foreach (var node in degree)
            {
                if (node.Value <= 2)
                {
                    leaves.Add(node.Key);
                    que.Enqueue(node.Key);
                    edgPts[node.Key] = 2;
                }
                else if (node.Value > 4)
                {
                    edgPts[node.Key] = 3;
                    ShowInfo.ShowPointAsO(node.Key);
                    ++bigPtCnt;
                }
            }
            if (bigPtCnt == 0)//special case(leave like shape)
            {
                return centerlines.Cast<Line>().ToList();
            }

            //2.backtrace
            List<Point3d> toDeletePt = new List<Point3d>();
            while (que.Count != 0)
            {
                Point3d lineStart = que.Dequeue();
                toDeletePt.Add(lineStart);
                foreach(var lineEnd in pt_edges[lineStart])
                {
                    toDeletePt.Add(lineEnd);
                    if (edgPts[lineEnd] != 2 && edgPts[lineEnd] != 3)
                    {
                        edgPts[lineEnd] = 2;
                        que.Enqueue(lineEnd);
                    }
                }
            }

            //3.output
            foreach(var pt in toDeletePt)
            {
                if (pt_edges.ContainsKey(pt) && edgPts[pt] != 3)
                {
                    pt_edges.Remove(pt);
                    ShowInfo.ShowPointAsX(pt);
                }
            }
            List<Line> lines = new List<Line>();
            foreach(var node in pt_edges)
            {
                foreach(var pt in node.Value)
                {
                    if (edgPts[pt] != 2)
                    {
                        lines.Add(new Line(node.Key, pt));
                        HostApplicationServices.WorkingDatabase.AddToModelSpace(new Line(node.Key, pt));//show line
                    }
                }
            }
            return lines;
        }

        public static List<Point3d> CLSimplifyPts(MPolygon mPolygon, double interpolationDistance = 300)
        {
            //1.init
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), interpolationDistance);
            Dictionary<Point3d, List<Point3d>> pt_edges = new Dictionary<Point3d, List<Point3d>>();
            Dictionary<Point3d, int> degree = new Dictionary<Point3d, int>();
            //points on centerline 1:has this point 2:watched and ready to delete 3:can not backtrace from now on
            Dictionary<Point3d, int> edgPts = new Dictionary<Point3d, int>();
            List<Point3d> leaves = new List<Point3d>();
            Point3d tmpStPt = new Point3d();
            Point3d tmpEdPt = new Point3d();
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    tmpStPt = line.StartPoint;
                    tmpEdPt = line.EndPoint;
                    degree[tmpStPt] = 0;
                    degree[tmpEdPt] = 0;
                    pt_edges[tmpStPt] = new List<Point3d>();
                }
            }
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    tmpStPt = line.StartPoint;
                    tmpEdPt = line.EndPoint;
                    pt_edges[tmpStPt].Add(tmpEdPt);
                    ++degree[tmpStPt];
                    ++degree[tmpEdPt];
                    //record the pole of a line
                    edgPts[tmpStPt] = 1;
                    edgPts[tmpEdPt] = 1;
                }
            }
            //abortable
            int bigPtCnt = 0;
            Queue<Point3d> que = new Queue<Point3d>();
            foreach (var node in degree)
            {
                if (node.Value <= 2)
                {
                    leaves.Add(node.Key);
                    que.Enqueue(node.Key);
                    edgPts[node.Key] = 2;
                }
                else if (node.Value > 4)
                {
                    edgPts[node.Key] = 3;
                    ++bigPtCnt;
                }
            }

            List<Point3d> ans = new List<Point3d>();
            if (bigPtCnt == 0)//special case(leave like shape)
            {
                CenterPoints(mPolygon.ToNTSPolygon(), ans);
                return ans;
            }

            List<Point3d> toDeletePt = new List<Point3d>();

            //2.backtrace
            while (que.Count != 0)
            {
                Point3d lineStart = que.Dequeue();
                toDeletePt.Add(lineStart);
                foreach (var lineEnd in pt_edges[lineStart])
                {
                    toDeletePt.Add(lineEnd);
                    if (edgPts[lineEnd] != 2 && edgPts[lineEnd] != 3)
                    {
                        edgPts[lineEnd] = 2;
                        que.Enqueue(lineEnd);
                    }
                }
            }

            //3.output
            foreach (var pt in toDeletePt)
            {
                if (pt_edges.ContainsKey(pt))
                {
                    pt_edges.Remove(pt);
                }
            }
            foreach (var node in pt_edges)
            {
                ans.Add(node.Key);
            }
            return ans;
        }

        // show centerline
        public static void ShowCenterLine(MPolygon mPolygon, AcadDatabase acdb, int colorindex = 220)
        {
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), 300);
            centerlines.Cast<Entity>().ToList().CreateGroup(acdb.Database, colorindex);
        }

        // get the points on the centerline
        public static void CenterPoints(Polygon geometry, List<Point3d> centerPoints, double interpolationDistance = 300)
        {
            foreach (Polygon polygon in geometry.VoronoiDiagram(interpolationDistance).Geometries)
            {
                var iterator = new LinearIterator(polygon.Shell);
                for (; iterator.HasNext(); iterator.Next())
                {
                    if (!iterator.IsEndOfLine)
                    {
                        var line = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(new Coordinate[] { iterator.SegmentStart, iterator.SegmentEnd });
                        if (line.Within(geometry))
                        {
                            Point3d curPoint = new Point3d(iterator.SegmentStart.X, iterator.SegmentStart.Y, 0);
                            centerPoints.Add(curPoint);
                        }
                    }
                }
            }
        }
    }
}