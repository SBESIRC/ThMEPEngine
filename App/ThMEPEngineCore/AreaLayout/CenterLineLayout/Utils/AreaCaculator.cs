using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using ThCADCore.NTS;
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
        public static NetTopologySuite.Geometries.Geometry UnVisibleArea(MPolygon mPolygon, List<Point3d> points, double radius)
        {
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
        public static NetTopologySuite.Geometries.Geometry UnCoverArea(MPolygon mPolygon, List<Point3d> points, double radius)
        {
            List<Polygon> Detect = new List<Polygon>();
            foreach (Point3d pt in points)
            {
                Detect.Add((new Circle(pt, Vector3d.ZAxis, radius)).ToNTSPolygon());
            }
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
        public static NetTopologySuite.Geometries.Geometry BlandArea(MPolygon mPolygon, List<Point3d> points, double radius, BlindType equipmentType)
        {
            if (equipmentType == BlindType.VisibleArea)
            {
                return UnVisibleArea(mPolygon, points, radius);
            }
            else
            {
                return UnCoverArea(mPolygon, points, radius);
            }
        }
    }
}
