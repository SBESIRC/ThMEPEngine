using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GridOperation.Model;
using ThMEPEngineCore.Algorithm.ArcAlgorithm;

namespace ThMEPEngineCore.GridOperation
{
    public class CutGridRegionService
    {
        public double tol = 10000;
        public List<Polyline> CutRegion(Polyline polyline, List<Curve> grids, List<Polyline> otherPolys, GridType gridType)
        {
            var polylines = new List<Polyline>(otherPolys);
            polylines.Add(polyline);
            var extendGrids = ExtendGirds(grids, polylines, gridType);
            foreach (var poly in otherPolys)
            {
                var dbCollec = new DBObjectCollection();
                poly.Explode(dbCollec);
                extendGrids.AddRange(dbCollec.Cast<Curve>());
            }

            var regions = GetGridRegion(extendGrids, polyline)
                .SelectMany(o => o.MakeValid().OfType<Polyline>())
                .Where(x => x.Area > 10)
                .ToList();
            var mPolyDic = ToMPolygonCollection(regions);
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(mPolyDic.Keys.ToCollection());
            foreach (var poly in otherPolys)
            {
                var bufferPoly = poly.Buffer(-10)[0] as Polyline;
                var intersectPolys = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<MPolygon>()
                    .Select(x => mPolyDic[x]).ToList();
                regions = regions.Except(intersectPolys).ToList();
            }

            return regions;
        }

        /// <summary>
        /// polyline转成mopolygon的映射
        /// </summary>
        /// <param name="polylines"></param>
        /// <returns></returns>
        private Dictionary<MPolygon, Polyline> ToMPolygonCollection(List<Polyline> polylines)
        {
            Dictionary<MPolygon, Polyline> mPolygonDic = new Dictionary<MPolygon, Polyline>();
            foreach (var poly in polylines)
            {
                var mPoly = new DBObjectCollection() { poly }.BuildMPolygon();
                mPolygonDic.Add(mPoly, poly);
            }
            return mPolygonDic;
        }

        /// <summary>
        /// 延申轴网线到边界
        /// </summary>
        /// <param name="grids"></param>
        /// <param name="polyline"></param>
        /// <param name="gridType"></param>
        /// <returns></returns>
        private List<Curve> ExtendGirds(List<Curve> grids, List<Polyline> polylines, GridType gridType)
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
                        var startPt = CalBoundaryPt(polylines, grid.StartPoint, -dir);
                        var endPt = CalBoundaryPt(polylines, grid.EndPoint, dir);
                        extendGrids.Add(new Line(startPt, endPt));
                    }
                }
            }
            else
            {
                foreach (var grid in grids)
                {
                    var dir = (grid.EndPoint - grid.StartPoint).GetNormal();
                    var startPt = CalBoundaryPt(polylines, grid.StartPoint, -dir);
                    var endPt = CalBoundaryPt(polylines, grid.EndPoint, dir);
                    extendGrids.Add(new Line(startPt, endPt));
                }
            }

            return extendGrids;
        }

        /// <summary>
        /// 根据轴网线切割轴网区域
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Polyline> GetGridRegion(List<Curve> curves, Polyline polyline)
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
        private Point3d CalBoundaryPt(List<Polyline> polylines, Point3d pt, Vector3d dir)
        {
            var startPt = pt;
            foreach (var polyline in polylines)
            {
                List<Point3d> allPts = new List<Point3d>();
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
                        allPts.AddRange(pts.Cast<Point3d>());
                    }
                }
                if (allPts.Count > 0)
                {
                    var closetPt = allPts.OrderBy(x => x.DistanceTo(startPt)).First();
                    if (closetPt.DistanceTo(startPt) < tol)
                    {
                        startPt = closetPt;
                    }
                }
            }


            return startPt;
        }
    }
}
