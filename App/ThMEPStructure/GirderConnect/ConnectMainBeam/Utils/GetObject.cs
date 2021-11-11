using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class GetObject
    {
        /// <summary>
        /// GetMpolygon
        /// </summary>
        /// <returns></returns>
        public static MPolygon GetMpolygon(AcadDatabase acdb)
        {
            var result = Active.Editor.GetSelection();
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            var objs = new DBObjectCollection();
            foreach (var obj in result.Value.GetObjectIds())
            {
                objs.Add(acdb.Element<Entity>(obj));
            }
            return objs.BuildMPolygon();
        }

        /// <summary>
        /// GetPolyline
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static Polyline GetPolyline(AcadDatabase acdb)
        {
            var result = Active.Editor.GetEntity("请选择多边形");
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            return acdb.Element<Polyline>(result.ObjectId);
        }

        /// <summary>
        /// GetPointsOnGraph
        /// </summary>
        /// <returns></returns>
        public static List<Point3d> GetPoints()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            var points = new List<Point3d>();

            PromptSelectionResult psr = ed.GetSelection(); // 输入命令后再选择
            SelectionSet ss = psr.Value;
            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    DBPoint ent = (DBPoint)trans.GetObject(id, OpenMode.ForRead);
                    if (true)
                    {
                        points.Add(ent.Position);
                    }
                }
                trans.Commit();
            }
            return points;
        }

        /// <summary>
        /// 获取多个多边形
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static MultiLineString GetMultiLineString(AcadDatabase acdb)
        {
            //var tvs = new List<TypedValue>();
            //tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Polyline)).DxfName));
            //var sf = new SelectionFilter(tvs.ToArray());
            //var result = Active.Editor.GetSelection(sf);
            var result = Active.Editor.GetSelection();

            DBObjectCollection objs = new DBObjectCollection();
            if (result.Status == PromptStatus.OK)
            {
                //anoother way
                result.Value.GetObjectIds().Select(o => acdb.Element<Entity>(o)).Where(o => o is Polyline)
                   .Cast<Polyline>().ToList();
                //result.Value.GetObjectIds().Select(o => acdb.Element<Polyline>(o)).ToList();
            }
            return objs.ToMultiLineString();
        }

        /// <summary>
        /// 获取多个多边形
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static List<Polyline> GetPolylines(AcadDatabase acdb)
        {
            var tvs = new List<TypedValue>();
            tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Polyline)).DxfName));
            var sf = new SelectionFilter(tvs.ToArray());
            var result = Active.Editor.GetSelection(sf);
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            return result.Value.GetObjectIds().Select(o => acdb.Element<Polyline>(o)).ToList();
        }

        /// <summary>
        /// 获得多个所选多边形的中点
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static Point3dCollection GetCenters(AcadDatabase acdb)
        {
            var result = Active.Editor.GetSelection();
            var points = new Point3dCollection();
            if (result.Status == PromptStatus.OK)
            {
                foreach (var obj in result.Value.GetObjectIds())
                {
                    points.Add(acdb.Element<Entity>(obj).GeometricExtents.CenterPoint());
                }
            }
            return points;
        }

        /// <summary>
        /// find the nearest point by correct direction
        /// </summary>
        /// <param name="fromPt">起始点</param>
        /// <param name="toPt">目标点</param>
        /// <param name="points">目标点所在的点集</param>
        /// <param name="tolerance">查找范围容差（容差越小越准确，也越难出结果）</param>
        /// <param name="constrain">限制最长链接距离</param>
        /// <returns>要连接的点</returns>
        public static Point3d GetPointByDirection(Point3d fromPt, Point3d toPt, Point3dCollection points, double tolerance = Math.PI / 8, double constrain = 9000)
        {
            Vector3d baseVec = toPt - fromPt;
            double minFstCross = double.MaxValue;
            double minSndCross = double.MaxValue;
            double minThdCross = double.MaxValue;

            Point3d ansFstPt = fromPt;
            Point3d ansSndPt = fromPt;
            Point3d ansThdPt = fromPt;

            foreach (Point3d curPt in points)
            {
                double curRotate = baseVec.GetAngleTo(curPt - fromPt);
                double curDis = fromPt.DistanceTo(curPt);
                double curCross = curRotate * curDis;
                if (curRotate < tolerance && curDis < constrain && curDis > 200)
                {
                    if (curDis < constrain / 2)
                    {
                        if (curCross < minFstCross)
                        {
                            minFstCross = curCross;
                            ansFstPt = curPt;
                        }
                    }
                    else if (curRotate < tolerance / 2)
                    {
                        if (curCross < minSndCross)
                        {
                            minSndCross = curCross;
                            ansSndPt = curPt;
                        }
                    }
                    else
                    {
                        if (curCross < minThdCross)
                        {
                            minThdCross = curCross;
                            ansThdPt = curPt;
                        }
                    }
                }
            }

            if (ansFstPt != fromPt)
            {
                return ansFstPt;
            }
            else if (ansSndPt != fromPt)
            {
                return ansSndPt;
            }
            else if (ansThdPt != fromPt)
            {
                return ansThdPt;
            }
            return fromPt;
        }

        /// <summary>
        /// Get Closest Point By Direction On a Polyline
        /// </summary>
        /// <param name="basePt">基准点</param>
        /// <param name="vector">方向</param>
        /// <param name="range">射线距离</param>
        /// <param name="polyline">穿过的多边形</param>
        /// <returns>返回最近的相交点</returns>
        public static Point3d GetClosestPointByDirection(Point3d basePt, Vector3d vector, double range, Polyline polyline)
        {
            Line line = new Line(basePt, basePt + vector * range);
            Point3dCollection intersectPts = new Point3dCollection();
            Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            line.IntersectWith(polyline, Intersect.OnBothOperands, plane, intersectPts, IntPtr.Zero, IntPtr.Zero);
            plane.Dispose();
            if (intersectPts.Count == 0)
            {
                return basePt;
            }
            else
            {
                double minDis = double.MaxValue;
                double curDis;
                Point3d ansPt = new Point3d();
                foreach (Point3d pt in intersectPts)
                {
                    curDis = pt.DistanceTo(basePt);
                    if (curDis < minDis)
                    {
                        minDis = curDis;
                        ansPt = pt;
                    }
                }
                return ansPt;
            }
        }

        /// <summary>
        /// 将包含在某多边形内部的点加入这个多边形，并组成结构
        /// </summary>
        /// <param name="points"></param>
        /// <param name="pointInOutlines"></param>
        public static void FindPointsInOutline(Point3dCollection points, Dictionary<Polyline, HashSet<Point3d>> pointInOutlines)
        {
            foreach (var pointInOutline in pointInOutlines)
            {
                foreach (Point3d pt in points)
                {
                    if (pointInOutline.Key.ContainsOrOnBoundary(pt) && !pointInOutline.Value.Contains(pt))
                    {
                        pointInOutline.Value.Add(pt);
                    }
                }
            }
        }

        /// <summary>
        /// 获取多边形上距离目标点某个方向最近的那条线(此函数不好使)
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="basePt"></param>
        /// <param name="baseVec"></param>
        /// <returns></returns>
        public static Line GetClosetLineOfPolyline(Polyline polyline, Point3d basePt, Vector3d baseVec)
        {
            int n = polyline.NumberOfVertices;
            if (n < 2)
            {
                return null;
            }
            double minDis = double.MaxValue;
            Line ansLine = null;
            for (int i = 0; i < n; ++i)
            {
                Vector3d curVec = polyline.GetPoint3dAt(i) - polyline.GetPoint3dAt((i + 1) % n);
                Line curLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                double curDis = basePt.DistanceTo(curLine.GetClosestPointTo(basePt, false));
                if (Math.Abs(curVec.GetAngleTo(baseVec) - Math.PI / 2) < 7 && curDis < minDis) //垂直偏离不超过7度
                {
                    minDis = curDis;
                    ansLine = curLine;
                }
            }
            return ansLine;
        }

        /// <summary>
        /// 在多段线中找到包含某个点的线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Line FindLineContainPoint(Polyline polyline, Point3d point)
        {
            int n = polyline.NumberOfVertices;
            if (n < 2)
            {
                return null;
            }
            for (int i = 0; i < n; ++i)
            {
                Line curLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                if (point.DistanceTo(curLine.GetClosestPointTo(point, false)) < 1)
                {
                    return curLine;
                }
            }
            return null;
        }
    }
}
