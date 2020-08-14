﻿using System;
using System.Collections.Generic;
using System.Linq;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
 
namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    /// <summary>
    /// Provides the Offset() extension method for the Polyline type
    /// </summary>
    public static class ThPolylineExtension
    {
        /// <summary>
        /// Enumeration of offset side options
        /// </summary>
        public enum OffsetSide
        {
            In, Out, Left, Right, Both
        }

        /// <summary>
        /// Offset the source polyline to specified side(s).
        /// </summary>
        /// <param name="source">The polyline to be offseted.</param>
        /// <param name="offsetDist">The offset distance.</param>
        /// <param name="side">The offset side(s).</param>
        /// <returns>A polyline sequence resulting from the offset of the source polyline.</returns>
        public static IEnumerable<Polyline> Offset(this Polyline source, double offsetDist, OffsetSide side)
        {
            offsetDist = Math.Abs(offsetDist);
            using (var plines = new DisposableSet<Polyline>())
            {
                IEnumerable<Polyline> offsetRight = source.GetOffsetCurves(offsetDist).Cast<Polyline>();
                plines.AddRange(offsetRight);
                IEnumerable<Polyline> offsetLeft = source.GetOffsetCurves(-offsetDist).Cast<Polyline>();
                plines.AddRange(offsetLeft);
                double areaRight = offsetRight.Select(pline => pline.Area).Sum();
                double areaLeft = offsetLeft.Select(pline => pline.Area).Sum();
                switch (side)
                {
                    case OffsetSide.In:
                        return plines.RemoveRange(
                           areaRight < areaLeft ? offsetRight : offsetLeft);
                    case OffsetSide.Out:
                        return plines.RemoveRange(
                           areaRight < areaLeft ? offsetLeft : offsetRight);
                    case OffsetSide.Left:
                        return plines.RemoveRange(offsetLeft);
                    case OffsetSide.Right:
                        return plines.RemoveRange(offsetRight);
                    case OffsetSide.Both:
                        plines.Clear();
                        return offsetRight.Concat(offsetLeft);
                    default:
                        return null;
                }
            }
        }

        public static Polyline ExpandBy(this Polyline rectangle, double deltaX, double deltaY)
        {
            var v0 = rectangle.GetPoint3dAt(0) - rectangle.GetPoint3dAt(3);
            var v1 = rectangle.GetPoint3dAt(0) - rectangle.GetPoint3dAt(1);
            var p0 = rectangle.GetPoint3dAt(0) + (v0.GetNormal() * deltaY + v1.GetNormal() * deltaX);
            var p1 = rectangle.GetPoint3dAt(1) + (v0.GetNormal() * deltaY - v1.GetNormal() * deltaX);
            var p2 = rectangle.GetPoint3dAt(2) - (v0.GetNormal() * deltaY + v1.GetNormal() * deltaX);
            var p3 = rectangle.GetPoint3dAt(3) - (v0.GetNormal() * deltaY - v1.GetNormal() * deltaX);
            return CreateRectangle(p0, p1, p2, p3);
        }

        public static Point3dCollection Vertices(this Polyline pLine)
        {
            //https://keanw.com/2007/04/iterating_throu.html
            Point3dCollection vertices = new Point3dCollection();
            for (int i = 0; i < pLine.NumberOfVertices; i++)
            {
                vertices.Add(pLine.GetPoint3dAt(i));
            }
            return vertices;
        }

        public static bool IsClosed(this Polyline pLine, Tolerance tolerance)
        {            
            // 最少三个顶点才能形成一个闭环
            var vertices = pLine.Vertices();
            if (vertices.Count < 3)
            {
                return false;
            }

            // 比较第一个顶点和最后一个顶点，若他们重合，则多段线闭合；否则不闭合
            var enumerator = vertices.Cast<Point3d>();
            return enumerator.First().IsEqualTo(enumerator.Last(), tolerance);
        }

        public static Polyline CreateRectangle(Point3d pt1, Point3d pt2, Point3d pt3, Point3d pt4)
        {
            var points = new Point3dCollection()
            {
                pt1,
                pt2,
                pt3,
                pt4
            };
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(points);
            return pline;
        }

        public static Polyline CreateRectangle(Extents3d extents)
        {
            Point3d pt1 = extents.MinPoint;
            Point3d pt3 = extents.MaxPoint;
            Point3d pt2 = new Point3d(pt3.X, pt1.Y, pt1.Z);
            Point3d pt4 = new Point3d(pt1.X, pt3.Y, pt1.Z);
            return CreateRectangle(pt1,pt2,pt3,pt4);
        }
    }

}


