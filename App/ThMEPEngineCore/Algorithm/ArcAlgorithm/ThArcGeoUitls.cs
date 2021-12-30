using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.ArcAlgorithm
{
    public static class ThArcGeoUitls
    {
        /// <summary>
        /// 判断两个同心圆是否overlap
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="otherArc"></param>
        /// <param name="overLapTol"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool ArcOverLap(this Arc arc, Arc otherArc, double overLapTol, double tol = 1)
        {
            if (arc.Center.DistanceTo(otherArc.Center) > tol)   //非同心圆不做比较
            {
                return false;
            }
            if (Math.Abs(arc.Radius - otherArc.Radius) >= overLapTol)
            {
                return false;
            }

            var sProjectPt = arc.GetClosestPointTo(otherArc.StartPoint, false);
            var eProjectPt = arc.GetClosestPointTo(otherArc.EndPoint, false);
            if (sProjectPt.DistanceTo(eProjectPt) < tol)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 用同心圆的一个弧剪切另一个弧
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="otherArc"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Arc> CutArcLine(this Arc arc, Arc otherArc, double tol = 1)
        {
            if (arc.Center.DistanceTo(otherArc.Center) > tol)   //非同心圆无法剪切
            {
                return new List<Arc>() { otherArc };
            }

            var sAngle = arc.StartAngle < arc.EndAngle ? arc.StartAngle : arc.EndAngle;
            var eAngle = arc.StartAngle > arc.EndAngle ? arc.StartAngle : arc.EndAngle;
            var sOtherAngle = otherArc.StartAngle < otherArc.EndAngle ? otherArc.StartAngle : otherArc.EndAngle;
            var eOtherAngle = otherArc.StartAngle > otherArc.EndAngle ? otherArc.StartAngle : otherArc.EndAngle;
            if (eOtherAngle < sAngle || sOtherAngle > eAngle)
            {
                return new List<Arc>() { otherArc };
            }

            var usefulAngle = new List<double>();
            if (!(sAngle < sOtherAngle && sOtherAngle < eAngle))
            {
                usefulAngle.Add(sOtherAngle);
            }
            if (!(sAngle < eOtherAngle && eOtherAngle < eAngle))
            {
                usefulAngle.Add(eOtherAngle);
            }
            usefulAngle.Add(sAngle);
            usefulAngle.Add(eAngle);
            usefulAngle = usefulAngle.OrderBy(x => x).ToList();
            List<Arc> resArcs = new List<Arc>();
            for (int i = 1; i < usefulAngle.Count; i++)
            {
                if (!(Math.Abs(usefulAngle[i - 1] - sAngle) < 0.1 && Math.Abs(usefulAngle[i] - eAngle) < 0.1))
                {
                    var cutArc = new Arc(otherArc.Center, otherArc.Radius, usefulAngle[i - 1], usefulAngle[i]);
                    resArcs.Add(cutArc);
                }
            }

            return resArcs.Where(x => x.Length > tol).ToList();
        }

        /// <summary>
        /// 计算两个同心圆的弧的距离
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="otherArc"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static double ArcDistance(this Arc arc, Arc otherArc, double tol = 5)
        {
            if (arc.Center.DistanceTo(otherArc.Center) > tol)   //非同心圆暂不支持计算
            {
                throw new NotSupportedException();
            }

            double minAngle = arc.StartAngle < arc.EndAngle ? arc.StartAngle : arc.EndAngle;
            double maxAngle = arc.StartAngle > arc.EndAngle ? arc.StartAngle : arc.EndAngle;
            double minOtherAngle = otherArc.StartAngle < otherArc.EndAngle ? otherArc.StartAngle : otherArc.EndAngle;
            double maxOtherAngle = otherArc.StartAngle > otherArc.EndAngle ? otherArc.StartAngle : otherArc.EndAngle;
            if (minAngle > maxOtherAngle || maxAngle < minOtherAngle)
            {
                List<double> dis = new List<double>();
                dis.Add(arc.StartPoint.DistanceTo(otherArc.StartPoint));
                dis.Add(arc.StartPoint.DistanceTo(otherArc.EndPoint));
                dis.Add(arc.EndPoint.DistanceTo(otherArc.StartPoint));
                dis.Add(arc.EndPoint.DistanceTo(otherArc.EndPoint));

                return dis.OrderBy(x => x).First();
            }
            else
            {
                return Math.Abs(arc.Radius - otherArc.Radius);
            }
        }

        /// <summary>
        /// 去掉arc上多余的点
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Polyline ArcSimplify(this Polyline polyline)
        {
            var polylineSegments = new PolylineSegmentCollection(polyline);
            Polyline simplifyPoly = new Polyline() { Closed = true };
            Point3d? centerPt = null;
            Point3d prePt = Point3d.Origin;
            for (int i = 0; i < polylineSegments.Count; i++)
            {
                var segment = polylineSegments[i];
                double bulge = 0;
                if (!segment.IsLinear)
                {
                    var arcSegment = segment.ToCircularArc();
                    if (centerPt == null)
                    {
                        prePt = new Point3d(segment.StartPoint.X, segment.StartPoint.Y, 0);
                        centerPt = new Point3d(arcSegment.Center.X, arcSegment.Center.Y, 0);
                        continue;
                    }
                    else
                    {
                        if (new Point3d(arcSegment.Center.X, arcSegment.Center.Y, 0).DistanceTo(centerPt.Value) < 10)
                        {
                            continue;
                        }
                        else
                        {
                            var dir1 = (prePt - centerPt.Value).GetNormal();
                            var dir2 = (polyline.GetPoint3dAt(i % polyline.NumberOfVertices) - centerPt.Value).GetNormal();
                            var angle = Math.Abs(dir1.GetAngleTo(dir2));
                            bulge = Math.Tan(angle / 4);
                            if (angle > Math.PI)
                            {
                                bulge = -bulge;
                            }
                            centerPt = null;
                            simplifyPoly.AddVertexAt(simplifyPoly.NumberOfVertices, segment.StartPoint, bulge, 0, 0);
                        }
                    }
                }

                simplifyPoly.AddVertexAt(simplifyPoly.NumberOfVertices, segment.EndPoint, bulge, 0, 0);
            }

            return simplifyPoly;
        }
    }
}
