using System;
using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;

namespace ThMEPLighting.EmgLight.Service
{
    class StructureServiceLight
    {

        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="polys"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys, double tol)
        {
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, tol, 0);
                return polys.Where(y =>
                {
                    var polyCollection = new DBObjectCollection() { y };
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            return resPolys;
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="lines"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, List<Line> lines, double tol)
        {
            //上下做框筛选框内构建. 不能直接transformToLine判断y值正负:打散很长的墙以后需要再筛选一遍
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, 0, 0);
                return polyline.Where(y =>
                {
                    //var polyCollection = new DBObjectCollection() { y };
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            var upPolyline = resPolys;

            resPolys = lines.SelectMany(x =>
           {
               var linePoly = StructUtils.ExpandLine(x, 0, 0, tol, 0);
               return polyline.Where(y =>
              {
                  //var polyCollection = new DBObjectCollection() { y };
                  return linePoly.Contains(y) || linePoly.Intersects(y);
              }).ToList();
           }).ToList();
            var downPolyline = resPolys;

            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }

        /// <summary>
        /// 查找柱或墙平行于车道线
        /// </summary>
        /// <param name="structrues"></param>
        /// <param name="line"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Polyline> getStructureParallelPart(List<Polyline> structureSegment, List<Line> lane)
        {
            List<Polyline> layoutColumns = new List<Polyline>();

            var LineDir = (lane.Last().EndPoint - lane.First().StartPoint).GetNormal();

            //平行于车道线的边
            var layoutInfo = GetWallParallelPart(structureSegment, LineDir);

            if (layoutInfo != null && layoutInfo.Count > 0)
            {
                layoutColumns.AddRange(layoutInfo);
            }

            return layoutColumns;
        }

        public static List<Polyline> BrakePolylineToLineList(List<Polyline> structures)
        {
            List<Polyline> structureSegment = new List<Polyline>();
            foreach (var stru in structures)
            {
                for (int i = 0; i < stru.NumberOfVertices; i++)
                {
                    Polyline plTemp = new Polyline();
                    plTemp.AddVertexAt(0, stru.GetPoint2dAt(i), 0, 0, 0);
                    plTemp.AddVertexAt(0, stru.GetPoint2dAt((i + 1) % stru.NumberOfVertices), 0, 0, 0);
                    structureSegment.Add(plTemp);
                }
            }

            return structureSegment;
        }

        public static List<Polyline> FilterStructIntersect(List<Polyline> structSeg, List<Polyline> structure, double tol)
        {
            List<Polyline> notIntersectSeg = new List<Polyline>();

            foreach (var seg in structSeg)
            {
                var bInter = false;
                var bContain = false;

                foreach (var poly in structure)
                {
                    bContain = bContain || poly.Contains(seg);
                    bInter = poly.Intersects(seg);
                    if (bInter == true)
                    {
                        Point3dCollection pts = new Point3dCollection();
                        seg.IntersectWith(poly, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);

                        if (pts.Count > 1)
                        {
                            bInter = true;
                            break;
                        }
                        else if (pts.Count == 1)
                        {
                            Point3d pt = new Point3d();
                            if (poly.Contains(seg.StartPoint) == true)
                            {
                                pt = seg.StartPoint;
                            }
                            if (poly.Contains(seg.EndPoint) == true)
                            {
                                pt = seg.EndPoint;
                            }

                            var l = new Line(pts[0], pt);

                            if (pt.X == 0)
                            {
                                bInter = false;
                            }

                            else if (l.Length < EmgLightCommon.TolIntersect)
                            {
                                bInter = false;
                            }
                            else
                            {
                                bInter = true;
                                break;
                            }
                        }
                        else
                        {
                            bInter = false;
                        }
                    }
                }
                if (!bInter && !bContain)
                {
                    notIntersectSeg.Add(seg);
                }
            }
            return notIntersectSeg;
        }

        /// <summary>
        /// 找到墙与车道线平行的边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <param name="layoutPt"></param>
        /// <returns></returns>
        private static List<Polyline> GetWallParallelPart(List<Polyline> structureSegment, Vector3d dir)
        {
            dir = dir.GetNormal();
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);

            var structureLayoutSegment = structureSegment.Where(x =>
            {
                var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                bool bAngle = Math.Abs(dir.DotProduct(xDir)) / (dir.Length * xDir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                return bAngle;
            }).ToList();

            return structureLayoutSegment;
        }

        /// <summary>
        /// 找到柱与车道线平行且最近的边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <param name="layoutPt"></param>
        /// <returns></returns>
        private static List<Polyline> GetColumnParallelPart(Polyline polyline, Point3d pt, Vector3d dir)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);

            List<Polyline> structureSegment = new List<Polyline>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Polyline plTemp = new Polyline();
                plTemp.AddVertexAt(0, polyline.GetPoint2dAt(i), 0, 0, 0);
                plTemp.AddVertexAt(0, polyline.GetPoint2dAt((i + 1) % polyline.NumberOfVertices), 0, 0, 0);
                structureSegment.Add(plTemp);
            }

            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var structureLayoutSegment = structureSegment.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
                .Where(x =>
                {
                    var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                    bool bAngle = Math.Abs(dir.DotProduct(xDir)) / (dir.Length * xDir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                    return bAngle;
                }).ToList();

            return structureLayoutSegment;
        }

        /// <summary>
        /// 大于TolLight的墙拆分成TolLight(尾点不够和前面合并)
        /// </summary>
        /// <param name="walls"></param>
        /// <returns></returns>
        public static List<Polyline> breakWall(List<Polyline> walls, double TolLight)
        {
            List<Polyline> returnWalls = new List<Polyline>();

            foreach (var wall in walls)
            {
                Polyline restWall = wall;
                bool doOnce = false;
                while (restWall.Length > TolLight)
                {
                    Point3d breakPt = restWall.GetPointAtDist(TolLight);
                    Polyline breakWall = new Polyline();
                    breakWall.AddVertexAt(breakWall.NumberOfVertices, restWall.StartPoint.ToPoint2D(), 0, 0, 0);
                    breakWall.AddVertexAt(breakWall.NumberOfVertices, breakPt.ToPoint2D(), 0, 0, 0);
                    returnWalls.Add(breakWall);

                    restWall.SetPointAt(0, breakPt.ToPoint2D());
                    doOnce = true;
                }

                if (doOnce == true && restWall.Length > 0)
                {
                    returnWalls.Last().SetPointAt(returnWalls.Last().NumberOfVertices - 1, restWall.EndPoint.ToPoint2D());
                }
                else
                {
                    returnWalls.Add(wall);
                }
            }

            return returnWalls;
        }

        /// <summary>
        /// 车道线往前做框buffer
        /// </summary>
        /// <param name="Lines"></param>
        /// <returns></returns>
        public static List<Line> LaneHeadExtend(List<Line> Lines, double tol)
        {
            var moveDir = (Lines[0].EndPoint - Lines[0].StartPoint).GetNormal();
            var ExtendLineStart = Lines[0].StartPoint - moveDir * tol;
            var ExtendLineEnd = Lines[0].StartPoint + moveDir * tol;
            var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
            var ExtendLineList = new List<Line>();
            ExtendLineList.Add(ExtendLine);

            return ExtendLineList;
        }

    }

}
