using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.AFASRegion.Utls;
using Dreambuild.AutoCAD;
using ThMEPArchitecture.PartitionLayout;

namespace TianHua.Mep.UI.Command
{
    public class ThBuildDoorsCmd : ThMEPBaseCommand, IDisposable
    {
        public DBObjectCollection doors { get; set; }

        private string _wallLayer { get; set; }
        private string _shearwallLayer { get; set; }
        private string _doorLayer { get; set; }
        private string _columnLayer { get; set; }

        /// <summary>
        /// 可能构建出门的墙的厚度选择
        /// </summary>
        private HashSet<int> _wallThickness { get; set; }

        /// <summary>
        /// 两侧均为剪力墙的可能出现门的宽度选择
        /// </summary>
        private HashSet<int> _shearwallSpacing { get; set; }

        /// <summary>
        /// 至少有一侧为非剪力墙的可能出现门的宽度选择
        /// </summary>
        private HashSet<int> _OtherwallSpacing { get; set; }

        public ThBuildDoorsCmd(string wallLayer, string shearwallLayer, string doorLayer, string columnLayer)
        {
            ActionName = "根据AI-墙线找门";
            CommandName = "XXXX";
            _wallLayer = wallLayer;
            _shearwallLayer = shearwallLayer;
            _doorLayer = doorLayer;
            _columnLayer = columnLayer;
            _wallThickness = new HashSet<int> { 250, 300, 350, 400, 500, 600 };
            _shearwallSpacing = new HashSet<int> { 5500, 5600, 6000, 7000 };
            _OtherwallSpacing = new HashSet<int> { 500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500, 1550, 1600, 1800, 2000, 2100 };
        }
        public override void SubExecute()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var RangePts = GetRange(); //获取布置范围
                if (RangePts.Count < 3)
                {
                    return;
                }
                var Transformer = new ThMEPOriginTransformer(RangePts[0]);
                var newRangePts = Transformer.Transform(RangePts);
                var walls = GetWalls(acdb.Database, new List<string>() { _wallLayer, _doorLayer }, newRangePts, Transformer);
                var shearwalls = GetWalls(acdb.Database, new List<string>() { _shearwallLayer }, newRangePts, Transformer);
                var columns = GetColunms(acdb.Database, _columnLayer, newRangePts, Transformer);
                var wallSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.Union(shearwalls).ToCollection());
                foreach (Polyline column in columns)
                {
                    var space = column.BufferPL(10)[0] as Polyline;
                    var objs = wallSpatialIndex.SelectCrossingPolygon(space);
                    if (objs.Count > 0)
                    {
                        walls.AddRange(column.GetAllLinesInPolyline());
                    }
                }
                var wallObjs = new DBObjectCollection();
                
                walls.Union(shearwalls).OfType<Curve>().ForEach(c =>
                {
                    if (c is Polyline poly)
                    {
                        if (HasArc(poly))
                        {
                            var newPoly = poly.TessellatePolylineWithArc(100.0);
                            wallObjs.Add(newPoly);
                        }
                        else
                        {
                            wallObjs.Add(poly);
                        }
                    }
                    else if (c is Line line)
                    {
                        wallObjs.Add(line);
                    }
                });
                var collinear_gap_distance = ThLaneLineEngine.collinear_gap_distance;
                ThLaneLineEngine.collinear_gap_distance = 10;
                var mergedLines = ThLaneLineEngine.Explode(wallObjs);
                mergedLines = ThLaneLineMergeExtension.Merge(mergedLines);
                //mergedLines = ThLaneLineEngine.Noding(mergedLines);
                mergedLines = ThLaneLineEngine.CleanZeroCurves(mergedLines);
                ThLaneLineEngine.collinear_gap_distance = collinear_gap_distance;

                //Find ShearWall door stack
                var spatialIndex = new ThCADCoreNTSSpatialIndex(mergedLines);
                var shearwallSpatialIndex = new ThCADCoreNTSSpatialIndex(shearwalls.ToCollection());
                doors = new DBObjectCollection();
                Dictionary<Line, Vector3d> ReasonableWalls = new Dictionary<Line, Vector3d>();
                foreach (Line line in mergedLines)
                {
                    //过滤
                    if (line.Length < 210 || _wallThickness.Any(o => Math.Abs(line.Length - o) <= 10))
                    {
                        var pl = line.ExtendLine(10).Buffer(10);
                        var intersectingLines = spatialIndex.SelectFence(pl).OfType<Line>().ToList();
                        intersectingLines.Remove(line);
                        if (intersectingLines.Count >= 2)
                        {
                            if (IsSameSide(line, intersectingLines))
                            {
                                var startLines = intersectingLines.Where(o => line.StartPoint.IsPointOnLine(o, 10));
                                var endLines = intersectingLines.Where(o => line.EndPoint.IsPointOnLine(o, 10));
                                if (startLines.Count() == 1 && endLines.Count() == 1)
                                {
                                    var line1 = startLines.First();
                                    var line2 = endLines.First();
                                    var lineDirection = line.LineDirection();
                                    if (line1.Length > 49 && line2.Length > 49 && line1.LineDirection().IsVertical(lineDirection) && line2.LineDirection().IsVertical(lineDirection))
                                    {
                                        var vector = line1.LineDirection();
                                        if (line1.StartPoint.IsPointOnLine(line, 10))
                                        {
                                            vector = vector.Negate();
                                        }
                                        ReasonableWalls.Add(line, vector);
                                    }
                                }
                            }
                        }
                    }
                }
                ReasonableWalls = ReasonableWalls.OrderBy(o => o.Key.Length).ToDictionary(o => o.Key, o => o.Value);
                while (ReasonableWalls.Count > 0)
                {
                    var reasonableWall = ReasonableWalls.First();
                    var centerPt = GetCenterPt(reasonableWall.Key);
                    var probe = new Line(centerPt, centerPt + reasonableWall.Value * 7500).Buffer(10);
                    var objs = spatialIndex.SelectFence(probe);
                    objs.Remove(reasonableWall.Key);
                    var line = objs.OfType<Line>().OrderBy(o => o.DistanceTo(centerPt, false)).FirstOrDefault();
                    if (!line.IsNull())
                    {
                        if (ReasonableWalls.ContainsKey(line) && line.IsParallelToEx(reasonableWall.Key) && ReasonableWalls[line].IsCodirectionalTo(reasonableWall.Value.Negate(), new Tolerance(1, 1)))
                        {
                            var dis = line.Distance(reasonableWall.Key);
                            if (shearwallSpatialIndex.SelectCrossingPolygon(line.ExtendLine(10).Buffer(10)).Count > 0 && shearwallSpatialIndex.SelectCrossingPolygon(reasonableWall.Key.ExtendLine(10).Buffer(10)).Count > 0)
                            {
                                if (_shearwallSpacing.Any(o => Math.Abs(dis - o) <= 15))
                                {
                                    doors.Add(BuildDoor(reasonableWall.Key, line));
                                    ReasonableWalls.Remove(line);
                                }
                            }
                            else
                            {
                                if(_OtherwallSpacing.Any(o => Math.Abs(dis - o) <= 10))
                                {
                                    doors.Add(BuildDoor(reasonableWall.Key, line));
                                    ReasonableWalls.Remove(line);
                                }
                            }
                        }
                    }
                    ReasonableWalls.Remove(reasonableWall.Key);
                }
                Transformer.Reset(doors);
            }
        }

        private Polyline BuildDoor(Line line1, Line line2)
        {
            var minLine = line1.Length > line2.Length ? line2 : line1;
            var maxLine = line1.Length > line2.Length ? line1 : line2;
            var pt1 = minLine.StartPoint.GetProjectPtOnLine(maxLine.StartPoint, maxLine.EndPoint);
            var pt2 = minLine.EndPoint.GetProjectPtOnLine(maxLine.StartPoint, maxLine.EndPoint);
            return GeoUtilities.CreatePolyFromPoints(new Point3d[] { minLine.StartPoint, pt1, pt2, minLine.EndPoint });
        }

        private List<Curve> GetWalls(Database acdb, List<string> wallLayer, Point3dCollection rangePts, ThMEPOriginTransformer transformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(acdb))
            {
                var wallobjs = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .Where(o => wallLayer.Contains(o.Layer))
                    .Select(o =>
                    {
                        var entity = o.Clone() as Curve;
                        transformer.Transform(entity);
                        return entity;
                    })
                    .ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(wallobjs);
                return spatialIndex.SelectCrossingPolygon(rangePts).OfType<Curve>().ToList();
            }
        }

        private List<Polyline> GetColunms(Database acdb, string columnLayer, Point3dCollection rangePts, ThMEPOriginTransformer transformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(acdb))
            {
                var columnobjs = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer.Equals(columnLayer))
                    .Select(o =>
                    {
                        var entity = o.Clone() as Polyline;
                        transformer.Transform(entity);
                        return entity;
                    })
                    .ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(columnobjs);
                return spatialIndex.SelectCrossingPolygon(rangePts).OfType<Polyline>().ToList();
            }
        }

        public void Dispose()
        {
            //
        }

        private Point3dCollection GetRange()
        {
            var frame = ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
            if (frame.Area < 1e-4)
            {
                return new Point3dCollection();
            }
            var nFrame = ThMEPFrameService.Normalize(frame);
            return nFrame.Vertices();
        }

        /// <summary>
        /// 判断与line相交的直线是否都在line的同一侧
        /// </summary>
        /// <returns></returns>
        private bool IsSameSide(Line line, List<Line> otherLines)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (var otherline in otherLines)
            {
                if (otherline.StartPoint.IsPointOnLine(line, 10))
                {
                    pts.Add(otherline.EndPoint);
                }
                else if (otherline.EndPoint.IsPointOnLine(line, 10))
                {
                    pts.Add(otherline.StartPoint);
                }
                else
                {
                    return false;
                }
            }
            var pt = pts[0];
            for (int i = 1; i < pts.Count; i++)
            {
                if (new Line(pt, pts[i]).LineIsIntersection(line))
                {
                    return false;
                }
            }
            return true;
        }

        private Point3d GetCenterPt(Line line)
        {
            return line.StartPoint + (line.EndPoint - line.StartPoint) / 2.0;
        }

        private bool HasArc(Polyline poly)
        {
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var st = poly.GetSegmentType(i);
                if (st == SegmentType.Arc)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
