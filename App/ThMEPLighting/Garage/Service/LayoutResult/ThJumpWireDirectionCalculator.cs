using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThJumpWireDirectionCalculator
    {
        private const double PointToLineDis = 1.0;
        private ThQueryLineService LineQuery { get; set; }
        private Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; }
        public ThJumpWireDirectionCalculator(
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            CenterSideDicts = centerSideDicts;
            CreateLineQuery();
        }

        private void CreateLineQuery()
        {
            var lines = new List<Line>();
            lines.AddRange(CenterSideDicts.SelectMany(o => o.Value.Item1));
            lines.AddRange(CenterSideDicts.SelectMany(o => o.Value.Item2));
            LineQuery = ThQueryLineService.Create(lines);
        }
        /// <summary>
        /// 计算点到中心线的向量
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Vector3d? Calcuate(Point3d pt)
        {
            var lines = LineQuery.Query(pt, PointToLineDis);
            if(lines.Count == 1)
            {
                var center = FindCenter(lines[0]);
                var projectionPt = ThGeometryTool.GetProjectPtOnLine(pt, center.StartPoint, center.EndPoint);
                return pt.GetVectorTo(projectionPt);
            }
            return null;
        }
        private Line FindCenter(Line side)
        {
            return CenterSideDicts
                .Where(o =>o.Value.Item1.Contains(side) || o.Value.Item2.Contains(side))
                .First().Key;
        }
    }
}
