using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GridOperation.Model;
using ThMEPEngineCore.GridOperation.Utils;

namespace ThMEPEngineCore.GridOperation
{
    public class GridLineCleanService
    {
        double arcCenterTol = 100;
        double angleTolerance = 1 * Math.PI / 180.0;
        public double lineExtendLength = 10000;
        /// <summary>
        /// 清洗轴网线
        /// </summary>
        /// <param name="grids">轴网（直线或者弧）</param>
        /// <param name="columns"></param>
        public void CleanGrid(List<Curve> grids, List<Polyline> columns, out List<LineGridModel> lineGridRes, out List<ArcGridModel> arcGridRes)
        {
            FilterLines(grids, out List<Line> lineGrids, out List<Arc> arcGrids);
            
            //分组弧形轴网和直线轴网
            CalGridGroup(lineGrids, arcGrids, out List<LineGridModel> lineGroup, out List<ArcGridModel> arcGroup);

            //简单过滤
            lineGroup = lineGroup.Where(x => !(x.xLines == null || x.xLines.Count <= 0 || x.yLines == null || x.yLines.Count <= 0))
                .Where(x => x.xLines.Any(y => x.yLines.Any(z => z.IsIntersects(y)))).ToList();
            arcGroup = arcGroup.Where(x => !(x.lines == null || x.lines.Count <= 0 || x.arcLines == null || x.arcLines.Count <= 0)).ToList();
            FilterGrids(ref lineGroup, ref arcGroup);

            //处理直线轴网
            GridLineSimplifyService simplifyService = new GridLineSimplifyService();
            lineGridRes = simplifyService.Simplify(lineGroup);
            lineGridRes = GridLineExtendService.ExtendGrid(lineGridRes, lineExtendLength);
            lineGridRes = GridLineMergeService.MergeLine(lineGridRes, columns);

            //处理弧形轴网
            GridArcSimplifyService arcSimplifyService = new GridArcSimplifyService();
            arcGridRes = arcSimplifyService.Simplify(arcGroup);
            arcGridRes = GridArcExtendService.ExtendGrid(arcGridRes);
            arcGridRes = GridArcMergeService.MergeArcGrid(arcGridRes, columns);

            //处理相近轴网线
            GridBoundaryExtendService boundaryExtendService = new GridBoundaryExtendService();
            boundaryExtendService.ExtendGrid(lineGridRes, arcGridRes, out List<LineGridModel> extendLineGrids);
            lineGridRes = extendLineGrids;

            //using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            //{
            //    foreach (var item in extendLineGrids)
            //    {
            //        foreach (var ss in item.xLines)
            //        {
            //            db.ModelSpace.Add(ss);
            //        }
            //        foreach (var ss in item.yLines)
            //        {
            //            db.ModelSpace.Add(ss);
            //        }
            //    }
            //    foreach (var item in arcGridRes)
            //    {
            //        foreach (var ss in item.arcLines)
            //        {
            //            db.ModelSpace.Add(ss);
            //        }
            //        foreach (var ss in item.lines)
            //        {
            //            db.ModelSpace.Add(ss);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// 过滤掉无法构成闭合区域的轴网
        /// </summary>
        /// <param name="lineGridRes"></param>
        /// <param name="arcGridRes"></param>
        private void FilterGrids(ref List<LineGridModel> lineGridRes, ref List<ArcGridModel> arcGridRes)
        {
            List<LineGridModel> discardLineGrids = new List<LineGridModel>();
            foreach (var grid in lineGridRes)
            {
                var allLines = new List<Curve>(grid.xLines);
                allLines.AddRange(grid.yLines);
                if(allLines.ToCollection().PolygonsEx().Count <= 0)
                {
                    discardLineGrids.Add(grid);
                }
            }

            List<ArcGridModel> discardArcGrids = new List<ArcGridModel>();
            foreach (var grid in arcGridRes)
            {
                var allLines = new List<Curve>(grid.arcLines);
                allLines.AddRange(grid.lines);
                if (allLines.ToCollection().PolygonsEx().Count <= 0)
                {
                    discardArcGrids.Add(grid);
                }
            }

            lineGridRes = lineGridRes.Except(discardLineGrids).ToList();
            arcGridRes = arcGridRes.Except(discardArcGrids).ToList();
        }

        /// <summary>
        /// 简单过滤杂线
        /// </summary>
        /// <param name="grids"></param>
        /// <param name="lineGrids"></param>
        /// <param name="arcGrids"></param>
        private void FilterLines(List<Curve> grids, out List<Line> lineGrids, out List<Arc> arcGrids)
        {
            lineGrids = new List<Line>();
            arcGrids = new List<Arc>();
            //清除近乎零长度的对象（length≤300mm（梁线为40））
            //Z值归零（当直线夹点Z值不为零时需要处理）
            foreach (var curve in grids)
            {
                if (curve.GetLength() < 300)
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

        /// <summary>
        /// 分组轴网线（弧轴网/直线轴网，弧轴网根据圆心细分组，直线轴网根据方向细分组）
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="ArcLines"></param>
        /// <param name="lineGroup"></param>
        /// <param name="arcGroup"></param>
        private void CalGridGroup(List<Line> lines, List<Arc> ArcLines, out List<LineGridModel> lineGroup, out List<ArcGridModel> arcGroup)
        {
            lineGroup = new List<LineGridModel>();
            arcGroup = new List<ArcGridModel>();
            foreach (var arc in ArcLines)
            {
                var compareKey = arcGroup.Where(x => x.centerPt.DistanceTo(arc.Center) < arcCenterTol).FirstOrDefault();
                if (compareKey == null)
                {
                    ArcGridModel arcGrid = new ArcGridModel() { centerPt = arc.Center };
                    arcGrid.arcLines = new List<Arc>() { arc };
                    arcGroup.Add(arcGrid);
                }
                else
                {
                    arcGroup.First(x => x == compareKey).arcLines.Add(arc);
                }
            }

            foreach (var line in lines)
            {
                var compareValue = arcGroup.Where(x => line.GetClosestPointTo(x.centerPt, true).DistanceTo(x.centerPt) < arcCenterTol)
                    .Where(x => x.arcLines.Any(y => y.IsIntersects(line)))
                    .FirstOrDefault();
                if (compareValue != null)
                {
                    var arcLine = arcGroup.First(x => x == compareValue);
                    if (arcLine.lines != null)
                    {
                        arcLine.lines.Add(line);
                    }
                    else
                    {
                        arcLine.lines = new List<Line>() { line };
                    }
                }
                else
                {
                    var dir = (line.EndPoint - line.StartPoint).GetNormal();
                    var compareKey = lineGroup.Where(x => 
                    {
                        var angle = x.vecter.GetAngleTo(dir);
                        angle %= Math.PI;
                        if (angle <= angleTolerance || angle >= Math.PI - angleTolerance)
                        {
                            //平行
                            return true;
                        }
                        else if(Math.Abs(angle - Math.PI/2)<=angleTolerance)
                        {
                            return true;
                        }
                        return false;
                    }).FirstOrDefault();
                    if (compareKey != null)
                    {
                        var lineKey = lineGroup.First(x => x == compareKey);
                        var angle = lineKey.vecter.GetAngleTo(dir);
                        angle %= Math.PI;
                        if (angle <= angleTolerance || angle >= Math.PI - angleTolerance)
                        {
                            //平行
                            lineKey.xLines.Add(line);
                        }
                        else
                        {
                            if (lineKey.yLines != null)
                            {
                                lineKey.yLines.Add(line);
                            }
                            else
                            {
                                lineKey.yLines = new List<Line>() { line };
                            }
                        }
                    }
                    else
                    {
                        LineGridModel lineGrid = new LineGridModel();
                        lineGrid.vecter = dir;
                        lineGrid.xLines = new List<Line>() { line };
                        lineGroup.Add(lineGrid);
                    }
                }
            }
        }
    }
}
