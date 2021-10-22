using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using ThMEPStructure.GirderConnect.Utils;

namespace ThMEPStructure.GirderConnect.ConnectProcess
{
    class ConnectMainBeam
    {

        public static List<Tuple<Point3d, Point3d>> Calculate(Point3dCollection points)
        {
            //接收提取的数据

            //生成主梁



            //输出主梁线

            return new List<Tuple<Point3d, Point3d>>();
        }



        ////
        //public static void GetBorderPoints(List<Polyline> polylines, )
        //{
        //首先找到轮廓线， 作垂线找到最近的点，以这个点为圆心，找到半径为这个点到出发点的距离
        //在这个范围内找合适的点。按照优先级来找，，，，，优先剪力墙、垂直下来的、半径以内剪力墙、（到时候参考需求文本）
        //}


        public static List<Tuple<Point3d, Point3d>> FstStep(List<Point3d> points)
        {

            return new List<Tuple<Point3d, Point3d>>();
        }

        /// <summary>
        /// 使用维诺图方式进行初步链接
        /// </summary>
        /// <param name="points"></param>
        public static void VoronoiDiagramConnect(Point3dCollection points)
        {
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); //通过有方向的某根线找到其包围的点
            Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines = new Dictionary<Point3d, List<Tuple<Point3d, Point3d>>>(); // 通过柱中点找到柱维诺图边界的点

            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());

            //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries) //同等效力
            foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
            {
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt))
                    {
                        List<Tuple<Point3d, Point3d>> aroundLines = new List<Tuple<Point3d, Point3d>>();
                        for (int i = 0; i < polyline.NumberOfVertices - 1; ++i)
                        {
                            Tuple<Point3d, Point3d> border = new Tuple<Point3d, Point3d>(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1));
                            line2pt.Add(border, pt);
                            aroundLines.Add(border);
                        }
                        if (!pt2lines.ContainsKey(pt))
                        {
                            pt2lines.Add(pt, aroundLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList());
                        }
                        break;
                    }
                }
            }

            HashSet<Tuple<Point3d, Point3d>> connectLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (Point3d pt in points)
            {
                //HostApplicationServices.WorkingDatabase.AddToModelSpace(pt2Polygon[pt].ToDbEntity());//
                connectNaighbor(pt, pt2lines, line2pt, connectLines);
            }

            foreach (var line in connectLines)
            {
                if (connectLines.Contains(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))) //是否双线
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2);
                }
            }
        }

        /// <summary>
        /// 当前点链接周围的点
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pt2lines"></param>
        /// <param name="line2pt"></param>
        /// <param name="connectLines"></param>
        public static void connectNaighbor(Point3d point, Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines, Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt, HashSet<Tuple<Point3d, Point3d>> connectLines)
        {
            int cnt = 0;
            if (pt2lines.ContainsKey(point))
            {
                foreach (Tuple<Point3d, Point3d> line in pt2lines[point])
                {
                    Tuple<Point3d, Point3d> conversLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                    if (cnt > 3 || !line2pt.ContainsKey(conversLine))
                    {
                        break;
                    }
                    Tuple<Point3d, Point3d> connectLine = new Tuple<Point3d, Point3d>(point, line2pt[conversLine]);

                    connectLines.Add(connectLine);

                    //draw_line(point, line2pt[conversLine], 130);
                    ++cnt;
                }
            }
        }

        /// <summary>
        /// 使用德劳内三角划分进行初步链接
        /// </summary>
        /// <param name="points"></param>
        public static void DelaunayTriangulationConnect(Point3dCollection points)
        {
            HashSet<Line> lines = new HashSet<Line>();
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0 ：初始化 1：最长线 
            foreach (Entity diagram in points.DelaunayTriangulation())
            {
                if (diagram is Polyline pl)
                {
                    Line maxLine = new Line();
                    double maxLen = 0.0;
                    for (int i = 0; i < pl.NumberOfVertices - 1; ++i) // pl.NumberOfVertices == 4
                    {
                        Line line = new Line(pl.GetPoint3dAt(i), pl.GetPoint3dAt(i + 1));
                        linesType.Add(new Tuple<Point3d, Point3d>(line.StartPoint, line.EndPoint), 0);
                        lines.Add(line);
                        if (line.Length > maxLen)
                        {
                            maxLen = line.Length;
                            maxLine = line;
                        }
                    }
                    linesType[new Tuple<Point3d, Point3d>(maxLine.StartPoint, maxLine.EndPoint)] = 1;
                    if (linesType.ContainsKey(new Tuple<Point3d, Point3d>(maxLine.EndPoint, maxLine.StartPoint)))
                    {
                        linesType[new Tuple<Point3d, Point3d>(maxLine.EndPoint, maxLine.StartPoint)] = 1;
                    }
                }
            }
            foreach (var line in linesType.Keys)
            {
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                }
            }
        }

        /// <summary>
        /// 使用带有约束的德劳内三角划分进行初步链接
        /// </summary>
        /// <param name="points"></param>
        /// <param name="polylines">约束</param>
        public static void ConformingDelaunayTriangulationConnect(Point3dCollection points, MultiLineString polylines)
        {
            var objs = new DBObjectCollection();
            var builder = new ConformingDelaunayTriangulationBuilder();
            var sites = ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPointFromCoords(points.ToNTSCoordinates());
            builder.SetSites(sites);
            builder.Constraints = polylines;
            var triangles = builder.GetTriangles(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach (var geometry in triangles.Geometries)
            {
                if (geometry is Polygon polygon)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            HashSet<Line> lines = new HashSet<Line>();
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0 ：初始化 1：最长线 
            foreach (Entity diagram in objs)
            {
                if (diagram is Polyline pl)
                {
                    Line maxLine = new Line();
                    double maxLen = 0.0;
                    for (int i = 0; i < pl.NumberOfVertices - 1; ++i) // pl.NumberOfVertices == 4
                    {
                        Line line = new Line(pl.GetPoint3dAt(i), pl.GetPoint3dAt(i + 1));
                        linesType.Add(new Tuple<Point3d, Point3d>(line.StartPoint, line.EndPoint), 0);
                        lines.Add(line);
                        if (line.Length > maxLen)
                        {
                            maxLen = line.Length;
                            maxLine = line;
                        }
                    }
                    linesType[new Tuple<Point3d, Point3d>(maxLine.StartPoint, maxLine.EndPoint)] = 1;
                    if (linesType.ContainsKey(new Tuple<Point3d, Point3d>(maxLine.EndPoint, maxLine.StartPoint)))
                    {
                        linesType[new Tuple<Point3d, Point3d>(maxLine.EndPoint, maxLine.StartPoint)] = 1;
                    }
                }
            }
            foreach (var line in linesType.Keys)
            {
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                }
            }
        }

        /// <summary>
        /// 将多边形分割成四边形或三角形列表
        ///  每次只切一刀，将一个多边形切成两个多边形，然后分别对切割后的多边形进行递归切割
        ///  递归边界：如果多边形的边小于5，不会再切割；如果多边形的边等于5，是否切割需要进行判断；
        /// </summary>
        /// <param name="tuples">要分割的图形（多于4边）</param>
        /// <returns>分割后的图形</returns>
        public static void SplitPolyline(List<Tuple<Point3d, Point3d>> tuples, List<List<Tuple<Point3d, Point3d>>> tupleLines)
        {
            int n = tuples.Count;
            if(n <= 5)
            {
                tupleLines.Add(tuples);
                return;
            }
            Polyline polyline = LineDealer.Tuples2Polyline(tuples);
            double area = polyline.Area;

            //每一🔪肯定是尽可能从中间去切开，找到能把切开后面积的方差最小的，如有面积相似的，找连接线长最短的（连接线长*面积的方差和最小的？）
            double minArea = double.MaxValue;
            double minDis = double.MaxValue;
            double minCmp = double.MaxValue;
            int halfCnt = (n + 1) / 2;

            //Tuples2Polyline get Area/2:halfArea

            //get minArea, record best split
            for (int i = 0; i < (n + 1) / 2; ++i)
            {
                //get tuplesA & areaA
                //get tuplesB & areaB
                //curCmp =  ((areaA - halfArea)^2 + (areaB - halfArea)^2) * dis;
                //if(curCmp < cmp)
                //{
                //  minCmp = curCmp;
                //}

            }
        }

        /// <summary>
        /// 合并两个多边形为一个
        /// </summary>
        /// <param name="polylineA"></param>
        /// <param name="polylineB"></param>
        /// <returns></returns>
        public static List<Tuple<Point3d, Point3d>> MergePolyline(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB, double tolerance = 1)
        {
            HashSet<Tuple<Point3d, Point3d>> lineVisited = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in polylineA)
            {
                lineVisited.Add(line);
            }
            foreach (var line in polylineB)
            {
                var converseLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                if (lineVisited.Contains(converseLine))
                {
                    lineVisited.Remove(converseLine);
                    continue;
                }
                lineVisited.Add(line);
            }
            return LineDealer.OrderTuples(lineVisited.ToList());
        }

        /// <summary>
        /// 针对(2*n+1) + (3)边形，转变成偶数边形, 然后分成多个小边形
        /// </summary>
        /// <param name="polylineA"></param>
        /// <param name="polylineB"></param>
        /// <returns></returns>
        public static void CaseOddP3(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB)
        {
            List<Tuple<Point3d, Point3d>> sixLines = MergePolyline(polylineA, polylineB);
            List<List<Tuple<Point3d, Point3d>>> polylines = new List<List<Tuple<Point3d, Point3d>>>();
            SplitPolyline(sixLines, polylines);
            foreach (var lines in polylines)
            {
                foreach (var line in lines)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                }
            }
        }
    }
}
