using System;
using System.Collections.Generic;
using System.Linq;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
 
namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class ThPolylineExtension
    {
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
                // 暂时不考虑“圆弧”的情况
                vertices.Add(pLine.GetPoint3dAt(i));
            }

            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if (pLine.Closed && !vertices[0].Equals(vertices[vertices.Count - 1]))
            {
                vertices.Add(vertices[0]);
            }

            return vertices;
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


