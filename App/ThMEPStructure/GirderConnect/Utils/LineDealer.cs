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
    class LineDealer
    {
        /// <summary>
        /// 使一组乱序的tuples首尾相连
        /// </summary>
        /// <param name="tuples"></param>
        public static List<Tuple<Point3d, Point3d>> OrderTuples(List<Tuple<Point3d, Point3d>> tuples, double tolerance = 1.0)
        {
            List<Tuple<Point3d, Point3d>> ansTuples = new List<Tuple<Point3d, Point3d>>();
            var tmpPt = tuples[0].Item1;
            for (int i = 0; i < tuples.Count; ++i)
            {
                foreach(var tup in tuples)
                {
                    if((tup.Item1 == tmpPt || tmpPt.DistanceTo(tup.Item1) <= tolerance) && !ansTuples.Contains(new Tuple<Point3d, Point3d>(tup.Item1, tup.Item2)))
                    {
                        ansTuples.Add(new Tuple<Point3d, Point3d>(tup.Item1, tup.Item2));
                        tmpPt = tup.Item2;
                        break;
                    }
                }
            }
            return ansTuples;
        }

        /// <summary>
        /// 将多边形变成以两个点表示一条线列表
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Tuple<Point3d, Point3d>> Polyline2Tuples(Polyline polyline, double tolerance = 1.0)
        {
            List<Tuple<Point3d, Point3d>> tuples = new List<Tuple<Point3d, Point3d>>();
            Point3d prePoint = polyline.GetPoint3dAt(0);
            for (int i = 1; i < polyline.NumberOfVertices; ++i)
            {
                Point3d curPoint = polyline.GetPoint3dAt(i);
                if (prePoint.DistanceTo(curPoint) <= tolerance)
                {
                    continue;
                }
                tuples.Add(new Tuple<Point3d, Point3d>(prePoint, curPoint));
                prePoint = curPoint;
            }
            return tuples;
        }

        /// <summary>
        /// 将以两个点表示一条线的列表变成多边形
        /// </summary>
        /// <param name="tuples"></param>
        /// <returns></returns>
        public static Polyline Tuples2Polyline(List<Tuple<Point3d, Point3d>> tuples, double tolerance = 1.0)
        {
            Point3dCollection point3DCollection = new Point3dCollection();
            Polyline polyline = new Polyline();
            int n = tuples.Count;
            if(n == 0)
            {
                return polyline;
            }
            else if(n == 1)
            {
                polyline.AddVertexAt(0, new Point2d(tuples[0].Item1.X, tuples[0].Item1.Y), 0, 0, 0);
                polyline.AddVertexAt(1, new Point2d(tuples[0].Item2.X, tuples[0].Item2.Y), 0, 0, 0);
                return polyline;
            }
            else
            {
                var edges = OrderTuples(tuples);
                polyline.AddVertexAt(0, new Point2d(tuples[0].Item1.X, tuples[0].Item1.Y), 0, 0, 0);
                int cnt = 1;
                foreach(var edge in edges)
                {
                    if(edge.Item1.DistanceTo(edge.Item2) <= tolerance)
                    {
                        continue;
                    }
                    polyline.AddVertexAt(cnt, new Point2d(edge.Item2.X, edge.Item2.Y), 0, 0, 0);
                    ++cnt;
                }
                polyline.Closed = true;
                return polyline;
            }
        }
    }
}
