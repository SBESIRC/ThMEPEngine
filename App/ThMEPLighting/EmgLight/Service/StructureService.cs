using System;
using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.EmgLight.Model;
using ThMEPLighting.EmgLight.Common;

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
                var linePoly = GeomUtils.ExpandLine(x, tol, 0, tol, 0);
                return structs.Where(y =>
                {
                    return linePoly.Contains(y.geom) || linePoly.Intersects(y.geom);
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
        public static List<List<ThStruct>> SeparateStructByLine(List<ThStruct> polyline, List<Line> lines, double tol)
        {
            //上下做框筛选框内构建. 不能直接transformToLine判断y值正负:打散很长的墙以后需要再筛选一遍
            var upPolyline = lines.SelectMany(x =>
            {
                var linePoly = GeomUtils.ExpandLine(x, tol, 0, 0, 0);
                return polyline.Where(y =>
                {
                    return linePoly.Contains(y.geom) || linePoly.Intersects(y.geom);
                }).ToList();
            }).ToList();

            var downPolyline = lines.SelectMany(x =>
            {
                var linePoly = GeomUtils.ExpandLine(x, 0, 0, tol, 0);
                return polyline.Where(y =>
                {
                    return linePoly.Contains(y.geom) || linePoly.Intersects(y.geom);
                }).ToList();
            }).ToList();

            var usefulStruct = new List<List<ThStruct>>() { upPolyline, downPolyline };

            return usefulStruct;
        }

        /// <summary>
        /// 清理一个点和重复的项目
        /// </summary>
        /// <param name="structList"></param>
        public static void removeDuplicateStruct( List<List<ThStruct>> structList)
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
        /// 打散构建并生成数据结构
        /// </summary>
        /// <param name="structures"></param>
        /// <returns></returns>
        public static List<ThStruct> BrakeStructToStructSeg(List<Polyline> structures)
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
        /// 大于TolLight的墙拆分成TolLight(尾点不够和前面合并)
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="TolLight"></param>
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

        /// <summary>
        /// 找ExtendPoly框内且离Pt点最近的构建
        /// </summary>
        /// <param name="structList"></param>
        /// <param name="Pt"></param>
        /// <param name="ExtendPoly"></param>
        /// <param name="closestStruct"></param>
        /// <returns></returns>
        public static bool FindClosestStructToPt(List<ThStruct> structList, Point3d Pt, Polyline ExtendPoly, out ThStruct closestStruct)
        {
            bool bReturn = false;
            closestStruct = null;

            FindPolyInExtendPoly(structList, ExtendPoly, out var inExtendStruct);
            if (inExtendStruct.Count > 0)
            {
                //框内有位置布灯
                findClosestStruct(inExtendStruct, Pt, out closestStruct);
                bReturn = true;
            }
            else
            {
                bReturn = false;
            }
            return bReturn;
        }

        /// <summary>
        /// 找距离pt最近的构建
        /// </summary>
        /// <param name="structList"></param>
        /// <param name="Pt"></param>
        /// <param name="closestStruct"></param>
        private static void findClosestStruct(List<ThStruct> structList, Point3d Pt, out ThStruct closestStruct)
        {
            double minDist = 10000;
            closestStruct = null;

            for (int i = 0; i < structList.Count; i++)
            {
                if (structList[i].geom.Distance(Pt) <= minDist)
                {
                    minDist = structList[i].geom.Distance(Pt);
                    closestStruct = structList[i];
                }
            }
        }

        /// <summary>
        /// 判断框内是否有构建
        /// </summary>
        /// <param name="structList"></param>
        /// <param name="ExtendPoly"></param>
        /// <param name="inExtendStruct"></param>
        private static void FindPolyInExtendPoly(List<ThStruct> structList, Polyline ExtendPoly, out List<ThStruct> inExtendStruct)
        {
            inExtendStruct = structList.Where(x =>
            {
                return (ExtendPoly.Contains(x.geom) || ExtendPoly.Intersects(x.geom));
            }).ToList();

        }

        /// <summary>
        /// 检查构建是否在车道线头部附近,防止布点到车道线头的墙
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="laneList"></param>
        /// <returns></returns>
        public static bool CheckIfInLaneHead(ThStruct structure, List<ThLane> laneList)
        {
            bool bReturn = false;
            if (structure != null)
            {
                foreach (var l in laneList)
                {
                    if (l.headProtectPoly.Contains(structure.geom) || l.headProtectPoly.Intersects(structure.geom) ||
                      l.endProtectPoly.Contains(structure.geom) || l.endProtectPoly.Intersects(structure.geom))
                    {
                        bReturn = true;
                        break;
                    }
                }
            }

            return bReturn;
        }

        /// <summary>
        /// layout到structure TolRangeMin以内找离structure最近的,没有返回null
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="structure"></param>
        /// <param name="Tol"></param>
        /// <returns></returns>
        public static ThStruct CheckIfInLayout(ThStruct structure, List<ThStruct> layoutList, double Tol)
        {
            double minDist = Tol + 1;
            ThStruct closestLayout = null;

            if (structure != null)
            {
                for (int i = 0; i < layoutList.Count; i++)
                {
                    var dist = layoutList[i].geom.StartPoint.DistanceTo(structure.geom.StartPoint);
                    if (dist <= minDist && dist < Tol)
                    {
                        minDist = dist;
                        closestLayout = layoutList[i];
                    }
                }
            }
            return closestLayout;
        }



    }

}
