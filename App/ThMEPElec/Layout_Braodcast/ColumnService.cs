using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Layout_Braodcast
{
    public class ColumnService
    {
        readonly double tol = 800;

        public void HandleColumns(List<List<Line>> mainLines, List<List<Line>> otherLines, List<Polyline> columnPoly,
            out Dictionary<List<Line>, List<ColumnModel>> mainColumns, out Dictionary<List<Line>, List<ColumnModel>> otherColumns)
        {
            mainColumns = GetParkingLineColumn(mainLines, columnPoly);
            otherColumns = GetParkingLineColumn(otherLines, columnPoly);
        }

        public Dictionary<List<Line>, List<ColumnModel>> GetParkingLineColumn(List<List<Line>> parkingLines, List<Polyline> columnPoly)
        {
            Dictionary<List<Line>, List<ColumnModel>> resColumns = new Dictionary<List<Line>, List<ColumnModel>>();
            foreach (var pLines in parkingLines)
            {
                List<ColumnModel> pLineColumns = new List<ColumnModel>();
                List<ColumnModel> upColumns = new List<ColumnModel>();
                List<ColumnModel> downColumns = new List<ColumnModel>();
                foreach (var line in pLines)
                {
                    var sColPolys = SeparateColumnsByLine(columnPoly, line);
                    double upLength = CalColumnDistance(sColPolys[0], line);
                    double downLength = CalColumnDistance(sColPolys[1], line);
                    double length = tol;
                    if (upLength > downLength)
                    {
                        if (upLength > 2 * downLength && downLength > 2000)
                        {
                            length = length + downLength;
                        }
                        else
                        {
                            length = length + upLength;
                        }
                    }
                    else
                    {
                        if (2 * upLength < downLength && upLength > 2000)
                        {
                            length = length + upLength;
                        }
                        else
                        {
                            length = length + downLength;
                        }
                    }

                    Polyline polyline = expandLine(line, length);
                    upColumns.AddRange(GetColumn(line, polyline, CreateColumnModel(sColPolys[0])));
                    downColumns.AddRange(GetColumn(line, polyline, CreateColumnModel(sColPolys[1])));
                }

                if (upColumns.Count >= downColumns.Count)
                {
                    pLineColumns.AddRange(upColumns);
                }
                else
                {
                    pLineColumns.AddRange(downColumns);
                }
                pLineColumns = pLineColumns.Distinct().ToList();
                resColumns.Add(pLines, pLineColumns);
            }

            //using (AcadDatabase acdb = AcadDatabase.Active())
            //{
            //    foreach (var ss in resColumns)
            //    {
            //        foreach (var item in ss.Value)
            //        {
            //            item.columnPoly.ColorIndex = 1;
            //            acdb.ModelSpace.Add(item.columnPoly);
            //            acdb.ModelSpace.Add(new Line(item.layoutPoint, item.layoutPoint + item.layoutDirection * 10));
            //        }
            //    }
            //}
            return resColumns;
        }

        /// <summary>
        /// 获取停车线周边柱信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<ColumnModel> GetColumn(Line line, Polyline linePoly, List<ColumnModel> columns)
        {
            List<ColumnModel> columnLst = new List<ColumnModel>();
            var cols = columns.Where(x => linePoly.IndexedPointInPolygon(x.columnCenterPt) == LocateStatus.Interior).ToList();
            foreach (var col in cols)
            {
                CalColumnLayoutPoint(col, line);
            }
            columnLst.AddRange(cols);
            return columnLst;
        }

        /// <summary>
        /// 计算柱上排布点
        /// </summary>
        /// <param name="column"></param>
        /// <param name="line"></param>
        public void CalColumnLayoutPoint(ColumnModel column, Line line)
        {
            var closetPt = line.GetClosestPointTo(column.columnCenterPt, true);
            Line intersectLine = new Line(column.columnCenterPt, closetPt);
            
            Polyline columnPoly = column.columnPoly;
            List<Point3d> columnPts = new List<Point3d>();
            for (int i = 0; i < columnPoly.NumberOfVertices; i++)
            {
                columnPts.Add(columnPoly.GetPoint3dAt(i));
            }

            var interPoint = intersectLine.Intersection(columnPoly);
            columnPts = columnPts.OrderBy(x => x.DistanceTo(interPoint)).ToList();
            Point3d p1 = columnPts[0];
            Point3d p2 = columnPts[1];
            column.layoutPoint = new Point3d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0);
            column.layoutDirection = (column.layoutPoint - column.columnCenterPt).GetNormal();
        }

        /// <summary>
        /// 获取最近的柱距离
        /// </summary>
        /// <param name="columnPoly"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public double CalColumnDistance(List<Polyline> columnPoly, Line line)
        {
            DBObjectCollection dBObject = new DBObjectCollection();
            foreach (var column in columnPoly)
            {
                dBObject.Add(column);
            }

            double length = 0;
            if (dBObject.Count <= 0)
            {
                return length;
            }

            ThCADCoreNTSSpatialIndex thPatialIndex = new ThCADCoreNTSSpatialIndex(dBObject);
            var closet = thPatialIndex.NearestNeighbours(line, 1);

            
            if (closet.Count > 0)
            {
                Polyline closetColumn = closet.Cast<Polyline>().First();
                ColumnModel column = new ColumnModel(closetColumn);
                Point3d closetPt = line.GetClosestPointTo(column.columnCenterPt, false);
                length = closetPt.DistanceTo(column.columnCenterPt);
            }

            return length;
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<List<Polyline>> SeparateColumnsByLine(List<Polyline> columns, Line line)
        {
            Vector3d xDir = CalLineDirection(line);
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, line.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, line.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, line.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0
                });

            List<Polyline> upColumn = new List<Polyline>();
            List<Polyline> downColumn = new List<Polyline>();
            foreach (var col in columns)
            {
                var colModel = new ColumnModel(col);
                var transPt = colModel.columnCenterPt.TransformBy(matrix.Inverse());
                if (transPt.Y < 0)
                {
                    downColumn.Add(col);
                }
                else
                {
                    upColumn.Add(col);
                }
            }
            
            return new List<List<Polyline>>() { upColumn, downColumn };
        }
        
        /// <summary>
        /// 让线指向x或者y轴正方向
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public Vector3d CalLineDirection(Line line)
        {
            Vector3d lineDir = (line.EndPoint - line.StartPoint).GetNormal();

            double xDotValue = Vector3d.XAxis.DotProduct(lineDir);
            double yDotValue = Vector3d.YAxis.DotProduct(lineDir);
            if (Math.Abs(xDotValue) > Math.Abs(yDotValue))
            {
                return xDotValue > 0 ? lineDir : -lineDir;
            }
            else
            {
                return yDotValue > 0 ? lineDir : -lineDir;
            }
        }

        /// <summary>
        /// 创建柱对象
        /// </summary>
        /// <param name="columnPoly"></param>
        /// <returns></returns>
        public List<ColumnModel> CreateColumnModel(List<Polyline> columnPoly)
        {
            List<ColumnModel> colLst = new List<ColumnModel>();
            foreach (var cPoly in columnPoly)
            {
                ColumnModel column = new ColumnModel(cPoly);
                colLst.Add(column);
            }

            return colLst;
        }

        /// <summary>
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint - lineDir * tol + moveDir * distance;
            Point3d p2 = line.EndPoint + lineDir * tol + moveDir * distance;
            Point3d p3 = line.EndPoint + lineDir * tol - moveDir * distance;
            Point3d p4 = line.StartPoint - lineDir * tol - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
    }
}
