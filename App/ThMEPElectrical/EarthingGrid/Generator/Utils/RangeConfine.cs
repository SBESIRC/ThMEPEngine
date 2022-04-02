using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Data;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class RangeConfine
    {
        private Dictionary<Tuple<Point3d, Point3d>, Point3d> lineToCenter = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); // 通过一条线找到这条线所在多边形对应的中点
        private Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> centerToFace = new Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>>(); // 用一个点代表多边形
        private Dictionary<Point3d, HashSet<Point3d>> centerGrid = new Dictionary<Point3d, HashSet<Point3d>>(); // 多边形中点连接形成的图

        private HashSet<Polyline> innOutline = new HashSet<Polyline>();
        private HashSet<Polyline> extOutline = new HashSet<Polyline>();

        public RangeConfine(PreProcess _preProcessData, Dictionary<Tuple<Point3d, Point3d>, Point3d> _lineToCenter,
            Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> _centerToFace, Dictionary<Point3d, HashSet<Point3d>> _centerGrid)
        {
            innOutline = _preProcessData.innOutline;// preProcessData.buildingOutline
            extOutline = _preProcessData.extOutline;

            lineToCenter = _lineToCenter;
            centerToFace = _centerToFace;
            centerGrid = _centerGrid;
        }

        public void RemoveExteriorAndInteriorLines(ref Dictionary<Tuple<Point3d, Point3d>, Point3d> _lineToCenter,
            ref Dictionary<Point3d, HashSet<Tuple<Point3d, Point3d>>> _centerToFace, ref Dictionary<Point3d, HashSet<Point3d>> _centerGrid)
        {
            var dbPoints = centerToFace.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);

            RemoveInneriorLines(spatialIndex);
            RemoveExteriorLines(spatialIndex);

            _lineToCenter = lineToCenter;
            _centerToFace = centerToFace;
            _centerGrid = centerGrid;
        }

        /// <summary>
        /// 删除掉圈内的点，并改变结构
        /// </summary>
        private void RemoveInneriorLines(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            foreach(var ol in innOutline)
            {
                var containPoints = spatialIndex.SelectWindowPolygon(ol).OfType<DBPoint>().Select(d => d.Position).Distinct();
                foreach(var curCenterPt in containPoints)
                {
                    RemoveAPointFromStructure(curCenterPt);
                }
            }
        }

        /// <summary>
        /// 删除掉圈外的点，并改变结构
        /// </summary>
        private void RemoveExteriorLines(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            foreach (var ol in extOutline)
            {
                var containPoints = spatialIndex.SelectWindowPolygon(ol).OfType<DBPoint>().Select(d => d.Position).Distinct().ToHashSet();
                foreach(var curCenterPt in centerToFace.Keys.ToList())
                {
                    if (containPoints.Contains(curCenterPt))
                    {
                        continue;
                    }
                    RemoveAPointFromStructure(curCenterPt);
                }
            }
        }

        /// <summary>
        /// 从结构中删除一个点
        /// </summary>
        private void RemoveAPointFromStructure(Point3d curCenterPt)
        {
            if (centerToFace.ContainsKey(curCenterPt))
            {
                foreach (var line in centerToFace[curCenterPt])
                {
                    if (lineToCenter.ContainsKey(line))
                    {
                        lineToCenter.Remove(line);
                    }
                }
                centerToFace.Remove(curCenterPt);
            }
            if (centerGrid.ContainsKey(curCenterPt))
            {
                centerGrid.Remove(curCenterPt);
            }
        }
    }
}
