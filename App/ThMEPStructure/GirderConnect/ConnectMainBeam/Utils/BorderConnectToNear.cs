using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Geometries;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class BorderConnectToNear
    {
        Point3dCollection clumnPts = new Point3dCollection();
        Dictionary<Polyline, HashSet<Point3d>> outlineClumns = new Dictionary<Polyline, HashSet<Point3d>>();
        Dictionary<Polyline, Point3dCollection> outlineNearPts = new Dictionary<Polyline, Point3dCollection>();
        Dictionary<Polyline, HashSet<Polyline>> outlineWallsMerged = new Dictionary<Polyline, HashSet<Polyline>>();
        Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
        Dictionary<Polyline, List<Point3d>> outline2ZeroPts = new Dictionary<Polyline, List<Point3d>>();
        double MaxBeamLength = 13000;
        double SimilarAngle = Math.PI / 8;

        public BorderConnectToNear(Point3dCollection _clumnPts
            , Dictionary<Polyline, HashSet<Point3d>> _outlineClumns, Dictionary<Polyline, Point3dCollection> _outlineNearPts
            , Dictionary<Polyline, HashSet<Polyline>> _outlineWallsMerged, Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> _outline2BorderNearPts
            , Dictionary<Polyline, List<Point3d>> _outline2ZeroPts, double _MaxBeamLength, double _SimilarAngle)
        {
            clumnPts = _clumnPts;
            outlineClumns = _outlineClumns;
            outlineNearPts = _outlineNearPts;
            outlineWallsMerged = _outlineWallsMerged;
            outline2BorderNearPts = _outline2BorderNearPts;
            outline2ZeroPts = _outline2ZeroPts;
            MaxBeamLength = _MaxBeamLength;
            SimilarAngle = _SimilarAngle;
        }

        /// <summary>
        /// 获取多边形边界和多边形外的连接
        /// </summary>
        public Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> BorderConnectToVDNear(ref List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples, ref Dictionary<Polyline, HashSet<Point3d>> outline2WallPts)
        {
            //1、获取近点
            VoronoiDiagramNearPoints(clumnPts, ref outlineNearPts);

            //2、获取“BorderPt与NearPt的连接”
            PriorityBorderPoints(outlineNearPts, outlineWallsMerged, outlineClumns, ref outline2BorderNearPts, ref outline2ZeroPts, ref priority1stBorderNearTuples, ref outline2WallPts, MaxBeamLength, SimilarAngle);

            return outline2BorderNearPts;
        }

        /// <summary>
        /// Get Near Points By VoronoiDiagram
        /// </summary>
        /// <param name="points"></param>
        /// <param name="poly2points"></param>
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

        /// <summary>
        /// 获取轮廓上与临近点相连的点组成的点对
        /// </summary>
        /// <param name="outlineNearPts">某轮廓和它的近点</param>
        /// <param name="outlineWalls">某轮廓和它的剪力墙</param>
        /// <param name="outlineClumns">某轮廓和它包含的柱点</param>
        /// <param name="outline2BorderNearPts">Input and Output</param>
        public static void PriorityBorderPoints(Dictionary<Polyline, Point3dCollection> outlineNearPts, Dictionary<Polyline, HashSet<Polyline>> outlineWalls,
            Dictionary<Polyline, HashSet<Point3d>> outlineClumns, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts,
            ref Dictionary<Polyline, List<Point3d>> outline2ZeroPts, ref List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples,
            ref Dictionary<Polyline, HashSet<Point3d>> outline2WallPts, double MaxBeamLength = 13000, double SimilarAngle = Math.PI / 8)
        {
            List<Point3d> fstPts = new List<Point3d>();
            List<Point3d> thdPts = new List<Point3d>();
            List<Point3d> tmpFstPts = new List<Point3d>();
            List<Point3d> tmpThdPts = new List<Point3d>();
            List<Point3d> outPts = new List<Point3d>();
            List<Point3d> corePts = new List<Point3d>();
            Polyline curOutline;
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
                        Point3d cntNearPt = GetObject.GetPointByDirection(borderPt, curOutline.GetClosePoint(borderPt), outlineNearPt.Value, Math.PI / 6, MaxBeamLength);
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
                        }
                        priority1stBorderNearTuples.Add(new Tuple<Point3d, Point3d>(borderPt, cntNearPt));
                    }
                }

                fstPts.Clear();
                thdPts.Clear();
                outPts.Clear();
                if (!outlineWalls.ContainsKey(curOutline))
                {
                    continue;
                }
                foreach (var wall in outlineWalls[curOutline])
                {
                    if (wall.Closed == false || wall.Area < 10000)
                    {
                        continue;
                    }
                    tmpFstPts.Clear();
                    tmpThdPts.Clear();
                    try
                    {
                        //CenterLine.WallEdgePoint(wall.DPSimplify(1).ToNTSPolygon(), 10, ref tmpFstPts, ref tmpThdPts);
                        PointsDealer.WallCrossPoint(wall.DPSimplify(1), ref tmpFstPts, ref tmpThdPts);
                    }
                    catch (Exception Ex) { }
                    fstPts.AddRange(tmpFstPts);
                    thdPts.AddRange(tmpThdPts);
                }
                outPts = PointsDealer.OutPoints(curOutline);
                
                PointsDealer.RemovePointsFarFromOutline(ref fstPts, curOutline);
                PointsDealer.RemovePointsFarFromOutline(ref thdPts, curOutline);
                //fstPts = PointsDealer.PointsDistinct(fstPts, 200);
                //thdPts = PointsDealer.PointsDistinct(thdPts, 200);
                fstPts = PointsCenters.PointsCores(fstPts, 350);
                thdPts = PointsCenters.PointsCores(thdPts, 350);

                if (!outline2WallPts.ContainsKey(curOutline))
                {
                    outline2WallPts.Add(curOutline, fstPts.ToHashSet());
                }
                corePts.AddRange(fstPts);

                outline2ZeroPts[curOutline].AddRange(fstPts);
                Point3d curBorderPt;
                foreach (Point3d nearPt in tmpNearPts)
                {
                    Point3d outlinePt = curOutline.GetClosePoint(nearPt);
                    Vector3d baseDirection = outlinePt - nearPt;
                    for (int i = 0; i < 4; ++i)
                    {
                        Vector3d aimDirection = baseDirection.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis).GetNormal();

                        //Get VerticalPoint
                        Point3d verticalPt = GetObject.GetClosestPointByDirection(nearPt, aimDirection, MaxBeamLength, curOutline);
                        if (verticalPt == nearPt || verticalPt.DistanceTo(nearPt) > MaxBeamLength)
                        {
                            continue;
                        }
                        //Get the line who contains the point will be connect
                        Line closetLine = GetObject.FindLineContainPoint(curOutline, verticalPt);
                        if (closetLine == null)
                        {
                            continue;
                        }
                        //找到近点nearPt最佳的边界连接点
                        if (i == 0)
                        {
                            curBorderPt = BestConnectPt(nearPt, verticalPt, fstPts, thdPts, outlineWalls[curOutline], closetLine, SimilarAngle * 2, MaxBeamLength);
                        }
                        else
                        {
                            curBorderPt = BestConnectPt(nearPt, verticalPt, fstPts, thdPts, outlineWalls[curOutline], closetLine, SimilarAngle, MaxBeamLength);
                        }
                        var disb2n = curBorderPt.DistanceTo(nearPt);
                        if (disb2n > MaxBeamLength || disb2n < 500 || curBorderPt.DistanceTo(curOutline.GetClosePoint(curBorderPt)) > 500)
                        {
                            continue;
                        }

                        if (!outline2BorderNearPts[curOutline].ContainsKey(curBorderPt))
                        {
                            outline2BorderNearPts[curOutline].Add(curBorderPt, new HashSet<Point3d>());
                        }
                        outline2BorderNearPts[curOutline][curBorderPt].Add(nearPt);
                    }
                }
            }
            //Merge very close points to one whithout change structure
            corePts = PointsDealer.PointsDistinct(corePts, 200);
            LineDealer.SimplifyLineConnect(outline2BorderNearPts, corePts);
        }

        /// <summary>
        /// Find Best Connect Point
        /// </summary>
        public static Point3d BestConnectPt(Point3d basePt, Point3d verticalPt, List<Point3d> fstPts, List<Point3d> thdPts,
            HashSet<Polyline> walls, Line closetLine, double toleranceDegree = Math.PI / 4, double MaxBeamLength = 13000)
        {
            double baseRadius = basePt.DistanceTo(verticalPt) / Math.Cos(toleranceDegree);
            baseRadius = baseRadius > MaxBeamLength ? MaxBeamLength : baseRadius;
            double curDis;
            Point3d tmpPt = verticalPt;
            double minDis = baseRadius;
            //1、Find the nearest Cross Point
            foreach (var fstPt in fstPts)
            {
                if (fstPt.DistanceTo(basePt) > baseRadius || fstPt.DistanceTo(closetLine.GetClosestPointTo(fstPt, false)) > 600)
                {
                    continue;
                }
                curDis = fstPt.DistanceTo(verticalPt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    tmpPt = fstPt;
                }
            }
            if (tmpPt != verticalPt && tmpPt.DistanceTo(verticalPt) < 4000)
            {
                return tmpPt;
            }

            //2、If there is a near wall, get vertical point on wall
            Circle circle = new Circle(verticalPt, Vector3d.ZAxis, 300);
            foreach (var wall in walls)
            {
                if (wall.Intersects(circle) || wall.Contains(verticalPt))
                {
                    return verticalPt;
                }
            }

            //3、Find apex point in range(45degree)
            minDis = baseRadius;
            foreach (var thdPt in thdPts)
            {
                if (thdPt.DistanceTo(basePt) > baseRadius || thdPt.DistanceTo(closetLine.GetClosestPointTo(thdPt, false)) > 600)
                {
                    continue;
                }
                curDis = thdPt.DistanceTo(verticalPt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    tmpPt = thdPt;
                }
            }
            if (tmpPt != verticalPt && tmpPt.DistanceTo(verticalPt) < 4000)
            {
                return tmpPt;
            }

            //4、Return the vertical point on outline
            //ShowInfo.ShowPointAsU(verticalPt, 7, 200);
            return verticalPt;
        }
    }
}
