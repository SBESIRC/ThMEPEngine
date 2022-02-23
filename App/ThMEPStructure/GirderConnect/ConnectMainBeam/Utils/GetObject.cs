using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Geometries;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;

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
        public static Point3d GetPointByDirection(Point3d fromPt, Vector3d aimDirection, HashSet<Point3d> basePts, double tolerance = Math.PI / 12, double constrain = 9000)
        {
            double minFstCross = double.MaxValue;
            double minSndCross = double.MaxValue;
            double minThdCross = double.MaxValue;

            Point3d ansFstPt = fromPt;
            Point3d ansSndPt = fromPt;
            Point3d ansThdPt = fromPt;

            foreach (Point3d curPt in basePts)
            {
                double curRotate = aimDirection.GetAngleTo(curPt - fromPt);
                double curDis = fromPt.DistanceTo(curPt);
                double curCross = curRotate * curDis * curDis;
                //double curCross = curRotate * curDis;
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
            else
            {
                return ansThdPt;
            }
        }
        public static Point3d GetPointByDirectionB(Point3d fromPt, Vector3d aimDirection, Point3dCollection basePts, double tolerance = Math.PI / 12, double constrain = 9000)
        {
            Point3d ansPt = fromPt;
            double minDis = double.MaxValue;
            foreach (Point3d curPt in basePts)
            {
                double curRotate = aimDirection.GetAngleTo(curPt - fromPt);
                double curDis = fromPt.DistanceTo(curPt);
                if (curRotate < tolerance && curDis < constrain && curDis > 200)
                {
                    if (curDis < minDis)
                    {
                        ansPt = curPt;
                        minDis = curDis;
                    }
                }
            }
            return ansPt;
        }
        public static Point3d GetPointByDirectionB(Point3d fromPt, Vector3d aimDirection, HashSet<Point3d> basePts, double tolerance = Math.PI / 12, double constrain = 9000)
        {
            Point3d ansPt = fromPt;
            double minDis = double.MaxValue;
            foreach (Point3d curPt in basePts)
            {
                double curRotate = aimDirection.GetAngleTo(curPt - fromPt);
                double curDis = fromPt.DistanceTo(curPt);
                if (curRotate < tolerance && curDis < constrain && curDis > 200)
                {
                    if (curDis < minDis)
                    {
                        ansPt = curPt;
                        minDis = curDis;
                    }
                }
            }
            return ansPt;
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
        public static Point3d GetClosestPointByDirectionB(Point3d basePt, Vector3d vector, double range, List<Line> exdLines, ref Line vtPtLine)
        {
            var directionLine = new Line(basePt, basePt + vector * range);
            double minDis = double.MaxValue;
            double curDis;
            Point3d ansPt = basePt;
            foreach (var exdLine in exdLines)
            {
                var pts = LineDealer.IntersectWith(directionLine, exdLine);
                if (pts.Count == 0)
                {
                    continue;
                }
                Point3d pt = pts[0];
                curDis = basePt.DistanceTo(pt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    ansPt = pt;
                    vtPtLine = exdLine;
                }
            }
            return ansPt;
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
        /// 获取多边形上距离目标点某个方向最近的那条线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="basePt"></param>
        /// <param name="baseVec"></param>
        /// <returns></returns>
        public static Line GetCloestLineOfPolyline(Polyline polyline, Point3d basePt, Vector3d baseVec)
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
                if (Math.Abs(curVec.GetAngleTo(baseVec) - Math.PI / 2) < Math.PI/180*7 && curDis < minDis) //垂直偏离不超过7度
                {
                    minDis = curDis;
                    ansLine = curLine;
                }
            }
            return ansLine;
        }


        public static void GetCloestLineOfPolyline(Polyline polyline, Point3d basePt, ref Vector3d vector)
        {
            int n = polyline.NumberOfVertices;
            if (n < 2)
            {
                return;
            }
            double minDis = double.MaxValue;
            Line ansLine = null;
            for (int i = 0; i < n; ++i)
            {
                //Vector3d curVec = polyline.GetPoint3dAt(i) - polyline.GetPoint3dAt((i + 1) % n);
                Line curLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                double curDis = basePt.DistanceTo(curLine.GetClosestPointTo(basePt, false));
                if (curDis < minDis)
                {
                    minDis = curDis;
                    ansLine = curLine;
                }
            }
            vector = basePt.DistanceTo(ansLine.StartPoint) < basePt.DistanceTo(ansLine.EndPoint) ? ansLine.StartPoint - ansLine.EndPoint : ansLine.EndPoint - ansLine.StartPoint;
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
                if (point.DistanceTo(curLine.GetClosestPointTo(point, false)) < 10)
                {
                    return curLine;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据三个方向确定第四个方向
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="ptC"></param>
        /// <returns></returns>
        public static Vector3d GetDirectionByThreeVecs(Point3d basePt, Point3d ptA, Point3d ptB, Point3d ptC)
        {
            var vecA = ptA - basePt;
            var vecB = ptB - basePt;
            var vecC = ptC - basePt;
            double angelAB = vecA.GetAngleTo(vecB, Vector3d.ZAxis);
            double angelCA = vecC.GetAngleTo(vecA, Vector3d.ZAxis);
            double angelBC = vecB.GetAngleTo(vecC, Vector3d.ZAxis);
            double angelAC = vecA.GetAngleTo(vecC, Vector3d.ZAxis);
            if (angelAB > angelAC)
            {
                vecB = ptC - basePt;
                vecC = ptB - basePt;
                angelAB = vecA.GetAngleTo(vecB, Vector3d.ZAxis);
                angelBC = vecB.GetAngleTo(vecC, Vector3d.ZAxis);
                angelCA = vecC.GetAngleTo(vecA, Vector3d.ZAxis);
            }
            double absAB90 = Math.Abs(angelAB - Math.PI / 2);
            double absCA90 = Math.Abs(angelCA - Math.PI / 2);
            double absBC90 = Math.Abs(angelBC - Math.PI / 2);
            double absAB180 = Math.Abs(angelAB - Math.PI);
            double absCA180 = Math.Abs(angelCA - Math.PI);
            double absBC180 = Math.Abs(angelBC - Math.PI);
            double min90;
            double min180;
            Vector3d vec9X, vec9Y, vec9Z;
            Vector3d vec18X, vec18Y;
            if (absAB90 <= absCA90 && absAB90 <= absBC90)
            {
                min90 = absAB90;
                vec9X = vecA;
                vec9Y = vecB;
                vec9Z = vecC;
            }
            else if (absCA90 <= absBC90)
            {
                min90 = absCA90;
                vec9X = vecC;
                vec9Y = vecA;
                vec9Z = vecB;
            }
            else
            {
                min90 = absBC90;
                vec9X = vecB;
                vec9Y = vecC;
                vec9Z = vecA;
            }
            if (absAB180 <= absCA180 && absAB180 <= absBC180)
            {
                min180 = absAB180;
                vec18X = vecA;
                vec18Y = vecB;
            }
            else if (absCA180 <= absBC180)
            {
                min180 = absCA180;
                vec18X = vecC;
                vec18Y = vecA;
            }
            else
            {
                min180 = absBC180;
                vec18X = vecB;
                vec18Y = vecC;
            }
            if (min90 < min180)
            {
                var angelXY = vec9X.GetAngleTo(vec9Y, Vector3d.ZAxis);
                if (vec9Z.GetAngleTo(vec9X, Vector3d.ZAxis) > Math.PI - angelXY / 2)
                {
                    return vec9X.RotateBy(Math.PI - angelXY, -Vector3d.ZAxis);
                }
                else
                {
                    return vec9Y.RotateBy(Math.PI - angelXY, Vector3d.ZAxis);
                }
            }
            else
            {
                return vec18X.RotateBy((vec18X.GetAngleTo(vec18Y, Vector3d.ZAxis)) / 2, Vector3d.ZAxis);
            }
        }
    }
}
