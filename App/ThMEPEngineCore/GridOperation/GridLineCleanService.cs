using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GridOperation.Utils;

namespace ThMEPEngineCore.GridOperation
{
    public class GridLineCleanService
    {
        double gridLength = 10;
        double mergeSpacing = 3500;
        /// <summary>
        /// 清洗轴网线
        /// </summary>
        /// <param name="grids">轴网（直线或者弧）</param>
        /// <param name="columns"></param>
        public void CleanGrid(List<Curve> grids, List<Polyline> columns)
        {
            FilterLines(grids, out List<Line> lineGrids, out List<Arc> arcGrids);
            var lineGridGroup = LineGridGroup(lineGrids);
            var extendGrids = GridLineExtendService.ExtendGrid(lineGridGroup);
            //GridLineExtendService
        }

        private void FilterLines(List<Curve> grids, out List<Line> lineGrids, out List<Arc> arcGrids)
        {
            lineGrids = new List<Line>();
            arcGrids = new List<Arc>();
            //清除近乎零长度的对象（length≤10mm（梁线为40））
            //Z值归零（当直线夹点Z值不为零时需要处理）
            foreach (var curve in grids)
            {
                if (curve.GetLength() < 10)
                    continue;
                if (curve is Line line)
                {
                    var sp = line.StartPoint;
                    var ep = line.EndPoint;
                    sp = new Point3d(sp.X, sp.Y, 0);
                    ep = new Point3d(ep.X, ep.Y, 0);
                    lineGrids.Add(new Line(sp, ep));
                }
                else if (curve is Arc arc)
                {
                    var center = arc.Center;
                    center = new Point3d(center.X, center.Y, 0);
                    arcGrids.Add(new Arc(center, arc.Radius, arc.StartAngle, arc.EndAngle));
                }
            }
        }

        private Dictionary<Vector3d, List<Line>> LineGridMerge(List<Line> lines, List<Polyline> columns)
        {
            Dictionary<Vector3d, List<Line>> lineGroup = new Dictionary<Vector3d, List<Line>>();
            foreach (var line in lines)
            {
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                var compareKey = lineGroup.Keys.Where(x => x.IsParallelTo(dir, new Tolerance(0.01, 0.01))).FirstOrDefault();
                if (compareKey != null)
                {
                    lineGroup[compareKey].Add(line);
                }
                else
                {
                    var valueLines = new List<Line>() { line };
                    lineGroup.Add(dir, valueLines);
                }
            }



            return lineGroup;
        }

        private static Dictionary<Vector3d, List<Line>> LineGridGroup(List<Line> lines)
        {
            Dictionary<Vector3d, List<Line>> lineGroup = new Dictionary<Vector3d, List<Line>>();
            foreach (var line in lines)
            {
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                var compareKey = lineGroup.Keys.Where(x => x.IsParallelTo(dir, new Tolerance(0.01, 0.01))).FirstOrDefault();
                if (compareKey != null)
                {
                    lineGroup[compareKey].Add(line);
                }
                else
                {
                    var valueLines = new List<Line>() { line };
                    lineGroup.Add(dir, valueLines);
                }
            }

            return lineGroup;
        }
    }
}
