using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class ShowInfo
    {
        /// <summary>
        /// 显示一个Geometry集合
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="colerIndex"></param>
        public static void ShowGeometry(NetTopologySuite.Geometries.Geometry geometry, int colerIndex = 1)
        {
            foreach (Entity obj in geometry.ToDbCollection())
            {
                obj.ColorIndex = colerIndex;
                HostApplicationServices.WorkingDatabase.AddToModelSpace(obj);
            }
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
        /// 用方块显示一个点
        /// </summary>
        /// <param name="pt">点的位置</param>
        public static void ShowPointAsU(Point3d pt, int colorIndex = 7, double radius = 200)
        {
            Point3d p1 = new Point3d(pt.X - radius, pt.Y - radius, 0);
            Point3d p2 = new Point3d(pt.X - radius, pt.Y + radius, 0);
            Point3d p3 = new Point3d(pt.X + radius, pt.Y + radius, 0);
            Point3d p4 = new Point3d(pt.X + radius, pt.Y - radius, 0);

            Line line1 = new Line(p1, p2);
            line1.ColorIndex = colorIndex;
            Line line2 = new Line(p2, p3);
            line2.ColorIndex = colorIndex;
            Line line3 = new Line(p3, p4);
            line3.ColorIndex = colorIndex;
            Line line4 = new Line(p4, p1);
            line4.ColorIndex = colorIndex;
            HostApplicationServices.WorkingDatabase.AddToModelSpace(line1, line2, line3, line4);
        }

        /// <summary>
        /// 用带有方向的箭头表示一个布置设备的方向
        /// </summary>
        /// <param name="pt">设备布置位置</param>
        /// <param name="vec">方向</param>
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
            else if (type == 'O')
            {
                foreach (Point3d pt in pts)
                {
                    ShowPointAsO(pt, colorIndex, radius);
                }
            }
            else
            {
                foreach (Point3d pt in pts)
                {
                    ShowPointAsU(pt, colorIndex, radius);
                }
            }
        }

        /// <summary>
        /// 画一条线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="color"></param>
        public static void ShowLine(Point3d point1, Point3d point2, int color = 1)
        {
            Line line = new Line(point1, point2);
            line.ColorIndex = color;
            line.AddToCurrentSpace();
        }

        public static void ShowGraph(Dictionary<Point3d, HashSet<Point3d>> graph, int color)
        {
            foreach (var dicTuple in graph)
            {
                foreach (var pt in dicTuple.Value)
                {
                    ShowLine(dicTuple.Key, pt, color);
                }
            }
        }

        public static void ShowBorderConnectNearLines(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, int color = 1)
        {
            foreach (var dicKVpair in outline2BorderNearPts.Values)
            {
                foreach (var KVpair in dicKVpair)
                {
                    foreach (var pt in KVpair.Value)
                    {
                        ShowLine(KVpair.Key, pt, color);
                    }
                }
            }
        }
    }
}
