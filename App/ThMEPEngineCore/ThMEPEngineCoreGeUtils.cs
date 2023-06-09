﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreGeUtils
    {
        /// <summary>
        /// 计算最大最小点
        /// </summary>
        /// <returns></returns>
        public static List<Point3d> CalBoundingBox(List<Point3d> points)
        {
            double maxX = points.Max(x => x.X);
            double minX = points.Min(x => x.X);
            double maxY = points.Max(x => x.Y);
            double minY = points.Min(x => x.Y);

            return new List<Point3d>(){
                new Point3d(maxX, maxY, 0), //p2(max point)
                new Point3d(maxX, minY, 0), //p3
                new Point3d(minX, maxY, 0), //p1
                new Point3d(minX, minY, 0), //p4(min point)
            };
        }

        /// <summary>
        /// 计算轴网坐标系
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Matrix3d GetGridMatrix(Vector3d xDir)
        {
            Vector3d longDir = xDir;
            Vector3d shortDir = Vector3d.ZAxis.CrossProduct(longDir);

            Matrix3d matrix = new Matrix3d(new double[]{
                    longDir.X, shortDir.X, Vector3d.ZAxis.X, 0,
                    longDir.Y, shortDir.Y, Vector3d.ZAxis.Y, 0,
                    longDir.Z, shortDir.Z, Vector3d.ZAxis.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            return matrix;
        }

        /// <summary>
        /// 创建polyline轴网线
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Polyline GetPolylineByPts(List<Point3d> pts)
        {
            Polyline poly = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                poly.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }
            return poly;
        }

        public static bool IsAisleArea(Geometry areaPolygon, double shrinkValue = 3600, double threshold = 0.1)
        {
            var originalArea = areaPolygon.Area;
            var shrinkedArea = areaPolygon.Buffer(-shrinkValue).Area;

            return shrinkedArea / originalArea < threshold;
        }

        /// <summary>
        /// 内缩外扩判断区域是不是走廊
        /// threshold:非走廊区域面积占全面积百分比
        /// </summary>
        /// <param name="areaPolygon"></param>
        /// <param name="shrinkValue"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool IsAisleBufferShrinkFrame(Geometry areaPolygon, double shrinkValue = 3600, double threshold = 0.1)
        {
            var isAisle = true;

            var originalArea = areaPolygon.Area;
            var shrinkedGeom = areaPolygon.Buffer(-shrinkValue);
            var shrinkedArea = shrinkedGeom.Area;
            //var objs2 = shrinkedGeom.ToDbCollection();
            //foreach (var obj in objs2)
            //{
            //    if (obj is Entity pl)
            //    {
            //        DrawUtils.ShowGeometry(pl, "l1shrink", 2);
            //    }
            //}

            var bufferShrink = shrinkedGeom.Buffer(shrinkValue);
            var bufferShrinkArea = bufferShrink.Area;
            //var objs3 = bufferShrink.ToDbCollection();
            //foreach (var obj in objs3)
            //{
            //    if (obj is Entity pl)
            //    {
            //        DrawUtils.ShowGeometry(pl, "l2bufferShrink", 3);
            //    }
            //}

            var shrinkAreaPer = shrinkedArea / originalArea;
            var bufferShrinkAreaPer = bufferShrinkArea / originalArea;

            if (bufferShrinkAreaPer >= threshold)
            {
                isAisle = false;
            }

            return isAisle;
        }
    }
}
