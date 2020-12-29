using System;
using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.EmgLight.Service
{
    public class CheckService
    {
        //public List<Polyline> FilterColumns(List<Polyline> columns, Line line, Polyline frame, Point3d sPt, Point3d ePt)
        //{
        //    Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
        //    Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
        //    Vector3d zDir = Vector3d.ZAxis;
        //    Matrix3d matrix = new Matrix3d(
        //        new double[] {
        //            xDir.X, yDir.X, zDir.X, line.StartPoint.X,
        //            xDir.Y, yDir.Y, zDir.Y, line.StartPoint.Y,
        //            xDir.Z, yDir.Z, zDir.Z, line.StartPoint.Z,
        //            0.0, 0.0, 0.0, 1.0
        //        });

        //    if (columns.Count > 0)
        //    {
        //        var orderColumns = columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix).X).ToList();
        //        if (!IsUsefulColumn(frame, orderColumns.First(), sPt, xDir))
        //        {
        //            columns.Remove(orderColumns.First());
        //        }
        //        if (!IsUsefulColumn(frame, orderColumns.Last(), ePt, xDir))
        //        {
        //            columns.Remove(orderColumns.Last());
        //        }
        //        //var moveColumns = polyCols.Where(x => !plFrame.Contains(StructUtils.GetStructCenter(x))).ToList();
        //        //foreach (var col in moveColumns)
        //        //{
        //        //    columns.Remove(col);
        //        //}
        //    }

        //    return columns;
        //}

        ///// <summary>
        ///// 给定边是否平行于车道线,是否与防火墙相交
        ///// </summary>
        ///// <param name="frame"></param>
        ///// <param name="polyline"></param>
        ///// <param name="pt"></param>
        ///// <param name="dir"></param>
        ///// <returns></returns>
        //private bool IsUsefulColumn(Polyline frame, Polyline polyline, Point3d pt, Vector3d dir)
        //{
        //    var newPoly = polyline.Buffer(200)[0] as Polyline;
        //    Line layoutLine = IsLayoutColumn(newPoly, pt, dir);
        //    Point3dCollection pts = new Point3dCollection();
        //    layoutLine.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
        //    if (pts.Count > 0)
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        ///// <summary>
        ///// 柱是否平行于车道线
        ///// </summary>
        ///// <param name="polyline"></param>
        ///// <param name="pt"></param>
        ///// <param name="dir"></param>
        ///// <returns></returns>
        //private Line IsLayoutColumn(Polyline polyline, Point3d pt, Vector3d dir)
        //{
        //    var closetPt = polyline.GetClosestPointTo(pt, false);
        //    List<Line> lines = new List<Line>();
        //    for (int i = 0; i < polyline.NumberOfVertices; i++)
        //    {
        //        lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
        //    }

        //    Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
        //    var layoutLine = lines.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
        //        .Where(x =>
        //        {
        //            var xDir = (x.EndPoint - x.StartPoint).GetNormal();
        //            return Math.Abs(otherDir.DotProduct(xDir)) < Math.Abs(dir.DotProduct(xDir));
        //        }).FirstOrDefault();

        //    return layoutLine;
        //}

        //public List<Polyline> FilterWalls(List<Polyline> walls, List<Line> lines, double expandLength, double tol)
        //{
        //    var wallCollections = walls.ToCollection();
        //    var resPolys = lines.SelectMany(x =>
        //    {
        //        var linePoly = StructUtils.ExpandLine(x, expandLength, tol);
        //        return linePoly.Intersection(wallCollections).Cast<Polyline>().ToList();
        //    }).ToList();

        //    return resPolys;
        //}

        public List<Polyline> FilterColumns(List<Polyline> columns, Line line, Polyline frame)
        {
            if (columns.Count <= 0)
            {
                return null;
            }

            List<Polyline> layoutColumns = new List<Polyline>();
            line = line.Normalize();

            var LineDir = (line.EndPoint - line.StartPoint).GetNormal();

            StructureLayoutServiceLight layoutServiceLight = new StructureLayoutServiceLight();
            foreach (Polyline structure in columns)
            {
                //平行于车道线的边
                //var newPoly = column.Buffer(200)[0] as Polyline;
                var layoutInfo = layoutServiceLight.GetColumnParallelPart(structure, line.StartPoint, LineDir, out Point3d closetPt);


                //选与防火框不相交且在防火框内
                if (layoutInfo != null)
                {

                    layoutInfo = layoutInfo.Where(x =>
                     {
                         Point3dCollection pts = new Point3dCollection();
                         x.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                         return pts.Count <= 0 && frame.Contains(x.StartPoint);
                     }).ToList();

                    layoutColumns.AddRange(layoutInfo);

                }

            }
            return layoutColumns;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="lines"></param>
        /// <param name="expandLength"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public List<Polyline> FilterWalls(List<Polyline> walls, Line line, Polyline frame)
        {
            //List<Polyline> layoutWalls = new List<Polyline>();
            //layoutWalls = FilterColumns(walls, line, frame);

            //var wallCollections = layoutWalls.ToCollection();
            //var resPolys = line.SelectMany(x =>
            //{
            //    var linePoly = StructUtils.ExpandLine(x,  tol);
            //    return linePoly.Intersection(wallCollections).Cast<Polyline>().ToList();
            //}).ToList();

            //return resPolys;

            if (walls.Count <= 0)
            {
                return null;
            }

            List<Polyline> layoutColumns = new List<Polyline>();
            line = line.Normalize();

            var LineDir = (line.EndPoint - line.StartPoint).GetNormal();

            StructureLayoutServiceLight layoutServiceLight = new StructureLayoutServiceLight();
            foreach (Polyline structure in walls)
            {
                //平行于车道线的边
                //var newPoly = column.Buffer(200)[0] as Polyline;
                var layoutInfo = layoutServiceLight.GetWallParallelPart(structure, line.StartPoint, LineDir, out Point3d closetPt);


                //选与防火框不相交且在防火框内
                if (layoutInfo != null)
                {

                    layoutInfo = layoutInfo.Where(x =>
                    {
                        Point3dCollection pts = new Point3dCollection();
                        x.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        return pts.Count <= 0 && frame.Contains(x.StartPoint);
                    }).ToList();

                    layoutColumns.AddRange(layoutInfo);

                }

            }
            return layoutColumns;
        }


    }
}
