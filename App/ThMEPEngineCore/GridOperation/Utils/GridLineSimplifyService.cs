using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public class GridLineSimplifyService
    {
        double tol = 5;
        public List<LineGridModel> Simplify(List<LineGridModel> lineGrids)
        {
            var resGrids = new List<LineGridModel>();
            foreach (var grid in lineGrids)
            {
                LineGridModel lineGrid = new LineGridModel()
                {
                    vecter = grid.vecter,
                };

                lineGrid.xLines = MergeLine(grid.xLines);
                lineGrid.yLines = MergeLine(grid.yLines);
                resGrids.Add(lineGrid);
            }

            return resGrids;
        }

        /// <summary>
        /// 寻找距离过近的线并合并
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private List<Line> MergeLine(List<Line> lines)
        {
            var firLine = lines.First();
            lines.Remove(firLine);
            var resLines = new List<Line>();
            while (lines.Count > 0)
            {
                var tooCloseLines = lines.Where(x => x.Distance(firLine) < tol).ToList();
                if (tooCloseLines.Count > 0)
                {
                    tooCloseLines.Add(firLine);
                    lines = lines.Except(tooCloseLines).ToList();
                    var mergeLine = MergeCloesLine(tooCloseLines);
                    lines.Add(mergeLine);
                }
                else
                {
                    resLines.Add(firLine);
                }
                firLine = lines.First();
                lines.Remove(firLine);
            }
            resLines.Add(firLine);

            return resLines;
        }

        /// <summary>
        /// 距离过近的线段集合合并成一根长线
        /// </summary>
        /// <param name="closeLines"></param>
        /// <returns></returns>
        private Line MergeCloesLine(List<Line> closeLines)
        {
            var standardLine = closeLines.Last();
            var allPts = closeLines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }).ToList();

            var xDir = (standardLine.EndPoint - standardLine.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            allPts = allPts.Select(x => x.TransformBy(matrix)).OrderBy(x => x.X).ToList();
            var mergeLine = new Line(allPts.First(), allPts.Last());
            mergeLine.TransformBy(matrix.Inverse());

            return mergeLine;
        }
    }
}
