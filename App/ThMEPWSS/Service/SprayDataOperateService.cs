﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;
using ThMEPWSS.Utils;

namespace ThMEPWSS.Service
{
    public static class SprayDataOperateService
    {
        /// <summary>
        /// 计算喷淋布置点
        /// </summary>
        /// <param name="tLines"></param>
        /// <param name="vLines"></param>
        /// <param name="vDir"></param>
        /// <param name="tDir"></param>
        /// <returns></returns>
        public static List<SprayLayoutData> CalSprayPoint(List<List<Line>> tLines, List<List<Line>> vLines, Vector3d vDir, Vector3d tDir, double sideLength)
        {
            List<SprayLayoutData> layoutPts = new List<SprayLayoutData>();
            for (int i = 0; i < tLines.Count; i++)
            {
                foreach (var tLine in tLines[i])
                {
                    for (int j = 0; j < vLines.Count; j++)
                    {
                        foreach (var vLine in vLines[j])
                        {
                            Point3dCollection points = new Point3dCollection();
                            tLine.IntersectWith(vLine, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                            if (points.Count > 0)
                            {
                                var spray = SprayDataService.CreateSprayModelsByRay(points.Cast<Point3d>().First(), vDir, tDir, sideLength);
                                spray.vLine = vLine;
                                spray.tLine = tLine;

                                Point3dCollection testPts = new Point3dCollection();
                                if (i > 0)
                                {
                                    if (IsIntersect(vLine, tLines[i - 1], out Line interEntity))
                                    {
                                        spray.prevTLine = interEntity;
                                    }
                                }
                                if (i < tLines.Count - 1)
                                {
                                    if (IsIntersect(vLine, tLines[i + 1], out Line interEntity))
                                    {
                                        spray.nextTLine = interEntity;
                                    }
                                }
                                if (j > 0)
                                {
                                    if (IsIntersect(tLine, vLines[j - 1], out Line interEntity))
                                    {
                                        spray.prevVLine = interEntity;
                                    }
                                }
                                if (j < vLines.Count - 1)
                                {
                                    if (IsIntersect(tLine, vLines[j + 1], out Line interEntity))
                                    {
                                        spray.nextVLine = interEntity;
                                    }
                                }
                                
                                layoutPts.Add(spray);
                            }
                        }
                    }
                }
            }

            return layoutPts;
        }

        /// <summary>
        /// 计算喷淋布置点(只有原点信息)
        /// </summary>
        /// <param name="tLines"></param>
        /// <param name="vLines"></param>
        /// <param name="vDir"></param>
        /// <param name="tDir"></param>
        /// <returns></returns>
        public static List<SprayLayoutData> CalSprayPoint(List<Line> tLines, List<Line> vLines)
        {
            List<SprayLayoutData> layoutPts = new List<SprayLayoutData>();
            foreach (var tLine in tLines)
            {
                foreach (var vLine in vLines)
                {
                    Point3dCollection points = new Point3dCollection();
                    tLine.IntersectWith(vLine, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                    layoutPts.AddRange(SprayDataService.CreateSprayModels(points.Cast<Point3d>().ToList()));
                }
            }

            return layoutPts;
        }

        /// <summary>
        /// 计算喷淋布置点
        /// </summary>
        /// <param name="sprayPts"></param>
        /// <param name="vDir"></param>
        /// <param name="tDir"></param>
        /// <param name="sideLength"></param>
        /// <returns></returns>
        public static List<SprayLayoutData> CalSprayPoint(List<Point3d> sprayPts, Vector3d vDir, Vector3d tDir, double sideLength)
        {
            List<SprayLayoutData> layoutPts = new List<SprayLayoutData>();
            foreach (var spray in sprayPts)
            {
                var sprayData = SprayDataService.CreateSprayModelsByRay(spray, vDir, tDir, sideLength);
                layoutPts.Add(sprayData);
            }

            return layoutPts;
        }

        /// <summary>
        /// 计算相交处布置点
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point3d> CalSprayPoint(List<Line> polylines, Line polyline)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (var poly in polylines)
            {
                Point3dCollection points = new Point3dCollection();
                poly.IntersectWith(polyline, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                pts.AddRange(points.Cast<Point3d>());
            }

            return pts;
        }

        /// <summary>
        /// 计算相交处布置点
        /// </summary>
        /// <param name="vLine"></param>
        /// <param name="tLine"></param>
        /// <returns></returns>
        public static Point3d CalSprayPoint(Polyline vLine, Polyline tLine)
        {
            Point3dCollection points = new Point3dCollection();
            vLine.IntersectWith(tLine, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
            if (points.Count <= 0)
            {
                //return null;
            }
            return points[0];
        }

        /// <summary>
        /// 获取所有同向排布线
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="sprays"></param>
        /// <returns></returns>
        public static List<Line> GetAllSanmeDirLines(Vector3d dir, List<SprayLayoutData> sprays)
        {
            if (sprays.Count <= 0)
            {
                return null;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
            }

            if (sprays.First().JudgeMainLine(dir))
            {
                return sprays.Select(x => x.vLine).ToList();
            }
            else
            {
                return sprays.Select(x => x.tLine).ToList();
            }
        }

        /// <summary>
        /// 找到当前线
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Line GetPolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            if (sprayLayoutData.JudgeMainLine(dir))
            {
                return sprayLayoutData.vLine;
            }
            else
            {
                return sprayLayoutData.tLine;
            }
        }

        /// <summary>
        /// 找到不同向线
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Line GetOtherPolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            if (sprayLayoutData.JudgeMainLine(dir))
            {
                return sprayLayoutData.tLine;
            }
            else
            {
                return sprayLayoutData.vLine;
            }
        }

        /// <summary>
        /// 找到此方向线前一根线
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Line GetPrePolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            if (sprayLayoutData.JudgeMainLine(dir))
            {
                return sprayLayoutData.prevVLine;
            }
            else
            {
                return sprayLayoutData.prevTLine;
            }
        }

        /// <summary>
        /// 找到其他方向线前一根线
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Line GetOtherPrePolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            if (sprayLayoutData.JudgeMainLine(dir))
            {
                return sprayLayoutData.prevTLine;
            }
            else
            {
                return sprayLayoutData.prevVLine;
            }
        }

        /// <summary>
        /// 找到此方向后一根线
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Line GetNextPolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            if (sprayLayoutData.JudgeMainLine(dir))
            {
                return sprayLayoutData.nextVLine;
            }
            else
            {
                return sprayLayoutData.nextTLine;
            }
        }

        /// <summary>
        /// 找到其他方向后一根线
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Line GetOtherNextPolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            if (sprayLayoutData.JudgeMainLine(dir))
            {
                return sprayLayoutData.nextTLine;
            }
            else
            {
                return sprayLayoutData.nextVLine;
            }
        }

        /// <summary>
        /// 判断是vLine还是tLine（true：vLine，false：tLine）
        /// </summary>
        /// <param name="sprayLayoutData"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static bool JudgeMainLine(this SprayLayoutData sprayLayoutData, Vector3d dir)
        {
            var vDir = (sprayLayoutData.vLine.EndPoint - sprayLayoutData.vLine.StartPoint).GetNormal();
            var tDir = (sprayLayoutData.tLine.EndPoint - sprayLayoutData.tLine.StartPoint).GetNormal();
            if (Math.Abs(vDir.DotProduct(dir)) > Math.Abs(tDir.DotProduct(dir)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否相交
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="otherEnts"></param>
        /// <param name="interEntity"></param>
        /// <returns></returns>
        public static bool IsIntersect(Line entity, List<Line> otherEnts, out Line interEntity)
        {
            interEntity = null;
            Point3dCollection testPts = new Point3dCollection();
            foreach (var ent in otherEnts)
            {
                entity.IntersectWith(ent, Intersect.OnBothOperands, testPts, IntPtr.Zero, IntPtr.Zero);
                if (testPts.Count > 0)
                {
                    interEntity = ent;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 更新喷淋点的信息
        /// </summary>
        /// <param name="sprays"></param>
        /// <param name="originLine"></param>
        /// <param name="newLine"></param>
        public static void UpdateSpraysLine(List<SprayLayoutData> sprays, Line originLine, Line newLine)
        {
            var resSprays = sprays.Where(x => x.tLine.IsSameLine(originLine) || x.vLine.IsSameLine(originLine)).ToList();
            var resPreSprays = sprays.Where(x => x.prevTLine.IsSameLine(originLine) || x.prevVLine.IsSameLine(originLine)).ToList();
            var resNextSprays = sprays.Where(x => x.nextTLine.IsSameLine(originLine) || x.nextVLine.IsSameLine(originLine)).ToList();
            foreach (var spray in resSprays)
            {
                Point3dCollection points = new Point3dCollection();
                if (spray.vLine.IsSameLine(originLine))
                {
                    var preSpray = resPreSprays.Where(x => x.tLine.IsSameLine(spray.tLine)).FirstOrDefault();
                    var nextSpray = resNextSprays.Where(x => x.tLine.IsSameLine(spray.tLine)).FirstOrDefault();
                    spray.tLine.IntersectWith(originLine, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                    if (points.Count > 0)
                    {
                        spray.vLine = newLine;
                        if (preSpray != null)
                        {
                            preSpray.prevVLine = newLine;
                        }

                        if (nextSpray != null)
                        {
                            nextSpray.nextVLine = newLine;
                        }
                    }
                }

                if (spray.tLine.IsSameLine(originLine))
                {
                    points.Clear();
                    var preSpray = resPreSprays.Where(x => x.vLine.IsSameLine(spray.vLine)).FirstOrDefault();
                    var nextSpray = resNextSprays.Where(x => x.vLine.IsSameLine(spray.vLine)).FirstOrDefault();
                    spray.vLine.IntersectWith(originLine, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                    if (points.Count > 0)
                    {
                        spray.tLine = newLine;
                        if (preSpray != null)
                        {
                            preSpray.prevTLine = newLine;
                        }

                        if (nextSpray != null)
                        {
                            nextSpray.nextTLine = newLine;
                        }
                    }
                }

                points.Clear();
                spray.tLine.IntersectWith(spray.vLine, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                if (points.Count > 0)
                {
                    spray.Position = points[0];
                }
            }
        }

        /// <summary>
        /// 获取所有喷淋线
        /// </summary>
        /// <param name="sprays"></param>
        /// <returns></returns>
        public static List<Line> CalAllSprayLines(List<SprayLayoutData> sprays)
        {
            return sprays.SelectMany(x => new List<Line>() { x.vLine, x.tLine }).Distinct().ToList();
        }

        /// <summary>
        /// 获取该喷淋前后左右四个喷淋
        /// </summary>
        /// <param name="spray"></param>
        /// <param name="allSprays"></param>
        /// <returns></returns>
        public static List<SprayLayoutData> GetAroundSprays(this SprayLayoutData spray, List<SprayLayoutData> allSprays)
        {
            List<SprayLayoutData> aroundSprays = new List<SprayLayoutData>();
            aroundSprays.Add(allSprays.FirstOrDefault(x => x.vLine.IsSameLine(spray.prevVLine) && x.tLine.IsSameLine(spray.tLine)));
            aroundSprays.Add(allSprays.FirstOrDefault(x => x.vLine.IsSameLine(spray.nextVLine) && x.tLine.IsSameLine(spray.tLine)));
            aroundSprays.Add(allSprays.FirstOrDefault(x => x.tLine.IsSameLine(spray.prevTLine) && x.vLine.IsSameLine(spray.vLine)));
            aroundSprays.Add(allSprays.FirstOrDefault(x => x.tLine.IsSameLine(spray.nextTLine) && x.vLine.IsSameLine(spray.vLine)));

            return aroundSprays.Where(x => x != null).ToList();
        }
    }
}

