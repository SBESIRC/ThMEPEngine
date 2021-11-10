using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class StructureBuilder
    {
        /// <summary>
        /// Connect 4 dege diagram by VoronoiDiagram way
        /// </summary>
        /// <param name="points"></param>
        public static HashSet<Tuple<Point3d, Point3d>> VoronoiDiagramConnect(Point3dCollection points, Dictionary<Polyline, Point3dCollection> poly2points = null)
        {
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); //Find a point by its surrounding line
            Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines = new Dictionary<Point3d, List<Tuple<Point3d, Point3d>>>(); //Find surrounding lines by the point in them

            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());

            //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries) //Same use as fallow
            foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
            {
                if (polygon.IsEmpty)
                {
                    continue;
                }
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt) && !pt2lines.ContainsKey(pt))
                    {
                        if(poly2points != null)
                        {
                            foreach (var houseBorder in poly2points.Keys)
                            {
                                if (houseBorder.Intersects(polyline))
                                {
                                    poly2points[houseBorder].Add(pt);
                                    break;
                                }
                            }
                        }

                        List<Tuple<Point3d, Point3d>> aroundLines = new List<Tuple<Point3d, Point3d>>();
                        for (int i = 0; i < polyline.NumberOfVertices - 1; ++i)
                        {
                            Tuple<Point3d, Point3d> border = new Tuple<Point3d, Point3d>(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1));
                            line2pt.Add(border, pt);
                            aroundLines.Add(border);
                        }
                        pt2lines.Add(pt, aroundLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList());
                        break;
                    }
                }
            }

            HashSet<Tuple<Point3d, Point3d>> connectLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (Point3d pt in points)
            {
                connectNaighbor(pt, pt2lines, line2pt, connectLines);
            }
            HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in connectLines)
            {
                if (connectLines.Contains(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))) //judge double edge
                {
                    tuples.Add(line);
                }
            }
            return tuples;
        }

        /// <summary>
        /// Find naighbor points
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
        /// Connect 4 dege diagram by DelaunayTriangulation way
        /// </summary>
        /// <param name="points"></param>
        public static HashSet<Tuple<Point3d, Point3d>> DelaunayTriangulationConnect(Point3dCollection points)
        {
            HashSet<Line> lines = new HashSet<Line>();
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0：init 1：lognest line 
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

            HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in linesType.Keys)
            {
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0 && !tuples.Contains(line))
                {
                    //ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                    tuples.Add(line);
                }
            }
            return tuples;
        }

        /// <summary>
        /// Connect 4 dege diagram by ConformingDelaunayTriangulation way
        /// </summary>
        /// <param name="points"></param>
        /// <param name="polylines">constrain</param>
        public static void ConformingDelaunayTriangulationConnect(Point3dCollection points, MultiLineString polylines)
        {
            var objs = new DBObjectCollection();
            var builder = new ConformingDelaunayTriangulationBuilder();
            var sites = ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPointFromCoords(points.ToNTSCoordinates());
            builder.SetSites(sites);
            builder.Constraints = polylines;
            //builder.Tolerance = 500;
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
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0：init 1：lognest line 
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
                //if (linesType[line] == 0 )//&& linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                }
            }
        }

        /// <summary>
        /// 获取轮廓上与临近点相连的点组成的点对
        /// </summary>
        /// <param name="outlineNearPts">某轮廓和它的近点</param>
        /// <param name="outlineWalls">某轮廓和它的剪力墙</param>
        /// <param name="outlineClumns">某轮廓和它包含的柱点</param>
        /// <param name="outline2BorderNearPts">Input and Output</param>
        public static void PriorityBorderPoints(Dictionary<Polyline, Point3dCollection> outlineNearPts, Dictionary<Polyline, List<Polyline>> outlineWalls, 
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            List<Point3d> fstPts = new List<Point3d>();
            List<Point3d> thdPts = new List<Point3d>();
            List<Point3d> tmpFstPts = new List<Point3d>();
            List<Point3d> tmpThdPts = new List<Point3d>();
            List<Point3d> outPts = new List<Point3d>();
            Polyline curOutline;
            //Dictionary<Point3d, double> tmpPriotityCntPt = new Dictionary<Point3d, double>();
            foreach (var outlineNearPt in outlineNearPts)
            {
                curOutline = outlineNearPt.Key;
                if (!outline2BorderNearPts.ContainsKey(curOutline))
                {
                    outline2BorderNearPts.Add(curOutline, new Dictionary<Point3d, HashSet<Point3d>>());
                }

                Point3dCollection tmpNearPts = new Point3dCollection();
                foreach (Point3d pt in outlineNearPt.Value)
                {
                    tmpNearPts.Add(pt);
                }

                //处理0优先级：先连接轮廓内柱子到近点
                if (outlineClumns.ContainsKey(curOutline))
                {
                    foreach (Point3d borderPt in outlineClumns[curOutline])
                    {
                        //outlineNearPt.Value 有可能这些点并不是最好的，有可能需要加入的是所有其他柱点
                        Point3d cntNearPt = GetObject.GetPointByDirection(borderPt, curOutline.GetClosePoint(borderPt), outlineNearPt.Value, Math.PI / 6);//可能需要重写这个算法
                        if (cntNearPt == borderPt)
                        {
                            continue;//说明从这个边框内柱点找不到外部相连的近点
                        }
                        if (!outline2BorderNearPts[curOutline].ContainsKey(borderPt))
                        {
                            outline2BorderNearPts[curOutline].Add(borderPt, new HashSet<Point3d>());
                        }
                        if (!outline2BorderNearPts[curOutline][borderPt].Contains(cntNearPt))
                        {
                            outline2BorderNearPts[curOutline][borderPt].Add(cntNearPt);
                            //ShowInfo.DrawLine(borderPt, cntNearPt, 7);
                        }
                        if (tmpNearPts.Contains(cntNearPt))
                        {
                            tmpNearPts.Remove(cntNearPt);
                        }
                    }
                }

                fstPts.Clear();
                thdPts.Clear();
                outPts.Clear();
                //对于每一个轮廓线，先求它（包含的剪力墙）的折角点（Pr1）和边界点（Pr3）
                if (!outlineWalls.ContainsKey(curOutline))
                {
                    continue;
                }
                foreach (var wall in outlineWalls[curOutline])
                {
                    tmpFstPts.Clear();
                    tmpThdPts.Clear();
                    CenterLine.WallEdgePoint(wall.ToNTSPolygon(), 100, ref tmpFstPts, ref tmpThdPts);
                    fstPts.AddRange(tmpFstPts);
                    thdPts.AddRange(tmpThdPts);
                }
                outPts = PointsDealer.OutPoints(curOutline);
                PointsDealer.RemovePointsFarFromOutline(fstPts, curOutline);
                PointsDealer.RemovePointsFarFromOutline(thdPts, curOutline);

                foreach (Point3d nearPt in tmpNearPts)
                {
                    Point3d outlinePt = curOutline.GetClosePoint(nearPt);
                    //double baseDis = nearPt.DistanceTo(outlinePt);
                    Vector3d baseDirection = outlinePt - nearPt;

                    for (int i = 0; i < 4; ++i)
                    {
                        Vector3d aimDirection = baseDirection.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis);
                        double toleranceDegree = Math.PI / 15;
                        //Get VerticalPoint
                        Point3d verticalPt = GetObject.GetClosestPointByDirection(nearPt, aimDirection, 9000, curOutline);
                        if (verticalPt == nearPt)
                        {
                            continue;
                        }
                        //ShowInfo.ShowPointAsO(verticalPt, 1, 300);

                        //Get the line who contains the point will be connect
                        //Line closetLine = GetObject.GetClosetLineOfPolyline(curOutline, nearPt, aimDirection);
                        Line closetLine = GetObject.FindLineContainPoint(curOutline, verticalPt);
                        if (closetLine == null)
                        {
                            continue;
                        }
                        //ShowInfo.DrawLine(closetLine.StartPoint, closetLine.EndPoint, 50);
                        if (i == 0)
                        {
                            toleranceDegree = Math.PI / 4;
                        }

                        //找到近点nearPt最佳的边界连接点
                        Point3d borderPt = StructureDealer.BestConnectPt(nearPt, verticalPt, fstPts, thdPts, outlineWalls[curOutline], closetLine, toleranceDegree);

                        if (!outline2BorderNearPts[curOutline].ContainsKey(borderPt))
                        {
                            outline2BorderNearPts[curOutline].Add(borderPt, new HashSet<Point3d>());
                        }
                        outline2BorderNearPts[curOutline][borderPt].Add(nearPt);
                        //ShowInfo.DrawLine(nearPt, borderPt, 90);
                    }
                }
            }
            //Merge very close points to one whithout change structure
            LineDealer.SimplifyLineConnect(outline2BorderNearPts, outPts);
            //LineDealer.SimplifyLineConnect(borderPt2NearPts, thdPts);
            //LineDealer.SimplifyLineConnect(borderPt2NearPts, fstPts);
        }

        /// <summary>
        /// 通过线找到包含这条线的内部为空多边形
        /// </summary>
        public static void BuildPolygons(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLine)
        {
            List<Point3d> points = dicTuples.Keys.ToList();
            Dictionary<Point3d, int> ptVisited = new Dictionary<Point3d, int>();
            foreach(var startPt in points)
            {
                foreach(var endPt in dicTuples[startPt])
                {

                }
            }
            //List<Tuple<Point3d, Point3d>> lines = new List<Tuple<Point3d, Point3d>>();
            //lines.Add(curLine);
            //Tuple<Point3d, Point3d> tmpLine = new Tuple<Point3d, Point3d>();
            ////当能获得到下一根线的时候，就一直获取下一根线，如果获取不到了， 就返回null
            ////如果一开始就没有，
            //while ()
            //{
            //    lines.Add(GetNextLine(linesm));
            //}
        }

        /// <summary>
        /// 获取当前线所在多边形的下一条线段
        /// </summary>
        /// <param name="curLine"></param>
        /// <param name=""></param>
        /// <returns></returns>
        //public static Tuple<Point3d, Point3d> GetNextLine(Tuple<Point3d, Point3d> curLine, Dictionary<Tuple<Point3d, Point3d>, int> LineVisit)
        public static void GetNextLine(Tuple<Point3d, Point3d> curLine, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            ////如果下一个线不为空而且没有被访问过 ,则访问这个线
            ////////////////////////////////////////////////////////////////////////////////////////////此代码可能有问题，参考上面的注释和参数列表
            //double maxCmp = double.MinValue;
            //Point3d nextPt = new Point3d();
            //foreach (var point in lines[curLine.Item2])
            //{
            //    if (point == curLine.Item1)
            //    {
            //        continue;
            //    }
            //    var tmp = PointsDealer.DirectionCompair(curLine.Item1, curLine.Item2, point);
            //    if (tmp > maxCmp)
            //    {
            //        maxCmp = tmp;
            //        nextPt = point;
            //    }
            //}
            //return new Tuple<Point3d, Point3d>(curLine.Item1, nextPt);
        }
    }
}
