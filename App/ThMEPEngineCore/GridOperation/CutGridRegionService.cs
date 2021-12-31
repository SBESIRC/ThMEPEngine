using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.ArcAlgorithm;
using ThMEPEngineCore.GridOperation.Model;
using ThMEPEngineCore.GridOperation.Utils;

namespace ThMEPEngineCore.GridOperation
{
    public class CutGridRegionService
    {
        double tol = 10000;
        public List<Polyline> CutRegion(Polyline polyline, List<Curve> grids, GridType gridType)
        {
            var regions = CutByGrid(grids, polyline, gridType);
            return regions;
        }

        private List<Polyline> CutByGrid(List<Curve> grids, Polyline polyline, GridType gridType)
        {
            List<Curve> extendGrids = new List<Curve>();
            if (gridType == GridType.ArcGrid)
            {
                foreach (var grid in grids)
                {
                    if (grid is Arc)
                    {
                        extendGrids.Add(grid);
                    }
                    else
                    {
                        var dir = (grid.EndPoint - grid.StartPoint).GetNormal();
                        var startPt = CalBoundaryPt(polyline, grid.StartPoint, -dir);
                        var endPt = CalBoundaryPt(polyline, grid.EndPoint, dir);
                        extendGrids.Add(new Line(startPt, endPt));
                    }
                }
            }
            else
            {
                foreach (var grid in grids)
                {
                    var dir = (grid.EndPoint - grid.StartPoint).GetNormal();
                    var startPt = CalBoundaryPt(polyline, grid.StartPoint, -dir);
                    var endPt = CalBoundaryPt(polyline, grid.EndPoint, dir);
                    extendGrids.Add(new Line(startPt, endPt));
                }
            }

            var regions = CetGridRegion(extendGrids, polyline);
            return regions;
        }

        /// <summary>
        /// 根据轴网线切割轴网区域
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Polyline> CetGridRegion(List<Curve> curves, Polyline polyline)
        {
            var cutCurves = new List<Curve>(curves);
            return cutCurves.ArcPolygonize(polyline, 500);
        }

        /// <summary>
        /// 获得当前点在边界上的点
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Point3d CalBoundaryPt(Polyline polyline, Point3d pt, Vector3d dir)
        {
            var startPt = pt;
            if (polyline.Contains(startPt) && polyline.GetClosestPointTo(startPt, false).DistanceTo(startPt) > 1)
            {
                Ray ray = new Ray()
                {
                    BasePoint = startPt,
                    UnitDir = -dir,
                };
                Point3dCollection pts = new Point3dCollection();
                polyline.IntersectWith(ray, Intersect.ExtendArgument, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    var closetPt = pts.Cast<Point3d>().OrderBy(x => x.DistanceTo(startPt)).First();
                    if (closetPt.DistanceTo(startPt) < tol)
                    {
                        startPt = closetPt;
                    }
                }
            }

            return startPt;
        }

        /// <summary>
        /// 创建轴网类
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="gridType"></param>
        /// <returns></returns>
        private GridModel ClassifyGrid(List<Curve> curves, GridType gridType)
        {
            if (gridType == GridType.ArcGrid)
            {
                ArcGridModel arcGrid = new ArcGridModel()
                {
                    centerPt = (curves.First(x => x is Arc) as Arc).Center,
                    arcLines = new List<Arc>(),
                    lines = new List<Line>(),
                };
                foreach (var curve in curves)
                {
                    if (curve is Line line)
                    {
                        arcGrid.lines.Add(line);
                    }
                    else if (curve is Arc arc)
                    {
                        arcGrid.arcLines.Add(arc);
                    }
                }
                return arcGrid;
            }
            else
            {
                LineGridModel lineGrid = new LineGridModel()
                {
                    vecter = (curves.First().EndPoint - curves.First().StartPoint).GetNormal(),
                    xLines = new List<Line>(),
                    yLines = new List<Line>(),
                };
                foreach (Line curve in curves)
                {
                    if ((curve.EndPoint - curve.StartPoint).GetNormal().IsParallelTo(lineGrid.vecter, new Tolerance(0.1, 0.1)))
                    {
                        lineGrid.xLines.Add(curve);
                    }
                    else
                    {
                        lineGrid.yLines.Add(curve);
                    }
                }
                return lineGrid;
            }
        }
    }
}
