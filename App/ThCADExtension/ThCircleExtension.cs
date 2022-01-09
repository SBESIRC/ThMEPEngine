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
            return TessellateCircleWithArc(circle, ChordLength2ArcLength(length, circle.Radius));
        }

        public static double ChordLength2ArcLength(double chordLength, double radius)
        {
            // Circle - Arc Length from Chord Length and Radius
            // https://www.vcalc.com/equation/?uuid=7d9b22c3-5fe3-11ea-a7e4-bc764e203090
            return 2 * radius * Math.Asin(chordLength / (2 * radius));
        }

        public static double ArcLength2ChordLength(double chordLength, double radius)
        {
            // Circle - Chord Length from Arc Length and Radius
            // https://www.vcalc.com/wiki/vCalc/Circle+-+Chord+Length+from+Arc+Length+and+Radius
            return 2 * radius * Math.Sin(chordLength / (2 * radius));
        }

        public static double ChordLength2ChordHeight(double chordLength, double radius)
        {
            // Circle - Radius from chord length and arc height
            // https://www.vcalc.com/equation/?uuid=79418cb6-a6b2-11e6-9770-bc764e2038f2
            return radius - Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(chordLength / 2.0, 2));
        }
    }
}
