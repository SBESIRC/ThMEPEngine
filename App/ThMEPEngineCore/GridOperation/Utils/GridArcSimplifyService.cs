using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.ArcAlgorithm;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public class GridArcSimplifyService
    {
        double tol = 5;
        public List<ArcGridModel> Simplify(List<ArcGridModel> arcGrids)
        {
            var resGrids = new List<ArcGridModel>();
            foreach (var grid in arcGrids)
            {
                ArcGridModel arcGrid = new ArcGridModel()
                {
                    centerPt = grid.centerPt,
                };

                arcGrid.lines = MergeLine(grid.lines);
                arcGrid.arcLines = MergeArc(grid.arcLines);
                resGrids.Add(arcGrid);
            }

            return resGrids;
        }

        /// <summary>
        /// 寻找距离过近的弧线并合并
        /// </summary>
        /// <param name="arcs"></param>
        /// <returns></returns>
        private List<Arc> MergeArc(List<Arc> arcs)
        {
            //arcs = arcs.OrderByDescending(x => x.Length).ToList();
            var firArc = arcs.First();
            arcs.Remove(firArc);
            var resArcs = new List<Arc>();
            while (arcs.Count > 0)
            {
                var tooCloseArcs = arcs.Where(x => x.ArcDistance(firArc) < tol).ToList();
                if (tooCloseArcs.Count > 0)
                {
                    tooCloseArcs.Add(firArc);
                    arcs = arcs.Except(tooCloseArcs).ToList();
                    var mergeArc = MergeCloesArc(tooCloseArcs);
                    arcs.Add(mergeArc);
                }
                else
                {
                    resArcs.Add(firArc);
                }
                firArc = arcs.First();
                arcs.Remove(firArc);
            }
            resArcs.Add(firArc);

            return resArcs;
        }

        /// <summary>
        /// 距离过近的弧合成一根弧
        /// </summary>
        /// <param name="closeArcs"></param>
        /// <returns></returns>
        private Arc MergeCloesArc(List<Arc> closeArcs)
        {
            var standardArc = closeArcs.Last();
            var allAngles = closeArcs.SelectMany(x => new List<double>() { x.StartAngle, x.EndAngle })
                .OrderBy(x => x)
                .ToList();

            Arc arc = new Arc(standardArc.Center, standardArc.Radius, allAngles.First(), allAngles.Last());
            return arc;
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
