using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public class GridBoundaryExtendService
    {
        double spcing = 10000;
        double bufferTol = 500;
        public void ExtendGrid(List<LineGridModel> lineGrids, List<ArcGridModel> arcGrids, out List<LineGridModel> extendLineGrids)
        {
            extendLineGrids = new List<LineGridModel>();
            List<GridModel> grids = new List<GridModel>();
            var lineGridDic = lineGrids.ToDictionary(x => x, y => GetGridHull(GetAllGridPt(y)));
            var arcGridDic = arcGrids.ToDictionary(x => x, y => GetGridHull(GetAllGridPt(y)));
            foreach (var grid in lineGrids)
            {
                var gridHull = lineGridDic[grid];
                lineGridDic.Remove(grid);
                var otherGrids = lineGridDic.Where(x => x.Value.Intersects(gridHull)).Select(x => x.Key).ToList();
                var otherArcGrids = arcGridDic.Where(x => x.Value.Intersects(gridHull)).Select(x => x.Key).ToList();
                var extendGrid = ExtenLineGrid(grid, otherGrids, otherArcGrids);
                lineGridDic.Add(extendGrid, GetGridHull(GetAllGridPt(extendGrid)));
                extendLineGrids.Add(extendGrid);
            }
        }

        /// <summary>
        /// 延申轴网线到临近的轴网上
        /// </summary>
        /// <param name="lineGrid"></param>
        /// <param name="otherLineGrid"></param>
        /// <param name="arcGridModels"></param>
        private LineGridModel ExtenLineGrid(LineGridModel lineGrid, List<LineGridModel> otherLineGrid, List<ArcGridModel> arcGridModels)
        {
            LineGridModel extendGrid = new LineGridModel()
            {
                vecter = lineGrid.vecter,
                xLines = new List<Line>(),
                yLines = new List<Line>(),
            };

            var otherLines = new List<Curve>();
            otherLines.AddRange(otherLineGrid.SelectMany(x => x.xLines));
            otherLines.AddRange(otherLineGrid.SelectMany(x => x.yLines));
            otherLines.AddRange(arcGridModels.SelectMany(x => x.lines));
            otherLines.AddRange(arcGridModels.SelectMany(x => x.arcLines));
            foreach (var line in lineGrid.xLines)
            {   
                extendGrid.xLines.Add(ExtendLine(line, otherLines));
            }
            foreach (var line in lineGrid.yLines)
            {
                extendGrid.yLines.Add(ExtendLine(line, otherLines));
            }

            return extendGrid;
        }

        /// <summary>
        /// 延申一根线搭到最近的线上
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineGroup"></param>
        /// <returns></returns>
        private Line ExtendLine(Line line, List<Curve> lines)
        {
            Ray sRay = new Ray();
            sRay.BasePoint = line.StartPoint;
            sRay.UnitDir = (line.StartPoint - line.EndPoint).GetNormal();
            var sPt = GetIntersectPts(sRay, lines);
            if (sPt == null || sPt.Value.DistanceTo(line.StartPoint) > spcing)
            {
                sPt = line.StartPoint;
            }

            Ray eRay = new Ray();
            eRay.BasePoint = line.EndPoint;
            eRay.UnitDir = (line.EndPoint - line.StartPoint).GetNormal();
            var ePt = GetIntersectPts(eRay, lines);
            if (ePt == null || ePt.Value.DistanceTo(line.StartPoint) > spcing)
            {
                ePt = line.EndPoint;
            }
            return new Line(sPt.Value, ePt.Value);
        }

        /// <summary>
        /// 获取相交点
        /// </summary>
        /// <param name="Ray"></param>
        /// <param name="lineGroup"></param>
        /// <returns></returns>
        private Point3d? GetIntersectPts(Ray Ray, List<Curve> lineGroup)
        {
            var intersectPts = lineGroup.Select(x =>
            {
                Point3dCollection point3DCollection = new Point3dCollection();
                x.IntersectWith(Ray, Intersect.OnBothOperands, point3DCollection, (IntPtr)0, (IntPtr)0);
                if (point3DCollection.Count > 0)
                {
                    return point3DCollection[0] as Point3d?;
                }
                return null;
            })
            .Where(x => x != null)
            .ToList();
            if (intersectPts.Count <= 0)
            {
                return null;
            }

            var intersectPt = intersectPts.Select(x => x.Value)
            .OrderBy(x => x.DistanceTo(Ray.BasePoint))
            .FirstOrDefault();

            return intersectPt;
        }

        /// <summary>
        /// 获得轴网上所有点
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private List<Point3d> GetAllGridPt(GridModel grid)
        {
            var resPts = new List<Point3d>();
            if(grid is LineGridModel lineGrid)
            {
                resPts.AddRange(lineGrid.xLines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }));
                resPts.AddRange(lineGrid.yLines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }));
            }

            if (grid is ArcGridModel arcGrid)
            {
                resPts.AddRange(arcGrid.arcLines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }));
                resPts.AddRange(arcGrid.lines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }));
            }

            return resPts;
        }

        /// <summary>
        /// 计算凸包
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private Polyline GetGridHull(List<Point3d> pts)
        {
            var convex = pts.Select(x => x.ToPoint2D()).ToList().GetConvexHull();
            var convexPl = new Polyline() { Closed = true };
            for (int i = 0; i < convex.Count; i++)
            {
                convexPl.AddVertexAt(i, convex.ElementAt(i), 0, 0, 0);
            }

            convexPl = convexPl.Buffer(bufferTol)[0] as Polyline;
            return convexPl;
        }
    }
}
