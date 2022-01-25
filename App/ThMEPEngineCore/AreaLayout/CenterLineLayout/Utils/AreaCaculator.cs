using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Operation.Overlay;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.GridLayout.Method;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.Utils
{
    public static class AreaCaculator
    {
        /// <summary>
        /// 计算不可视区域集合
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry UnVisibleArea(MPolygon mPolygon, List<Point3d> points, double radius)
        {
            if (points.Count == 0)
            {
                return mPolygon.ToNTSPolygon();
            }
            //计算过点集覆盖的真实面积
            List<Polygon> Detect = new List<Polygon>();
            foreach (Point3d pt in points)
            {
                Detect.Add(DetectCalculator.CalculateDetect(new Coordinate(pt.X, pt.Y), mPolygon.ToNTSPolygon(), radius, true));
            }
            //计算光照盲区
            NetTopologySuite.Geometries.Geometry blind = mPolygon.ToNTSPolygon().Difference(OverlayNGRobust.Union(Detect.ToArray()));
            return blind;
        }

        /// <summary>
        /// 计算不可覆盖区域集合
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private static NetTopologySuite.Geometries.Geometry UnCoverArea(MPolygon mPolygon, List<Point3d> points, double radius)
        {
            if (points.Count == 0)
            {
                return mPolygon.ToNTSPolygon();
            }
            List<Polygon> Detect = new List<Polygon>();
            foreach (Point3d pt in points)
            {
                Detect.Add((new Circle(pt, Vector3d.ZAxis, radius)).ToNTSPolygon());
            }
            //using (Linq2Acad.AcadDatabase acdb = Linq2Acad.AcadDatabase.Active())
            //{
            //    ShowInfo.ShowGeometry(mPolygon.ToNTSGeometry(),acdb ,1);
            //}
            return mPolygon.ToNTSPolygon().Difference(OverlayNGRobust.Union(Detect.ToArray()));
        }

        /// <summary>
        /// 根据探测类型计算盲区
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <param name="equipmentType"></param>
        /// <returns></returns>
        //public static NetTopologySuite.Geometries.Geometry BlandArea(MPolygon mPolygon, List<Point3d> points, double radius, BlindType equipmentType)
        //{
        //    if (equipmentType == BlindType.VisibleArea)
        //    {
        //        return UnVisibleArea(mPolygon, points, radius);
        //    }
        //    else
        //    {
        //        return UnCoverArea(mPolygon, points, radius);
        //    }
        //}

        public static Geometry BlandArea(MPolygon mPolygon, List<Point3d> points, double radius, BlindType equipmentType, ThCADCoreNTSSpatialIndex detectSpatialIdx, Geometry EmptyDetect)
        {
            var detect = new List<Polygon>();
            var room = mPolygon.ToNTSPolygon();
            var isVisible = equipmentType == BlindType.VisibleArea ? true : false;
            foreach (var p in points)
            {
               
                Polygon detectHasPt = detectSpatialIdx != null ? GetDetect(p, detectSpatialIdx) : null;
                //能探测到中心点的布置区域
                Polygon d = DetectCalculator.CalculateDetect(new Coordinate(p.X, p.Y), (detectHasPt != null ? detectHasPt : room), radius, isVisible);
                if (d != null)
                {
                    detect.Add(d);
                }
          
            }

            var poly = OverlayNGRobust.Union(detect.ToArray());
            var blind = OverlayNGRobust.Overlay(room, EmptyDetect, SpatialFunction.Difference);
            blind = OverlayNGRobust.Overlay(blind, poly, SpatialFunction.Difference);

            return blind;
        }

        private static Polygon GetDetect(Point3d point, ThCADCoreNTSSpatialIndex detectSpatialIdx)
        {
            // 计算包含该点的可布置区域
            Polygon ans = null;
            var min = new Point3d(point.X - 1, point.Y - 1, 0);
            var max = new Point3d(point.X + 1, point.Y + 1, 0);

            var crossDetect = detectSpatialIdx.SelectCrossingWindow(min, max).OfType<MPolygon>();
            if (crossDetect.Count() > 0)
            {
                ans = crossDetect.First().ToNTSPolygon();
            }

            return ans;
        }

        public static Polyline GetDetectPolyline(Point3d point, ThCADCoreNTSSpatialIndex detectSpatialIdx)
        {
            //计算包含该点的可布置区域
            var min = new Point3d(point.X - 1, point.Y - 1, 0);
            var max = new Point3d(point.X + 1, point.Y + 1, 0);

            var returnPl = new Polyline();

            var tempPl = detectSpatialIdx.SelectCrossingWindow(min, max).Cast<MPolygon>();
            if (tempPl.Count() > 0)
            {
                returnPl = tempPl.First().Shell();
            }
            return returnPl;
        }
    }
}
