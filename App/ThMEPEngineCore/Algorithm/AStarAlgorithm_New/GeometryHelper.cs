using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New
{
    public static class GeometryHelper
    {
        #region GetAdjacentPoint
        /// <summary>
        /// GetAdjacentPoint 获取某个方向上的相邻点
        /// </summary>       
        public static List<Point3d> GetAdjacentPoint(Point3d current, List<Line> mapLines)
        {
            List<Point3d> adjacentPts = new List<Point3d>();
            var resLines = mapLines.Where(x => x.StartPoint.IsEqualTo(current, new Tolerance(0.001, 0.001)) || x.EndPoint.IsEqualTo(current, new Tolerance(0.001, 0.001))).ToList();
            foreach (var line in resLines)
            {
                adjacentPts.Add(line.EndPoint.IsEqualTo(current, new Tolerance(0.001, 0.001)) ? line.StartPoint : line.EndPoint);
            }

            return adjacentPts;
        }
        #endregion     
    }
}
