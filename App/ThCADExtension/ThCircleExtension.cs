using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThCircleExtension
    {
        /// <summary>
        /// 用弧长分割圆
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline TessellateCircleWithArc(this Circle circle, double length)
        {
            var points = new Point3dCollection();
            int pointNums = (int)Math.Ceiling(circle.Circumference / length);
            for (int i = 0; i < pointNums; i++)
            {
                double ang = i * (2 * Math.PI / pointNums);
                points.Add(new Point3d(circle.Radius * Math.Cos(ang), circle.Radius * Math.Sin(ang), 0));
            }
            var poly = new Polyline()
            {
                Closed = true,
            };
            poly.CreatePolyline(points);
            poly.TransformBy(Matrix3d.Displacement(circle.Center.GetAsVector()));
            return poly;
        }

        /// <summary>
        /// 用弦长分割圆
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline TessellateCircleWithChord(this Circle circle, double length)
        {
            return TessellateCircleWithArc(circle, circle.ChordLengthToArcLength(length));
        }

        /// <summary>
        /// 根据弦长求解对应较短弧的弧长
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static double ChordLengthToArcLength(this Circle circle, double length)
        {
            if(length > circle.Diameter)
            {
                throw new ArgumentOutOfRangeException();
            }
            return 2 * circle.Radius * Math.Asin(length / circle.Diameter);
        }
    }
}
