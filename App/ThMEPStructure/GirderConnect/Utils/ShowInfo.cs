using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.Utils
{
    class ShowInfo
    {
        /// <summary>
        /// 显示一个Geometry集合
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="acdb"></param>
        /// <param name="colerIndex"></param>
        public static void ShowGeometry(NetTopologySuite.Geometries.Geometry geometry, AcadDatabase acdb, int colerIndex = 1)
        {
            foreach (Entity obj in geometry.ToDbCollection())
            {
                obj.ColorIndex = colerIndex;
                acdb.ModelSpace.Add(obj);
            }
        }

        /// <summary>
        /// 显示设备所能探测的范围
        /// </summary>
        /// <param name="pt">设备位置</param>
        /// <param name="radius">设备可覆盖半径</param>
        public static void ShowArea(Point3d pt, double radius, int colorIndex = 90)
        {
            Circle circle = new Circle(pt, Vector3d.ZAxis, radius);
            circle.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(circle);
        }

        /// <summary>
        /// 用O显示一个点
        /// </summary>
        /// <param name="pt">点的位置</param>
        public static void ShowPointAsO(Point3d pt, int colorIndex = 80, double radius = 141.59265)
        {
            Circle circle = new Circle(pt, Vector3d.ZAxis, radius);
            circle.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(circle);
        }

        /// <summary>
        /// 用X显示一个点
        /// </summary>
        /// <param name="pt">点的位置</param>
        public static void ShowPointAsX(Point3d pt, int colorIndex = 80, double radius = 100)
        {
            Point3d p1 = new Point3d(pt.X - radius, pt.Y - radius, 0);
            Point3d p2 = new Point3d(pt.X - radius, pt.Y + radius, 0);
            Point3d p3 = new Point3d(pt.X + radius, pt.Y + radius, 0);
            Point3d p4 = new Point3d(pt.X + radius, pt.Y - radius, 0);

            Line line1 = new Line(p1, p3);
            line1.ColorIndex = colorIndex;
            Line line2 = new Line(p2, p4);
            line2.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(line1, line2);
        }

        /// <summary>
        /// 用带有方向的箭头表示一个布置设备的方向
        /// </summary>
        /// <param name="pt">设备布置位置</param>
        /// <param name="vector">方向</param>
        /// <param name="colorIndex">颜色</param>
        public static void ShowPointWithDirection(Point3d pt, Vector3d vec, int colorIndex = 210)
        {
            Point3d pt1 = pt + vec * 400;
            Point3d pt2 = pt + vec.RotateBy(Math.PI / 6, -Vector3d.ZAxis) * 200;
            Point3d pt3 = pt + vec.RotateBy(Math.PI / 6, Vector3d.ZAxis) * 200;
            Line line1 = new Line(pt, pt1);
            line1.ColorIndex = colorIndex;
            Line line2 = new Line(pt2, pt1);
            line2.ColorIndex = colorIndex;
            Line line3 = new Line(pt3, pt1);
            line3.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(line1, line2, line3);
        }

        /// <summary>
        /// 显示点集
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="type"></param>
        /// <param name="colorIndex"></param>
        public static void ShowPoints(List<Point3d> pts, char type = 'X', int colorIndex = 80, double radius = 100)
        {
            if (type == 'X')
            {
                foreach (Point3d pt in pts)
                {
                    ShowPointAsX(pt, colorIndex, radius);
                }
            }
            else
            {
                foreach (Point3d pt in pts)
                {
                    ShowPointAsO(pt, colorIndex, radius);
                }
            }
        }

        /// <summary>
        /// 画一条线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="color"></param>
        public static void DrawLine(Point3d point1, Point3d point2, int color = 1)
        {
            Line line = new Line(point1, point2);
            line.ColorIndex = color;
            line.AddToCurrentSpace();
        }
    }
}
