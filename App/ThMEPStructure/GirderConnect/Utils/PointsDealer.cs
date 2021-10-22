using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using AcHelper;
using DotNetARX;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using ThMEPStructure.GirderConnect.ConnectProcess;

namespace ThMEPStructure.GirderConnect.Utils
{
    class PointsDealer
    {
        /// <summary>
        /// 将多边形上的折点按内外点分类
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="PointClass"></param>
        /// <returns></returns>
        public static void PointClassify(Polyline polyline, Dictionary<Point3d, int> PointClass)
        {
            int n = polyline.NumberOfVertices;
            for (int i = 0; i < n; ++i)
            {
                var prePoint = polyline.GetPoint3dAt((i + n - 1) % n);
                var curPoint = polyline.GetPoint3dAt(i);
                var nxtPoint = polyline.GetPoint3dAt((i + 1) % n);
                if (!PointClass.ContainsKey(curPoint))
                {
                    if (DirectionCompair(prePoint, curPoint, nxtPoint) < 0)
                    {
                        //ShowInfo.ShowPointAsO(curPoint, 130); // debug
                        PointClass.Add(curPoint, 1);
                    }
                    else
                    {
                        //ShowInfo.ShowPointAsO(curPoint, 210); // debug
                        PointClass.Add(curPoint, 2);
                    }
                }
            }
        }

        /// <summary>
        /// 比较两条线的倾斜度
        /// </summary>
        /// <param name="prePoint">线A起点</param>
        /// <param name="curPoint">线A终点，同时是线B起点</param>
        /// <param name="nxtPoint">线B终点</param>
        /// <returns></returns>
        public static double DirectionCompair(Point3d prePoint, Point3d curPoint, Point3d nxtPoint)
        {
            return (curPoint.X - prePoint.X) * (nxtPoint.Y - curPoint.Y) - (nxtPoint.X - curPoint.X) * (curPoint.Y - prePoint.Y);
        }

        /// <summary>
        /// 获得多边形的外点
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point3d> OutPoints(Polyline polyline)
        {
            int ptType = 0;
            Dictionary<Point3d, int> PointClass = new Dictionary<Point3d, int>();
            PointClassify(polyline, PointClass);
            var points = Algorithms.GetConvexHull(PointClass.Keys.ToList());
            foreach(var point in points) //此步骤实际为O(1)时间
            {
                if (PointClass.ContainsKey(point))
                {
                    ptType = PointClass[point];
                    break;
                }
            }
            List<Point3d> outPoints = new List<Point3d>();
            if(ptType == 0)
            {
                return null;
            }
            else
            {
                foreach(var point in PointClass)
                {
                    if(point.Value == ptType)
                    {
                        ShowInfo.ShowPointAsO(point.Key, 130);
                        outPoints.Add(point.Key);
                    }
                    else
                    {
                        ShowInfo.ShowPointAsO(point.Key, 210);
                    }
                }
                return outPoints;
            }
        }

        /// <summary>
        /// 获取多边形的临近点
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Point3d> NearPoints(Polyline polyline, Point3dCollection points)
        {


            return new List<Point3d>();
        }
    }
}
