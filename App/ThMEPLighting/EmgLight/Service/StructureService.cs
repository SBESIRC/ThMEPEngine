using System;
using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.EmgLight.Model;

namespace ThMEPLighting.EmgLight.Service
{
    class StructureService
    {

        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="polys"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<ThStruct> GetStruct(List<ThStruct> structs, List<Line> lines, double tol)
        {

            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, tol, 0);
                return structs.Where(y =>
                {
                    return linePoly.Contains(y.geom) || linePoly.Intersects(y.geom);
                }).ToList();
            }).ToList();

            return resPolys;
        }

        public static List<List<ThStruct>> SeparateColumnsByLine(List<ThStruct> polyline, List<Line> lines, double tol)
        {
            //上下做框筛选框内构建. 不能直接transformToLine判断y值正负:打散很长的墙以后需要再筛选一遍
            var upPolyline = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, 0, 0);
                return polyline.Where(y =>
                {
                    return linePoly.Contains(y.geom) || linePoly.Intersects(y.geom);
                }).ToList();
            }).ToList();

            var downPolyline = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, 0, 0, tol, 0);
                return polyline.Where(y =>
                {
                    return linePoly.Contains(y.geom) || linePoly.Intersects(y.geom);
                }).ToList();
            }).ToList();


            return new List<List<ThStruct>>() { upPolyline, downPolyline };
        }

        /// <summary>
        /// 
        /// </summary>
        public static void removeDuplicateStruct(ref List<List<ThStruct>> structList)
        {
            foreach (var stru in structList)
            {
                for (int i = stru.Count - 1; i >= 0; i--)
                {

                    if (stru[i].geom.StartPoint == stru[i].geom.EndPoint)
                    {
                        stru.RemoveAt(i);
                    }
                }

                for (int i = 0; i < stru.Count; i++)
                {
                    var currS = stru[i].geom.StartPoint;
                    var currE = stru[i].geom.EndPoint;

                    for (int j = stru.Count - 1; j > i; j--)
                    {
                        var compareS = stru[j].geom.StartPoint;
                        var compareE = stru[j].geom.EndPoint;

                        if (currS == compareS && currE == compareE)
                        {
                            stru.RemoveAt(j);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="lines"></param>
        /// <param name="tol"></param>
        /// <returns></returns>


        /// <summary>
        /// 打散构建并生成数据结构
        /// </summary>
        /// <param name="structures"></param>
        /// <returns></returns>
        public static List<ThStruct> BrakePolylineToLineList(List<Polyline> structures)
        {
            List<ThStruct> structureSegment = new List<ThStruct>();
            foreach (var stru in structures)
            {
                for (int i = 0; i < stru.NumberOfVertices; i++)
                {
                    Polyline plTemp = new Polyline();
                    plTemp.AddVertexAt(0, stru.GetPoint2dAt(i), 0, 0, 0);
                    plTemp.AddVertexAt(0, stru.GetPoint2dAt((i + 1) % stru.NumberOfVertices), 0, 0, 0);
                    var structSeg = new ThStruct(plTemp, stru);
                    structureSegment.Add(structSeg);
                }
            }
            return structureSegment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="structSeg"></param>
        /// <param name="structure"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<ThStruct> FilterStructIntersect(List<ThStruct> structSeg, List<Polyline> structure, double tol)
        {
            List<ThStruct> notIntersectSeg = new List<ThStruct>();

            foreach (var seg in structSeg)
            {
                var bInter = false;
                var bContain = false;

                foreach (var poly in structure)
                {
                    bContain = bContain || poly.Contains(seg.geom);
                    bInter = poly.Intersects(seg.geom);
                    if (bInter == true)
                    {
                        Point3dCollection pts = new Point3dCollection();
                        seg.geom.IntersectWith(poly, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);

                        if (pts.Count > 1)
                        {
                            bInter = true;
                            break;
                        }
                        else if (pts.Count == 1)
                        {
                            Point3d ptIn = new Point3d();
                            Point3d ptOut = new Point3d();
                            if (poly.Contains(seg.geom.StartPoint) == true)
                            {
                                ptIn = seg.geom.StartPoint;
                                ptOut = seg.geom.EndPoint;
                            }
                            if (poly.Contains(seg.geom.EndPoint) == true)
                            {
                                ptIn = seg.geom.EndPoint;
                                ptOut = seg.geom.StartPoint;
                            }

                            var lIn = new Line(pts[0], ptIn);
                            var lOut = new Line(pts[0], ptOut);

                            if (ptIn.X == 0)
                            {
                                bInter = false;
                            }

                            else if (lIn.Length < EmgLightCommon.TolIntersect || lOut.Length >= EmgLightCommon.TolInterFilter)
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
                if (bInter == false && bContain == false)
                {
                    notIntersectSeg.Add(seg);
                }
            }
            return notIntersectSeg;
        }


        /// <summary>
        /// 找到柱与车道线平行且最近的边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <param name="layoutPt"></param>
        /// <returns></returns>
        //private static List<Polyline> GetColumnParallelPart(Polyline polyline, Point3d pt, Vector3d dir)
        //{
        //    var closetPt = polyline.GetClosestPointTo(pt, false);

        //    List<Polyline> structureSegment = new List<Polyline>();
        //    for (int i = 0; i < polyline.NumberOfVertices; i++)
        //    {
        //        Polyline plTemp = new Polyline();
        //        plTemp.AddVertexAt(0, polyline.GetPoint2dAt(i), 0, 0, 0);
        //        plTemp.AddVertexAt(0, polyline.GetPoint2dAt((i + 1) % polyline.NumberOfVertices), 0, 0, 0);
        //        structureSegment.Add(plTemp);
        //    }

        //    Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
        //    var structureLayoutSegment = structureSegment.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
        //        .Where(x =>
        //        {
        //            var xDir = (x.EndPoint - x.StartPoint).GetNormal();
        //            bool bAngle = Math.Abs(dir.DotProduct(xDir)) / (dir.Length * xDir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
        //            return bAngle;
        //        }).ToList();

        //    return structureLayoutSegment;
        //}

        /// <summary>
        /// 大于TolLight的墙拆分成TolLight(尾点不够和前面合并)
        /// </summary>
        /// <param name="walls"></param>
        /// <returns></returns>
        public static List<ThStruct> breakWall(List<ThStruct> walls, double TolLight)
        {
            List<ThStruct> returnWalls = new List<ThStruct>();

            foreach (var wall in walls)
            {
                Polyline restWall = wall.geom;
                bool doOnce = false;
                while (restWall.Length > TolLight)
                {
                    Point3d breakPt = restWall.GetPointAtDist(TolLight);
                    Polyline breakWall = new Polyline();
                    breakWall.AddVertexAt(breakWall.NumberOfVertices, restWall.StartPoint.ToPoint2D(), 0, 0, 0);
                    breakWall.AddVertexAt(breakWall.NumberOfVertices, breakPt.ToPoint2D(), 0, 0, 0);
                    ThStruct breakWallStruct = new ThStruct(breakWall, wall.oriStructGeo);
                    returnWalls.Add(breakWallStruct);

                    restWall.SetPointAt(0, breakPt.ToPoint2D());
                    doOnce = true;
                }

                if (doOnce == true && restWall.Length > 0)
                {
                    returnWalls.Last().geom.SetPointAt(returnWalls.Last().geom.NumberOfVertices - 1, restWall.EndPoint.ToPoint2D());
                }
                else
                {
                    returnWalls.Add(wall);
                }
            }

            return returnWalls;
        }

    }

}
