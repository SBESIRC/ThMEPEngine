using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
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
            RemoveZeroArea();
            _lineToCenter = lineToCenter;
            _centerToFace = centerToFace;
            _centerGrid = centerGrid;
        }

        /// <summary>
        /// 删除掉圈内的点，并改变结构
        /// </summary>
        private void RemoveInneriorLines(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            foreach (var ol in innOutline)
            {
                var containPoints = spatialIndex.SelectWindowPolygon(ol).OfType<DBPoint>().Select(d => d.Position).Distinct();
                foreach (var curCenterPt in containPoints)
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
                foreach (var curCenterPt in centerToFace.Keys.ToList())
                {
                    if (containPoints.Contains(curCenterPt))
                    {
                        continue;
                    }
                    RemoveAPointFromStructure(curCenterPt);
                }
            }
        }

        private void RemoveZeroArea()
        {
            foreach (var pt in centerGrid.Keys.ToList())
            {
                var pl = LineDealer.Tuples2Polyline(centerToFace[pt].ToList());
                if (pl.Area < 1000)
                {
                    RemoveAPointFromStructure(pt);
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
                foreach (var pt in centerGrid[curCenterPt].ToList())
                {
                    GraphDealer.DeleteFromGraph(pt, curCenterPt, ref centerGrid);
                }
            }
        }

        public static void RemoveOuterForbiddenLines(ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, PreProcess preProcessData)
        {
            var pt2Line = new Dictionary<Point3d, Tuple<Point3d, Point3d>>();
            foreach (var line in findPolylineFromLines.Keys)
            {
                var middlePt = new Point3d((line.Item1.X + line.Item2.X) / 2, (line.Item1.Y + line.Item2.Y) / 2, 0);
                if (!pt2Line.ContainsKey(middlePt))
                {
                    pt2Line.Add(middlePt, line);
                }
                if (!pt2Line.ContainsKey(line.Item1))
                {
                    pt2Line.Add(line.Item1, line);
                }
                if (!pt2Line.ContainsKey(line.Item2))
                {
                    pt2Line.Add(line.Item2, line);
                }
            }

            var dbPoints = pt2Line.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var containPoints = new HashSet<Point3d>();
            foreach (var ol in preProcessData.extOutline)
            {
                bool isClose = false;
                if(ol.Closed == false)
                {
                    isClose = true;
                    ol.Closed = true;
                }
                if (ol.Area < 10)
                {
                    continue;
                }
                spatialIndex.SelectWindowPolygon(ol.Buffer(50).OfType<Polyline>().Max()).OfType<DBPoint>().Select(d => d.Position).ForEach(pt => containPoints.Add(pt));
                if (isClose == true)
                {
                    isClose = false;
                    ol.Closed = false;
                }
            }
            foreach (var pt in pt2Line.Keys)
            {
                if (!containPoints.Contains(pt))
                {
                    var lineA = pt2Line[pt];
                    var lineB = new Tuple<Point3d, Point3d>(lineA.Item2, lineA.Item1);
                    RemoveAPolyline(ref findPolylineFromLines, lineA);
                    RemoveAPolyline(ref findPolylineFromLines, lineB);
                }
            }
        }

        private static void RemoveAPolyline(ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, Tuple<Point3d, Point3d> line)
        {
            if (!findPolylineFromLines.ContainsKey(line))
            {
                return;
            }
            var pl = findPolylineFromLines[line];
            foreach (var l in pl)
            {
                if (findPolylineFromLines.ContainsKey(l))
                {
                    findPolylineFromLines.Remove(l);
                }
            }
        }
    }
}
