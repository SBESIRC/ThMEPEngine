using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Algorithm;
using NFox.Cad;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class CenterLine
    {
        /// <summary>
        /// Simplify Centerline, remove leaves of centerline tree
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="interpolationDistance"></param>
        /// <returns></returns>
        public List<Line> CLSimplify(MPolygon mPolygon, double interpolationDistance = 300)
        {
            //1.init
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), interpolationDistance);
            Dictionary<Point3d, List<Point3d>> pt_edges = new Dictionary<Point3d, List<Point3d>>();
            Dictionary<Point3d, int> degree = new Dictionary<Point3d, int>();
            //points on centerline 1:has this point 2:watched and ready to delete 3:can not backtrace from now on
            Dictionary<Point3d, int> edgPts = new Dictionary<Point3d, int>();
            List<Point3d> leaves = new List<Point3d>();
            Point3d tmpStPt;
            Point3d tmpEdPt;
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
                    //ShowInfo.ShowPointAsO(node.Key, 130, 14.159265 * 2);//请勿删除
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
                if (pt_edges.ContainsKey(pt) && edgPts[pt] != 3)
                {
                    pt_edges.Remove(pt);
                    //ShowInfo.ShowPointAsX(pt, 80, 10);//请勿删除
                }
            }
            List<Line> lines = new List<Line>();
            foreach (var node in pt_edges)
            {
                foreach (var pt in node.Value)
                {
                    if (edgPts[pt] != 2)
                    {
                        lines.Add(new Line(node.Key, pt));
                        //HostApplicationServices.WorkingDatabase.AddToModelSpace(new Line(node.Key, pt));//show line
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// Branches Backtracking (delete branches by loop)
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="looptime">loop times，分叉回溯次数</param>
        /// <param name="interpolationDistance">分割细粒度</param>
        public static void CutBrancheLoop(MPolygon mPolygon, int looptime = 5, double interpolationDistance = 30)
        {
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), interpolationDistance);
            var lines = new List<Tuple<Point3d, Point3d>>();
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    lines.Add(new Tuple<Point3d, Point3d>(line.StartPoint, line.EndPoint));
                }
            }
            List<Point3d> points = new List<Point3d>();
            int color = 1;
            double radius = 100;
            for (int i = 0; i < looptime; ++i, color += 50, radius += 50, ++radius)
            {
                points = CutBranche(lines);
                foreach (var point in points)
                {
                    ShowInfo.ShowPointAsO(point, color, radius);
                }
                foreach (var line in lines)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, color);
                }
            }
        }

        /// <summary>
        /// Delete the outest branches in tree with double line structure, return leaves lines and inportant points
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Point3d> CutBranche(List<Tuple<Point3d, Point3d>> lines)
        {
            //1.init
            Dictionary<Point3d, List<Point3d>> pt_edges = new Dictionary<Point3d, List<Point3d>>();
            Dictionary<Point3d, int> degree = new Dictionary<Point3d, int>();
            //points on centerline 1:has this point 2:watched and ready to delete 3:can not backtrace from now on
            Dictionary<Point3d, int> edgPts = new Dictionary<Point3d, int>();
            List<Point3d> leaves = new List<Point3d>();
            foreach (var line in lines)
            {
                degree[line.Item1] = 0;
                degree[line.Item2] = 0;
                //record the pole of a line
                edgPts[line.Item1] = 1;
                edgPts[line.Item2] = 1;
                pt_edges[line.Item1] = new List<Point3d>();
            }
            foreach (var line in lines)
            {
                pt_edges[line.Item1].Add(line.Item2);
                ++degree[line.Item1];
                ++degree[line.Item2];
            }

            //abortable (can combine with backtrace block)
            int bigPtCnt = 0;
            Queue<Point3d> que = new Queue<Point3d>();
            List<Point3d> crossPt = new List<Point3d>();
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
                    //ShowInfo.ShowPointAsO(node.Key, 130, 14.159265 * 2);//请勿删除

                    crossPt.Add(node.Key);
                    ++bigPtCnt;
                }
            }
            if (bigPtCnt == 0)//special case(leave like shape)
            {
                return new List<Point3d>();//centerlines.Cast<Line>().ToList();
            }

            //2.backtrace
            List<Point3d> toDeletePt = new List<Point3d>();
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
            foreach (var pt in toDeletePt)
            {
                if (pt_edges.ContainsKey(pt) && edgPts[pt] != 3)
                {
                    pt_edges.Remove(pt);
                    //ShowInfo.ShowPointAsX(pt, 80, 10);//请勿删除
                }
            }

            //3.output
            lines.Clear();
            //lines = new List<Tuple<Point3d, Point3d>>(); //请勿删除
            foreach (var node in pt_edges)
            {
                foreach (var pt in node.Value)
                {
                    if (edgPts[pt] != 2)
                    {
                        lines.Add(new Tuple<Point3d, Point3d>(node.Key, pt));
                    }
                }
            }
            return crossPt;
        }

        /// <summary>
        /// Get Points in Polyline by Priority(based on centerline)
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="interpolationDistance"></param>
        /// <returns></returns>
        public static List<Point3d> WallEdgePoint(MPolygon mPolygon, double interpolationDistance = 30)
        {
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), interpolationDistance);
            var lines = new List<Tuple<Point3d, Point3d>>();
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    lines.Add(new Tuple<Point3d, Point3d>(line.StartPoint, line.EndPoint));
                }
            }
            var fstPoints = CutBranche(lines);

            Dictionary<Point3d, int> degree = new Dictionary<Point3d, int>();
            foreach (var line in lines)
            {
                degree[line.Item1] = 0;
                degree[line.Item2] = 0;
            }
            foreach (var line in lines)
            {
                ++degree[line.Item1];
                ++degree[line.Item2];
            }

            var sndPoints = new List<Point3d>();
            foreach (var point in degree)
            {
                if (point.Value == 2)
                {
                    fstPoints.Remove(point.Key);
                    sndPoints.Add(point.Key);
                    ShowInfo.ShowPointAsO(point.Key, 80, 150);
                }
            }
            foreach (var point in fstPoints)
            {
                ShowInfo.ShowPointAsO(point, 130, 250);
            }
            return new List<Point3d>();
        }
        public static void WallEdgePoint(Polygon polygon, double interpolationDistance, ref List<Point3d> fstPts, ref List<Point3d> SndPts)
        {
            fstPts.Clear();
            SndPts.Clear();
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(polygon, interpolationDistance);
            var lines = new List<Tuple<Point3d, Point3d>>();
            foreach (var cl in centerlines)
            {
                if (cl is Polyline line)
                {
                    lines.Add(new Tuple<Point3d, Point3d>(line.StartPoint, line.EndPoint));
                }
            }
            fstPts = CutBranche(lines);

            Dictionary<Point3d, int> degree = new Dictionary<Point3d, int>();
            foreach (var line in lines)
            {
                degree[line.Item1] = 0;
                degree[line.Item2] = 0;
            }
            foreach (var line in lines)
            {
                ++degree[line.Item1];
                ++degree[line.Item2];
            }

            foreach (var point in degree)
            {
                if (point.Value == 2)
                {
                    fstPts.Remove(point.Key);
                    SndPts.Add(point.Key);
                }
            }
        }

        public static List<Polyline> RECCenterLines(HashSet<Polyline> polylines)
        {
            var objs = new DBObjectCollection();
            var centerPolylines = new List<Polyline>();
            foreach (var polyline in polylines)
            {
                objs.Add(polyline);
            }
            //ThMEPEngineCoreLayerUtils.CreateAICenterLineLayer(acadDatabase.Database);
            objs.BuildArea()
                .OfType<Entity>()
                .ForEach(e =>
                {
                    ThMEPPolygonService.CenterLine(e)
                    .ToCollection()
                    .LineMerge()
                    .OfType<Polyline>()
                    .ForEach(o =>
                    {
                        centerPolylines.Add(o);
                    });
                });
            return centerPolylines;
        }
    }
}
