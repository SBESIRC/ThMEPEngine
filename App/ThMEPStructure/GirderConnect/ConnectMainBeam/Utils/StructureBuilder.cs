﻿using System;
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
                                continue;//line2pt.Add(border, new Point3d());
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
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))
                    && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0 && !tuples.Contains(line))
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
            //foreach (var line in linesType.Keys)
            //{
            //    //if (linesType[line] == 0 )//&& linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0)
            //    {
            //        ShowInfo.DrawLine(line.Item1, line.Item2, 130);
            //    }
            //}
        }

        /// <summary>
        /// 获取轮廓上与临近点相连的点组成的点对
        /// </summary>
        /// <param name="outlineNearPts">某轮廓和它的近点</param>
        /// <param name="outlineWalls">某轮廓和它的剪力墙</param>
        /// <param name="outlineClumns">某轮廓和它包含的柱点</param>
        /// <param name="outline2BorderNearPts">Input and Output</param>
        public static void PriorityBorderPoints(Dictionary<Polyline, Point3dCollection> outlineNearPts, Dictionary<Polyline, HashSet<Polyline>> outlineWalls,
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, ref Dictionary<Polyline, List<Point3d>> outline2ZeroPts)
        {
            List<Point3d> fstPtsS = new List<Point3d>();
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
                if (!outline2ZeroPts.ContainsKey(curOutline))
                {
                    outline2ZeroPts.Add(curOutline, new List<Point3d>());
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
                        Point3d cntNearPt = GetObject.GetPointByDirection(borderPt, curOutline.GetClosePoint(borderPt), outlineNearPt.Value, Math.PI / 6, 13000);//可能需要重写这个算法
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
                //var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
                foreach (var wall in outlineWalls[curOutline])
                {
                    if (wall.Closed == false || wall.Area < 10000)
                    {
                        continue;
                    }
                    tmpFstPts.Clear();
                    tmpThdPts.Clear();
                    //CenterLine.WallEdgePoint(wall.ToNTSPolygon(), 100, ref tmpFstPts, ref tmpThdPts);
                    PointsDealer.WallCrossPoint(wall.ToNTSPolygon(), ref tmpFstPts, ref tmpThdPts);//, ref zeroPts);
                    fstPts.AddRange(tmpFstPts);
                    thdPts.AddRange(tmpThdPts);
                    fstPtsS.AddRange(tmpFstPts);
                    outline2ZeroPts[curOutline].AddRange(tmpFstPts); /////////////////////这里可能重复添加了，可能需要进行单一化那个函数
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
                        Point3d verticalPt = GetObject.GetClosestPointByDirection(nearPt, aimDirection, 13000, curOutline);
                        if (verticalPt == nearPt || verticalPt.DistanceTo(nearPt) > 13000) 
                        {
                            continue;
                        }

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
                            toleranceDegree = Math.PI / 4; /////////////////////4
                        }

                        //找到近点nearPt最佳的边界连接点
                        Point3d borderPt = StructureDealer.BestConnectPt(nearPt, verticalPt, fstPts, thdPts, outlineWalls[curOutline], closetLine, toleranceDegree);//, zeroPts) ;
                        if(borderPt.DistanceTo(nearPt) > 13000)
                        {
                            continue;
                        }
                        if (!outline2BorderNearPts[curOutline].ContainsKey(borderPt))
                        {
                            outline2BorderNearPts[curOutline].Add(borderPt, new HashSet<Point3d>());
                        }
                        outline2BorderNearPts[curOutline][borderPt].Add(nearPt);
                        //ShowInfo.DrawLine(nearPt, borderPt, 1);
                    }
                }
            }
            //Merge very close points to one whithout change structure
            //LineDealer.SimplifyLineConnect(borderPt2NearPts, thdPts);
            LineDealer.SimplifyLineConnect(outline2BorderNearPts, fstPtsS);
        }

        /// <summary>
        /// Build a structure
        /// can find a polyline by any line in this polyline
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="findPolylineFromLines"></param>
        public static void BuildPolygons(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Tuple<Point3d, Point3d>,
            List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            Dictionary<Tuple<Point3d, Point3d>, int> lineVisit = new Dictionary<Tuple<Point3d, Point3d>, int>();
            foreach (var dicTuple in dicTuples)
            {
                foreach (var edPt in dicTuple.Value)
                {
                    if (!lineVisit.ContainsKey(new Tuple<Point3d, Point3d>(dicTuple.Key, edPt)))
                    {
                        lineVisit.Add(new Tuple<Point3d, Point3d>(dicTuple.Key, edPt), 0);
                    }
                }
            }
            List<Tuple<Point3d, Point3d>> tuppleList = lineVisit.Keys.ToList();
            foreach (var tuple in tuppleList)
            {
                if (lineVisit[tuple] == 0)
                {
                    var tmpLines = new List<Tuple<Point3d, Point3d>>();
                    lineVisit[tuple] = 1;
                    tmpLines.Add(tuple);
                    var curTuple = new Tuple<Point3d, Point3d>(tuple.Item1, tuple.Item2);
                    int flag = 0;
                    while (true)
                    {
                        Point3d nextPt = GetNextConnectPoint(curTuple.Item1, curTuple.Item2, dicTuples);
                        if (nextPt == curTuple.Item1) //find a leaf
                        {
                            flag = 1;
                            break;
                        }
                        curTuple = new Tuple<Point3d, Point3d>(curTuple.Item2, nextPt);
                        if (lineVisit[curTuple] == 1)
                        {
                            if (curTuple.Item2 != nextPt)
                            {
                                flag = 1;
                            }
                            break;
                        }
                        lineVisit[curTuple] = 1;
                        tmpLines.Add(curTuple);
                        if (nextPt == tuple.Item1) // had find a circle
                        {
                            break;
                        }
                    }

                    if (tmpLines.Count > 1 && flag != 1) //如果算法好，"tmpLines.Count > 1"是可以不要的
                    {
                        foreach (var tmpLine in tmpLines)
                        {
                            //ShowInfo.DrawLine(tmpLine.Item1, tmpLine.Item2);
                            if (!findPolylineFromLines.ContainsKey(tmpLine))
                            {
                                findPolylineFromLines.Add(tmpLine, tmpLines);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the next point in the polyline based on this corrent line
        /// </summary>
        /// <returns>if the return value equals to baseStPt，means there is no next point，also means this is a leaf point</returns>
        public static Point3d GetNextConnectPoint(Point3d baseStPt, Point3d baseEdPt, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            double minDegree = double.MaxValue;
            Vector3d baseVec = baseStPt - baseEdPt;
            Point3d aimEdPt = baseStPt;
            double curDegree;
            foreach (var curEdPt in dicTuples[baseEdPt])
            {
                if (curEdPt == baseStPt)
                {
                    continue;
                }
                curDegree = (curEdPt - baseEdPt).GetAngleTo(baseVec, Vector3d.ZAxis);
                if (curDegree < minDegree)
                {
                    minDegree = curDegree;
                    aimEdPt = curEdPt;
                }
            }
            return aimEdPt;
        }

        /// <summary>
        /// split block
        /// </summary>
        /// <param name="findPolylineFromLines"></param>
        /// <param name="closeBorderLines"></param>
        /// <param name="acdb"></param>
        public static void SplitBlock(Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, Dictionary<Point3d, Point3d> closeBorderLines = null)
        {
            //Remove closeBorder Lines
            if (closeBorderLines != null)
            {
                foreach (var closeBorderLine in closeBorderLines)
                {
                    if (findPolylineFromLines.ContainsKey(new Tuple<Point3d, Point3d>(closeBorderLine.Key, closeBorderLine.Value)))
                    {
                        findPolylineFromLines.Remove(new Tuple<Point3d, Point3d>(closeBorderLine.Key, closeBorderLine.Value));
                    }
                    //if (findPolylineFromLines.ContainsKey(new Tuple<Point3d, Point3d>(closeBorderLine.Value, closeBorderLine.Key)))
                    //{
                    //    findPolylineFromLines.Remove(new Tuple<Point3d, Point3d>(closeBorderLine.Value, closeBorderLine.Key));
                    //}
                }
            }
            //record line state: 0.init(exist & havenot seen); 1.visited and chose to stay; 2.vistited and chose to delete
            Dictionary<Tuple<Point3d, Point3d>, int> lineVisit = new Dictionary<Tuple<Point3d, Point3d>, int>();
            foreach (var tuple in findPolylineFromLines.Keys)
            {
                if (!lineVisit.ContainsKey(tuple))
                {
                    lineVisit.Add(tuple, 0);
                }
            }
            List<List<Tuple<Point3d, Point3d>>> splitedPolylines = new List<List<Tuple<Point3d, Point3d>>>();
            List<Tuple<Point3d, Point3d>> lines = lineVisit.Keys.ToList();
            //for each line in DCEL lines
            foreach (var line in lines)
            {
                if (lineVisit[line] == 0)
                {
                    if (findPolylineFromLines.ContainsKey(line) && findPolylineFromLines[line].Count > 5)
                    //if (findPolylineFromLines.ContainsKey(converseTuple) && findPolylineFromLines[converseTuple].Count == 3
                    //  && findPolylineFromLines.ContainsKey(line) && findPolylineFromLines[line].Count == 5)
                    {
                        StructureDealer.SplitPolyline(findPolylineFromLines[line], splitedPolylines);
                        var lList = findPolylineFromLines[line].ToList();
                        foreach (var l in lList)
                        {
                            lineVisit[l] = 1;
                            if (findPolylineFromLines.ContainsKey(l))
                            {
                                findPolylineFromLines.Remove(l);
                            }
                        }
                    }
                }
            }
            // change structure
            AddPolylinesToDic(splitedPolylines, findPolylineFromLines);
        }

        /// <summary>
        /// Merge neighbor fragments to one and split if it can
        /// 缺乏当一个三角形有多个可用时选择哪个的策略
        /// </summary>
        /// <param name="findPolylineFromLines"></param>
        public static void MergeFragments(Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, Dictionary<Point3d, Point3d> closeBorderLines = null)
        {
            //record line state: 0.init(exist & havenot seen); 1.visited and chose to stay; 2.vistited and chose to delete
            Dictionary<Tuple<Point3d, Point3d>, int> lineVisit = new Dictionary<Tuple<Point3d, Point3d>, int>();
            foreach (var tuple in findPolylineFromLines.Keys)
            {
                if (!lineVisit.ContainsKey(tuple))
                {
                    lineVisit.Add(tuple, 0);
                }
            }
            List<List<Tuple<Point3d, Point3d>>> mergedPolylines = new List<List<Tuple<Point3d, Point3d>>>();
            List<Tuple<Point3d, Point3d>> lines = lineVisit.Keys.ToList();
            foreach (var line in lines)
            {
                if (lineVisit[line] == 0)
                {
                    Tuple<Point3d, Point3d> converseLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                    if (!findPolylineFromLines.ContainsKey(converseLine) || !findPolylineFromLines.ContainsKey(line)
                        || ContainLines(findPolylineFromLines[converseLine], closeBorderLines) > 1)
                    {
                        continue;
                    }
                    if (findPolylineFromLines[converseLine].Count == 3 && (findPolylineFromLines[line].Count >= 5 ))//|| findPolylineFromLines[line].Count == 3))
                    {
                        List<Tuple<Point3d, Point3d>> evenLines =
                            StructureDealer.MergePolyline(findPolylineFromLines[line], findPolylineFromLines[converseLine]);
                        int obtuseAngleCount = LineDealer.ObtuseAngleCount(evenLines);
                        if (obtuseAngleCount == -1 || obtuseAngleCount > 0)
                        {
                            //foreach (var l in findPolylineFromLines[line])
                            //{
                            //    lineVisit[l] = 1;
                            //}
                            foreach (var l in findPolylineFromLines[converseLine])
                            {
                                lineVisit[l] = 1;
                            }
                            continue;
                        }
                        StructureDealer.SplitPolyline(evenLines, mergedPolylines);
                        if (findPolylineFromLines.ContainsKey(line))
                        {
                            findPolylineFromLines.Remove(line);
                        }
                        if (findPolylineFromLines.ContainsKey(converseLine))
                        {
                            findPolylineFromLines.Remove(converseLine);
                        }
                        // change structure
                        AddPolylinesToDic(mergedPolylines, findPolylineFromLines);
                    }
                }
            }
        }

        public static void AddPolylinesToDic(List<List<Tuple<Point3d, Point3d>>> splitedPolylines, 
            Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            foreach (var splitedPolyline in splitedPolylines)
            {
                if (splitedPolyline.Count > 0)
                {
                    foreach (var l in splitedPolyline)
                    {
                        if (findPolylineFromLines.ContainsKey(l))
                        {
                            findPolylineFromLines.Remove(l);
                        }
                        findPolylineFromLines.Add(l, splitedPolyline);
                        var reverseL = new Tuple<Point3d, Point3d>(l.Item2, l.Item1);
                        if (findPolylineFromLines.ContainsKey(reverseL))
                        {
                            findPolylineFromLines.Remove(reverseL);
                        }
                    }
                }
            }
        }

        public static int ContainLines(List<Tuple<Point3d, Point3d>> oriTuples, Dictionary<Point3d, Point3d> closeBorderLines)
        {
            int cnt = 0;
            foreach(var oriTuple in oriTuples)
            {
                if(closeBorderLines.ContainsKey(oriTuple.Item1) && closeBorderLines[oriTuple.Item1] == oriTuple.Item2)
                {
                    cnt++;
                }
            }
            return cnt;
        }

        public static void WallConnect(Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Polyline, List<Point3d>> outline2ZeroPts)
        {
            //连接不同的边界
            //点上的连接至少为1

            //连接相同的边界
            //连接逻辑：
            //保留连接线中点不在这个边界内的线
            //连接线两端的点不在同一条直线上
            //同线判定：(（a到lineA<500 && b到lineA<500）||（b到lineB<500 && a到lineB<500）)  lineA\lineB分别为a\b最近的线
        }
    }
}
