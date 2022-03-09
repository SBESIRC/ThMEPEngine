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
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThMEPElectrical.EarthingGrid.Generator.Connect;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.EarthGrid.Generator.Test
{
    class test
    {


        [CommandMethod("TIANHUACAD", "THClrearConnect", CommandFlags.Modal)]
        public void THClrearConnect()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                //获取柱点
                var columnPts = GetObjects.GetCenters(acdb);
                //获取外边框
                List<Polyline> outlines = GetObjects.GetPolylines(acdb);
                if (outlines == null)
                {
                    return;
                }
                //获取墙
                List<Polyline> walls = GetObjects.GetPolylines(acdb);
                if (walls == null)
                {
                    return;
                }
                Dictionary<Polyline, List<Polyline>> outlineWithWalls = new Dictionary<Polyline, List<Polyline>>();
                HashSet<Polyline> buildingOutline = new HashSet<Polyline>();
                var grid = new GridGenerator(outlineWithWalls, outlines, buildingOutline, columnPts);
                grid.Genterator();
            }
        }
        //获取内容：
        //一个polyline对应一堆点
        public static void main()
        {
            //记录每一条线和他身边的那些点
            Dictionary<Polyline, HashSet<Point3d>> outlineWithPts = new Dictionary<Polyline, HashSet<Point3d>>();

            //NearConnects


            //墙点：
            //高优先级：柱点、classify找出的点
            //较低优先级找出的点：剪力墙的拐点（中心线算法算出的）
            //（近点找到的垂点是最低优先级）

            //Polyline 分为 两类：分别是
            //1、ForbiddenOutline 里面不能进行连接的多边形：比如房间外边框、割除的多边形边界
            //2、ConnectOutline 内部可以胡乱连的多边形


            //获取近点的两种方式：
            //1、可以内缩+外扩包围圈内的点都可以是近点，近点连接边界点后进行一次双向删线（删相似线，保留距离近的）
            //2、通过维诺图进行相交查看，然后相交的是近点

            // nearPt-->borderPt/wall
            //近点连接逻辑、优先级
            //优先柱点和柱点之间的连接（墙点）
            //45°找不到，就做垂线

            // borderPt-->nearPt/borderPt/columnPt
            //墙连接， 墙点和墙点进行连接，

            //连接之后进行一次相似线删除操作，即靠近角度相似的线给删掉
            //在合适的时候对连接线进行缩短并和墙相交删除



            //以上近点和墙点都连了
            //进行柱网生成



            //进行柱网结构组装
            //小的合并
            //大的分割  要写一个分割函数

        }


        //获取近点
        public static void VoronoiDiagramNearPoints(Point3dCollection points, ref Dictionary<Polyline, Point3dCollection> poly2points)
        {
            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());
            foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
            {
                if (polygon.IsEmpty)
                {
                    continue;
                }
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt))
                    {
                        if (poly2points != null)
                        {
                            foreach (var pl2pts in poly2points)
                            {
                                if (!pl2pts.Value.Contains(pt) && pl2pts.Key.Intersects(polyline))
                                {
                                    pl2pts.Value.Add(pt);
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        //生成网格
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
            //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries)
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
                        if (poly2points != null)
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
                            if (line2pt.ContainsKey(border))
                            {
                                continue;
                            }
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
                ConnectNeighbor(pt, pt2lines, line2pt, connectLines);
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
        public static void ConnectNeighbor(Point3d point, Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines,
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt, HashSet<Tuple<Point3d, Point3d>> connectLines)
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
                    for (int i = 0; i < pl.NumberOfVertices - 1; ++i)
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
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))
                    && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0 && !tuples.Contains(line))
                {
                    tuples.Add(line);
                }
            }
            return tuples;
        }

        //获取墙的拐点
        /// <summary>
        /// 获得墙的拐角点和边界点
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="fstPts"></param>
        /// <param name="SndPts"></param>
        public static void WallCrossPoint(Polyline pline, ref List<Point3d> fstPts, ref List<Point3d> SndPts)
        {
            fstPts.Clear();
            SndPts.Clear();
            //首先找出中心线
            var lines = ThMEPPolygonService.CenterLine(pline);
            //var lines = GetCenterLines(pline);
            Dictionary<Point3d, HashSet<Point3d>> pt2Pts = Lines2Tuples(lines);

            //对块进行分割
            var walls = new DBObjectCollection();
            var columns = new DBObjectCollection();
            ThVStructuralElementSimplifier.Classify(pline.ToNTSPolygon().ToDbCollection(), columns, walls);
            List<Point3d> zeroPts = new List<Point3d>();
            foreach (var ent in columns)
            {
                if (ent is Polyline polyline)
                {
                    zeroPts.Add(polyline.GetCentroidPoint());
                    fstPts.Add(polyline.GetCentroidPoint());
                }
            }
            foreach (var pt2Pt in pt2Pts)
            {
                var pt = pt2Pt.Key;
                foreach (var ent in walls)
                {
                    if (ent is Polyline polyline)
                    {
                        if (polyline.ContainsOrOnBoundary(pt))
                        {
                            bool flag = false;
                            foreach (var zeroPt in zeroPts)
                            {
                                if (pt.DistanceTo(zeroPt) < 300)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if (flag == true)
                            {
                                continue;
                            }
                            if (IsCrossPt(pt, pt2Pts))
                            {
                                fstPts.Add(pt);
                            }
                            else
                            {
                                SndPts.Add(pt);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Judge wether a point is a cross point
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="pt2Pts"></param>
        /// <returns></returns>
        public static bool IsCrossPt(Point3d pt, Dictionary<Point3d, HashSet<Point3d>> pt2Pts)
        {
            if (!pt2Pts.ContainsKey(pt))
            {
                return false;
            }
            foreach (var ptA in pt2Pts[pt])
            {
                var vecA = ptA - pt;
                foreach (var ptB in pt2Pts[pt])
                {
                    var vecB = ptB - pt;
                    var angel = vecA.GetAngleTo(vecB);
                    if (angel > Math.PI / 6 && angel < Math.PI / 6 * 5)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static Dictionary<Point3d, HashSet<Point3d>> Lines2Tuples(List<Line> lines)
        {
            Dictionary<Point3d, HashSet<Point3d>> pt2Pts = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var line in lines)
            {
                var stPt = line.StartPoint;
                var edPt = line.EndPoint;
                if (!pt2Pts.ContainsKey(stPt))
                {
                    pt2Pts.Add(stPt, new HashSet<Point3d>());
                }
                if (!pt2Pts[stPt].Contains(edPt))
                {
                    pt2Pts[stPt].Add(edPt);
                }
                if (!pt2Pts.ContainsKey(edPt))
                {
                    pt2Pts.Add(edPt, new HashSet<Point3d>());
                }
                if (!pt2Pts[edPt].Contains(stPt))
                {
                    pt2Pts[edPt].Add(stPt);
                }
            }
            return pt2Pts;
        }

    }
}
