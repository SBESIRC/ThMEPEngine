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

namespace ThMEPElectrical.Broadcast
{
    public class LayoutService
    {
        readonly double protectRange = 27000;
        readonly double oneProtect = 21000;
        readonly double columnDis = 8000;
        public List<ColumnModel> LayoutBraodcast(Polyline roomPoly, Dictionary<List<Line>, List<ColumnModel>> mainColumns, Dictionary<List<Line>, List<ColumnModel>> otherColumns)
        {
            List<ColumnModel> layoutColumns = new List<ColumnModel>();
            foreach (var columnsPair in mainColumns)
            {
                var thisLines = columnsPair.Key;
                var thisColumns = columnsPair.Value;
                if (thisColumns.Count() <= 0)
                {
                    continue;
                }

                var columns = thisColumns.OrderByDescending(x => x.columnCenterPt.X).ToList();
                var numColumns = new List<ColumnModel>(columns);
                var firColumn = numColumns.First();
                numColumns.Remove(firColumn);
                double num = 0;

                while (numColumns.Count > 0)
                {
                    var matchColumns = numColumns.Where(x => x.columnCenterPt.DistanceTo(firColumn.columnCenterPt) < protectRange).ToList();
                    if (matchColumns.Count <= 0)
                    {
                        firColumn = numColumns.First();
                        numColumns.Remove(firColumn);
                    }
                    else
                    {
                        numColumns = numColumns.Except(matchColumns).ToList();
                        if (numColumns.Count > 0)
                        {
                            firColumn = matchColumns.OrderByDescending(x => x.columnCenterPt.DistanceTo(firColumn.columnCenterPt)).First();
                        }
                    }

                    num++;
                }

                double lineLength = thisLines.Sum(x => x.Length);
                if (oneProtect < lineLength && num == 1)
                {
                    num++;
                }

                if (num <= 1)
                {
                    var allPts = thisLines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }).OrderBy(x => x.X).ToList();
                    var centerPt = new Point3d((allPts[0].X + allPts[allPts.Count - 1].X) / 2, (allPts[0].Y + allPts[allPts.Count - 1].Y) / 2, 0);
                    layoutColumns.Add(columns.OrderBy(x => x.columnCenterPt.DistanceTo(centerPt)).First());
                }
                else
                {
                    var colFir = columns[0];
                    var colLast = columns[columns.Count - 1];
                    layoutColumns.AddRange(new List<ColumnModel>() { colFir, colLast });
                    if (colFir.columnCenterPt.DistanceTo(colLast.columnCenterPt) < protectRange)
                    {
                        num = num - 1;
                    }

                    int spacing = Convert.ToInt32(Math.Ceiling((columns.Count - 2) / num));
                    int index = spacing;
                    for (int i = 0; i < num - 1; i++)
                    {
                        layoutColumns.Add(columns[index]);
                        index = index + spacing;
                    }
                }
            }

            List<ColumnModel> supplementColumns = new List<ColumnModel>();
            supplementColumns.AddRange(otherColumns.SelectMany(x => x.Value));
            while (true)
            {
                //计算盲区
                var blindAreas = CalProtectBlindArea(layoutColumns, roomPoly);
                if (blindAreas.Count <= 0)
                {
                    break;
                }

                //补充盲区柱 
                supplementColumns = supplementColumns.Except(layoutColumns).ToList();
                var sColumns = SupplementBlindPoint(supplementColumns, blindAreas);
                if (sColumns.Count <= 0)
                {
                    break;
                }
                else
                {
                    layoutColumns.AddRange(sColumns);
                }
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (var item in layoutColumns)
                {
                    item.columnPoly.ColorIndex = 1;
                    acdb.ModelSpace.Add(item.columnPoly);
                    acdb.ModelSpace.Add(new Line(item.layoutPoint, item.layoutPoint + item.layoutDirection * 10));
                    //acdb.ModelSpace.Add(item.protectRadius);
                }
            }
            return layoutColumns;
        }

        /// <summary>
        /// 计算保护盲区
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="roomPoly"></param>
        /// <returns></returns>
        public List<Polyline> CalProtectBlindArea(List<ColumnModel> layoutColumns, Polyline roomPoly)
        {
            layoutColumns.ForEach(x => x.protectRadius = new Circle(x.columnCenterPt, Vector3d.ZAxis, protectRange));

            var objs = new DBObjectCollection();
            foreach (var col in layoutColumns)
            {
                foreach (var poly in col.protectRadius.ToNTSPolygon(20).ToDbPolylines())
                {
                    objs.Add(poly);
                }
            }

            var blindAreas = roomPoly.Difference(objs).Cast<Polyline>().ToList();
            return blindAreas;
        }

        /// <summary>
        /// 给盲区补充点
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="blindAreas"></param>
        /// <returns></returns>
        public List<ColumnModel> SupplementBlindPoint(List<ColumnModel> columns, List<Polyline> blindAreas)
        {
            List<ColumnModel> sColumn = new List<ColumnModel>();
            foreach (var poly in blindAreas)
            {
                Point3d firPt = poly.GetPoint3dAt(0);
                columns = columns.Where(x => x.columnCenterPt.DistanceTo(firPt) < protectRange).OrderBy(x => firPt.DistanceTo(x.columnCenterPt)).ToList();
                if (columns.Count > 0)
                {
                    if (!sColumn.Contains(columns.First()))
                    {
                        sColumn.Add(columns.First());
                    }
                }
            }

            return sColumn;
        }
    }
}
