using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;

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
        public static List<SprayLayoutData> CalSprayPoint(List<List<Polyline>> tLines, List<List<Polyline>> vLines, Vector3d vDir, Vector3d tDir, double sideLength)
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
                                    if (IsIntersect(vLine, tLines[i - 1], out Polyline interEntity))
                                    {
                                        spray.prevTLine = interEntity;
                                    }
                                }
                                if (i < tLines.Count - 1)
                                {
                                    if (IsIntersect(vLine, tLines[i + 1], out Polyline interEntity))
                                    {
                                        spray.nextTLine = interEntity;
                                    }
                                }
                                if (j > 0)
                                {
                                    if (IsIntersect(tLine, vLines[j - 1], out Polyline interEntity))
                                    {
                                        spray.prevVLine = interEntity;
                                    }
                                }
                                if (j < vLines.Count - 1)
                                {
                                    if (IsIntersect(tLine, vLines[j + 1], out Polyline interEntity))
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
        /// 计算相交处布置点
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point3d> CalSprayPoint(List<Polyline> polylines, Polyline polyline)
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
        /// 获取所有同向排布线
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="sprays"></param>
        /// <returns></returns>
        public static List<Polyline> GetAllSanmeDirLines(Vector3d dir, List<SprayLayoutData> sprays)
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
        public static Polyline GetPolylineByDir(this SprayLayoutData sprayLayoutData, Vector3d dir)
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
        public static bool IsIntersect(Polyline entity, List<Polyline> otherEnts, out Polyline interEntity)
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
    }
}
