using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class WeiZhi
    {
        /// <summary>
        /// 求出2个实体的交点
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="ent1"></param>
        /// <param name="ent2"></param>
        /// <returns></returns>
        public static Point3dCollection GetIntersectPoints<T1, T2>(this T1 ent1, T2 ent2, Intersect interType)
            where T1 : Entity
            where T2 : Entity
        {
            Point3dCollection pts = new Point3dCollection();
            IntPtr ptr = new IntPtr();

            //在XY平面上判断相交
            ent1.IntersectWith(ent2, interType, pts, ptr, ptr);

            return pts;
        }

        public static Point3dCollection GetIntersectPoints<T1, T2>(this T1 ent1, T2 ent2, Plane plane, Intersect interType)
    where T1 : Entity
    where T2 : Entity
        {
            Point3dCollection pts = new Point3dCollection();
            IntPtr ptr = new IntPtr();

            //在当前UCS判断XY平面上相交
            ent1.IntersectWith(ent2, interType, plane, pts, ptr, ptr);

            return pts;
        }

        public static bool IsBottomGongXian(this Point3d pt1, Point3d pt2, double tol)
        {
            return Math.Abs(pt1.Y - pt2.Y) <= tol;

        }
    }
}
