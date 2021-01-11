using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPLighting.EmgLight.Service
{
    public class StructureLayoutServiceLight
    {
        static double TolLight = 400;
        
        public  static void AddLayoutStructPt(List <Polyline> layoutList, List<Line> lane, ref Dictionary<Polyline, (Point3d, Vector3d)>  layoutPtInfo)
        {
            (Point3d, Vector3d) layoutInfo ;
            var laneDir = lane.Last().EndPoint - lane.First().StartPoint;

            foreach (var structure in layoutList)
            {
                if (layoutPtInfo.ContainsKey(structure) == false)
                {
                    layoutInfo = GetLayoutPoint(structure, laneDir);
                    layoutPtInfo.Add(structure, layoutInfo);
                }
            }
        }

            /// <summary>
            /// 计算柱上排布点和方向
            /// </summary>
            /// <param name="column"></param>
            /// <param name="pt"></param>
            /// <param name="dir"></param>
            /// <returns></returns>
            public static (Point3d, Vector3d) GetLayoutPoint(Polyline structure, Vector3d laneDir)
        {
            Point3d sPt = structure.StartPoint;
            Point3d ePt = structure.EndPoint;

            //计算排布点
            var layoutPt = new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0);

            //计算排布方向

            var StructDir = (ePt - sPt).GetNormal();
            var layoutDir = Vector3d.ZAxis.CrossProduct(StructDir);
           
            if (laneDir.DotProduct(StructDir) > 0)
            {
                layoutDir = -layoutDir;
            }

            return (layoutPt, layoutDir);
        }

        /// <summary>
        /// 找到墙与车道线平行的边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static List<Polyline> GetWallParallelPart(Polyline polyline, Point3d pt, Vector3d dir, out Point3d layoutPt)
        {

            var closetPt = polyline.GetClosestPointTo(pt, false);
            layoutPt = closetPt;
            List<Polyline> structureSegment = new List<Polyline>();


            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Polyline plTemp = new Polyline();
                plTemp.AddVertexAt(0, polyline.GetPoint2dAt(i), 0, 0, 0);
                plTemp.AddVertexAt(0, polyline.GetPoint2dAt((i + 1) % polyline.NumberOfVertices), 0, 0, 0);
                structureSegment.Add(plTemp);
            }

            dir = dir.GetNormal();
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);

            var structureLayoutSegment = structureSegment.Where(x =>
               {
                   var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                   bool bAngle = Math.Abs(dir.DotProduct(xDir)) / (dir.Length * xDir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                   return bAngle;
               }).ToList();

            return structureLayoutSegment;
        }


        /// <summary>
        /// 找到柱与车道线平行且最近的边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static List<Polyline> GetColumnParallelPart(Polyline polyline, Point3d pt, Vector3d dir, out Point3d layoutPt)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            layoutPt = closetPt;

            List<Polyline> structureSegment = new List<Polyline>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Polyline plTemp = new Polyline();
                plTemp.AddVertexAt(0, polyline.GetPoint2dAt(i), 0, 0, 0);
                plTemp.AddVertexAt(0, polyline.GetPoint2dAt((i + 1) % polyline.NumberOfVertices), 0, 0, 0);
                structureSegment.Add(plTemp);
            }

            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var structureLayoutSegment = structureSegment.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
                .Where(x =>
                {
                    var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                    bool bAngle = Math.Abs(dir.DotProduct(xDir)) / (dir.Length * xDir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                    return bAngle;
                }).ToList();

            return structureLayoutSegment;
        }

        /// <summary>
        /// 大于TolLight的墙拆分成TolLight(尾点不够和前面合并)
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        public static List<Polyline> breakWall(List<Polyline> walls)
        {
            List<Polyline> returnWalls = new List<Polyline>();
            
            foreach (var wall in walls)
            {
                Polyline restWall = wall;
                bool doOnce = false;
                while (restWall.Length > TolLight)
                {
                  Point3d breakPt= restWall.GetPointAtDist(TolLight);
                    Polyline breakWall = new Polyline();
                    breakWall.AddVertexAt(breakWall.NumberOfVertices, restWall.StartPoint.ToPoint2D() , 0, 0, 0);
                    breakWall.AddVertexAt(breakWall.NumberOfVertices, breakPt.ToPoint2D(), 0, 0, 0);
                    returnWalls.Add(breakWall);

                    restWall.SetPointAt(0, breakPt.ToPoint2D());
                    doOnce = true;
                }
                
                if (doOnce==true && restWall.Length> 0)
                {
                    returnWalls.Last().SetPointAt(returnWalls.Last().NumberOfVertices -1,restWall.EndPoint.ToPoint2D ());
                }

            }

            return returnWalls;
        }

        public static List<double> GetColumnDistList(List<Polyline> usefulOrderedColumns)
        {
            List<double> distX = new List<double>();
            for (int i = 0; i < usefulOrderedColumns.Count - 1; i++)
            {
                distX.Add((StructUtils.GetStructCenter(usefulOrderedColumns[i]) - StructUtils.GetStructCenter(usefulOrderedColumns[i + 1])).Length);
            }

            return distX;

        }

        public static double GetVariance(List<double> distX)
        {

            double avg = 0;
            double variance = 0;

            avg = distX.Sum() / distX.Count;

            for (int i = 0; i < distX.Count - 1; i++)
            {
                variance += Math.Pow(distX[i] - avg, 2);
            }

            variance = Math.Sqrt(variance / distX.Count);


            return variance;

        }

        public static List<Polyline> OrderingColumns(List<Polyline> Columns, List<Line> Lines)
        {
            Vector3d xDir = (Lines.First().EndPoint - Lines.First().StartPoint).GetNormal();
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
                });

            var orderColumns = Columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix.Inverse()).X).ToList();
            return orderColumns;
        }

        public static Point3d TransformPointToLine(Point3d pt, List<Line> Lines)
        {
            //getAngleTo根据右手定则旋转(一般逆时针)
            var rotationangle = Vector3d.XAxis.GetAngleTo((Lines.Last().EndPoint - Lines.First().StartPoint), Vector3d.ZAxis);
            Matrix3d matrix = Matrix3d.Displacement(Lines.First().StartPoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

            var transedPt = pt.TransformBy(matrix.Inverse());

            return transedPt;
        }

        /// <summary>
        /// 找到给定点投影到lanes尾的多线段和距离. 如果点在起点外,则返回投影到向前延长线到最末的距离和多线段.如果点在端点外,则返回点到端点的距离(负数)和多线段
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt1"></param>
        /// <param name="PolylineToEnd"></param>
        /// <returns></returns>
        public static double distToLineEnd(List<Line> lines, Point3d pt, out Polyline PolylineToEnd)
        {
            double distToEnd = -1;
            Point3d prjPt;
            PolylineToEnd = new Polyline();
            int timeToCheck = 0;
            var ptNew = TransformPointToLine(pt, lines);
            List<Line> transLines = lines.Select(x => new Line(TransformPointToLine(x.StartPoint, lines), TransformPointToLine(x.EndPoint, lines))).ToList();

            if (ptNew.X < transLines.First().StartPoint.X)
            {
                prjPt = lines[0].GetClosestPointTo(pt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                foreach (var l in lines)
                {
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                }
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                distToEnd = PolylineToEnd.Length;
            }
            else if (ptNew.X > transLines.Last().EndPoint.X)
            {
                prjPt = lines.Last().GetClosestPointTo(pt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                distToEnd = -PolylineToEnd.Length;
            }
            else
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (timeToCheck == 0 && transLines[i].StartPoint.X <= ptNew.X && ptNew.X <= transLines[i].EndPoint.X)
                    {
                        prjPt = lines[i].GetClosestPointTo(pt, false);
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                        timeToCheck = 1;
                    }
                    else if (timeToCheck > 0)
                    {
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines[i].StartPoint.ToPoint2D(), 0, 0, 0);
                    }
                }

                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                distToEnd = PolylineToEnd.Length;
            }


            return distToEnd;

        }

        /// <summary>
        /// 找点到线的投影点,如果点在线外,则返回延长线上的投影点
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="pt1"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        public static double distToLine(List<Line> lanes, Point3d pt, out Point3d prjPt)
        {
            double distProject = -1;
            var ptNew = TransformPointToLine(pt, lanes);
            prjPt = new Point3d();

            List<Line> transLines = lanes.Select(x => new Line(TransformPointToLine(x.StartPoint, lanes), TransformPointToLine(x.EndPoint, lanes))).ToList();


            if (ptNew.X < transLines.First().StartPoint.X)
            {
                prjPt = lanes[0].GetClosestPointTo(pt, true);

            }
            else if (ptNew.X > transLines.Last().EndPoint.X)
            {
                prjPt = lanes.Last().GetClosestPointTo(pt, true);

            }
            else
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    if (transLines[i].StartPoint.X <= ptNew.X && ptNew.X <= transLines[i].EndPoint.X)

                    {
                        prjPt = lanes[i].GetClosestPointTo(pt, false);
                        break;
                    }


                }
            }

            distProject = prjPt.DistanceTo(pt);
            return distProject;

        }

        /// <summary>
        /// 两点中点到线的投影点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="prjMidPt"></param>
        public static void findMidPointOnLine(List<Line> lines, Point3d pt1, Point3d pt2, out Point3d prjMidPt)
        {

            Point3d midPoint;


            Polyline lineTemp = new Polyline();

            midPoint = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            //InsertLightService.ShowGeometry (midPoint, 40);
            distToLine(lines, midPoint, out prjMidPt);

            // return distProject;

        }

        public static void findClosestStruct(List<Polyline> structure, Point3d Pt, List<Polyline> Layout, out double minDist, out Polyline closestStruct)
        {
            minDist = 10000;
            closestStruct = null;
            foreach (Polyline l in structure)
            {

                var connectLayout = Layout.Where(x => x.StartPoint == l.StartPoint ||
                                    x.StartPoint == l.EndPoint ||
                                    x.EndPoint == l.StartPoint ||
                                    x.EndPoint == l.EndPoint).ToList();



                if (l.Distance(Pt) <= minDist)
                {
                    minDist = l.Distance(Pt);
                    if (connectLayout.Count > 0)
                    {
                        closestStruct = connectLayout.First();
                    }
                    else
                    {
                        closestStruct = l;
                    }

                }
            }
        }

        public static bool FindPolyInExtendPoly(Polyline ExtendPoly, List<Polyline> usefulSturct, Polyline PolylineToEnd, double Tol, List<Polyline> Layout, out Polyline tempStruct)
        {
            bool bReturn = false;
            var inExtendStruct = usefulSturct.Where(x =>
            {
                return ExtendPoly.Contains(x) || ExtendPoly.Intersects(x);
            }).ToList();

            tempStruct = null;
            if (inExtendStruct.Count > 0)
            {
                //框内对面有位置布灯
                var ExtendLineStart = PolylineToEnd.GetPointAtDist(Tol);
                findClosestStruct(inExtendStruct, ExtendLineStart, Layout, out double minDist, out tempStruct);

            }
            if (tempStruct != null)
            {
                bReturn = true;
            }
            else
            {
                bReturn = false;
            }

            return bReturn;
        }


    }
}
