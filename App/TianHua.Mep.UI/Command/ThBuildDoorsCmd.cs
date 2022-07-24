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
using TianHua.Mep.UI.Data;
using DotNetARX;

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
        /// 可能构建出门的柱的厚度选择
        /// </summary>
        private HashSet<int> _columnThickness { get; set; }

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
            _columnThickness = new HashSet<int> { 250, 300, 350, 400, 500, 600, 700, 800, 900, 1000 };
            _shearwallSpacing = new HashSet<int> { 5500, 5600, 6000, 7000 };
            _OtherwallSpacing = new HashSet<int> { 500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500, 1550, 1600, 1800, 2000, 2100, 3600, 5500, 5600, 6000, 7000 };
        }
        public override void SubExecute()
        {
            using (var acdb = AcadDatabase.Active())
            {
                doors = new DBObjectCollection();
                var RangePts = GetRange(); //获取布置范围
                if (RangePts.Count < 3)
                {
                    return;
                }
                var Transformer = new ThMEPOriginTransformer(RangePts[0]);
                var newRangePts = Transformer.Transform(RangePts);
                var walls = GetWalls(acdb.Database, _wallLayer, newRangePts, Transformer);
                var dbDoors = GetWalls(acdb.Database, _doorLayer, newRangePts, Transformer);
                var shearwalls = GetWalls(acdb.Database, _shearwallLayer, newRangePts, Transformer);
                var columns = GetColunms(acdb.Database, _columnLayer, newRangePts, Transformer);
                walls = walls.Union(dbDoors).Union(shearwalls).ToList();//已存在的门当做墙处理

                var wallSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
                var columnLines = new List<Line>();
                foreach (Polyline column in columns)
                {
                    var space = column.BufferPL(10)[0] as Polyline;
                    var objs = wallSpatialIndex.SelectCrossingPolygon(space);
                    if (objs.Count > 0)
                    {
                        columnLines.AddRange(column.GetAllLinesInPolyline());
                    }
                }
                walls.AddRange(columnLines);
                var wallObjs = new DBObjectCollection();
                walls.OfType<Curve>().ForEach(c =>
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
                var columnSpatialIndex = new ThCADCoreNTSSpatialIndex(columnLines.ToCollection());
                Dictionary<Line, Vector3d> ReasonableWalls = new Dictionary<Line, Vector3d>();
                foreach (Line line in mergedLines)
                {
                    bool isColumnLine = false;
                    {
                        var pl = line.ExtendLine(10).Buffer(10);
                        var objs = columnSpatialIndex.SelectCrossingPolygon(pl);
                        if (objs.Count > 0)
                        {
                            isColumnLine = true;
                        }
                    }
                    //过滤
                    if (line.Length < 210 || (isColumnLine ? _columnThickness : _wallThickness).Any(o => Math.Abs(line.Length - o) <= 10))
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
                                    var door = BuildDoor(reasonableWall.Key, line);
                                    if (!door.IsNull())
                                    {
                                        doors.Add(door);
                                    }
                                    ReasonableWalls.Remove(line);
                                }
                            }
                            else
                            {
                                if (_OtherwallSpacing.Any(o => Math.Abs(dis - o) <= 10))
                                {
                                    var door = BuildDoor(reasonableWall.Key, line);
                                    if (!door.IsNull())
                                    {
                                        doors.Add(door);
                                    }
                                    ReasonableWalls.Remove(line);
                                }
                            }
                        }
                    }
                    ReasonableWalls.Remove(reasonableWall.Key);
                }

                var allDoors = doors.Union(dbDoors.ToCollection());
                var doorSpatialIndex = new ThCADCoreNTSSpatialIndex(allDoors);
                var doorZones = ThConfigDataTool.GetDoorZones(acdb.Database, RangePts);
                Transformer.Transform(doorZones);
                var doorZoneSpatialIndex = new ThCADCoreNTSSpatialIndex(doorZones);
                var bufferDoorZones = new DBObjectCollection();
                while (doorZones.Count > 0)
                {
                    var door = doorZones[0] as Polyline;
                    var pl = door.Buffer(100)[0] as Polyline;
                    var objs = doorZoneSpatialIndex.SelectFence(pl);
                    objs.Remove(door);
                    if (objs.Count == 0)
                    {
                        bufferDoorZones.Add(door);
                    }
                    else if (objs.Count == 1)
                    {
                        var obj = objs[0] as Polyline;
                        if (doorZones.Contains(obj))
                        {
                            if (IsNeighborDoor(door, obj))
                            {
                                doorZones.Remove(obj);
                                bufferDoorZones.Add(door.Buffer(50).Union(obj.Buffer(50)).UnionPolygons().BufferPolygons(-50.0).OfType<Polyline>().OrderByDescending(o => o.Area).First().GetMinimumRectangle());
                            }
                            else
                            {
                                bufferDoorZones.Add(door);
                            }
                        }
                        else
                        {
                            bufferDoorZones.Add(door);
                        }
                    }
                    else
                    {
                        bufferDoorZones.Add(door);
                        //暂不支持
                    }
                    doorZones.Remove(door);
                }
                //var bufferDoorZones = doorZones.BufferPolygons(50.0).UnionPolygons().BufferPolygons(-50.0).OfType<Polyline>().Select(o => o.GetMinimumRectangle()).ToCollection();
                foreach (Polyline doorZone in bufferDoorZones)
                {
                    var objs = doorSpatialIndex.SelectCrossingPolygon(doorZone);
                    if (objs.Count == 0)
                    {
                        //此处有门块，但之前逻辑并没有识别出门，所以，我们要补一个门
                        var doorZoneLines = doorZone.GetAllLinesInPolyline();
                        if (doorZoneLines.Count == 4)
                        {
                            Tuple<Line, Line> group1 = (doorZoneLines[0], doorZoneLines[2]).ToTuple();
                            Tuple<Line, Line> group2 = (doorZoneLines[1], doorZoneLines[3]).ToTuple();

                            var pl = doorZone.BufferPL(100)[0] as Polyline;
                            wallObjs = spatialIndex.SelectCrossingPolygon(pl);
                            var wallLines = wallObjs.OfType<Line>().Where(o => o.Length > 49);

                            //group 1
                            Line line1, line2;
                            if (FindDoorByGroup(group1, wallLines, out line1, out line2) && (line1.Length < 1000 || line2.Length < 1000))
                            {
                                //doors.Add(BuildDoor(internalWall[0], internalWall[1]));
                                var door = BuildDoor(line1, line2);
                                if (!door.IsNull())
                                {
                                    doors.Add(door);
                                }
                            }
                            else if (FindDoorByGroup(group2, wallLines, out line1, out line2) && (line1.Length < 1000 || line2.Length < 1000))
                            {
                                //doors.Add(BuildDoor(internalWall[0], internalWall[1]));
                                var door = BuildDoor(line1, line2);
                                if (!door.IsNull())
                                {
                                    doors.Add(door);
                                }
                            }
                        }
                    }
                }
                Transformer.Reset(doors);
            }
        }

        private bool FindDoorByGroup(Tuple<Line, Line> group, IEnumerable<Line> wallLines, out Line line1, out Line line2)
        {
            line1 = line2 = null;
            var IsParallelWalls = wallLines.Where(o => o.IsParallelToEx(group.Item1));
            line1 = IsParallelWalls.Where(o => o.Distance(group.Item1) < 10).OrderBy(o => o.Length % 50).FirstOrDefault();
            if (line1.IsNull())
            {
                line1 = IsParallelWalls.Where(o => o.Distance(group.Item1) < 50).OrderByDescending(o => o.Length).FirstOrDefault();
            }
            if (!line1.IsNull())
            {
                line2 = IsParallelWalls.Where(o => o.Distance(group.Item2) < 10).OrderBy(o => o.Length % 50).FirstOrDefault();
                if (line2.IsNull())
                {
                    line2 = IsParallelWalls.Where(o => o.Distance(group.Item2) < 50).OrderByDescending(o => o.Length).FirstOrDefault();
                }
                if (line2.IsNull())
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private Polyline BuildDoor(Line line1, Line line2)
        {
            var minLine = line1.Length > line2.Length ? line2 : line1;
            var maxLine = line1.Length > line2.Length ? line1 : line2;
            var pt1 = minLine.StartPoint.GetProjectPtOnLine(maxLine.StartPoint, maxLine.EndPoint);
            var pt2 = minLine.EndPoint.GetProjectPtOnLine(maxLine.StartPoint, maxLine.EndPoint);
            if (pt1.IsPointOnLine(maxLine, 10))
            {
                if (pt2.IsPointOnLine(maxLine, 10))
                {
                    return CreatePolyFromPoints(new Point3d[] { minLine.StartPoint, pt1, pt2, minLine.EndPoint });
                }
                else
                {
                    var pt3 = maxLine.StartPoint.GetProjectPtOnLine(minLine.StartPoint, minLine.EndPoint);
                    if (pt3.IsPointOnLine(minLine, 10))
                    {
                        return CreatePolyFromPoints(new Point3d[] { minLine.StartPoint, pt1, maxLine.StartPoint, pt3 });
                    }
                    else
                    {
                        var pt4 = maxLine.EndPoint.GetProjectPtOnLine(minLine.StartPoint, minLine.EndPoint);
                        if (pt4.IsPointOnLine(minLine, 10))
                        {
                            return CreatePolyFromPoints(new Point3d[] { minLine.StartPoint, pt1, maxLine.EndPoint, pt4 });
                        }
                    }
                }
            }
            else
            {
                if (pt2.IsPointOnLine(maxLine, 10))
                {
                    var pt3 = maxLine.StartPoint.GetProjectPtOnLine(minLine.StartPoint, minLine.EndPoint);
                    if (pt3.IsPointOnLine(minLine, 10))
                    {
                        return CreatePolyFromPoints(new Point3d[] { minLine.EndPoint, pt2, maxLine.StartPoint, pt3 });
                    }
                    else
                    {
                        var pt4 = maxLine.EndPoint.GetProjectPtOnLine(minLine.StartPoint, minLine.EndPoint);
                        if (pt4.IsPointOnLine(minLine, 10))
                        {
                            return CreatePolyFromPoints(new Point3d[] { minLine.EndPoint, pt2, maxLine.EndPoint, pt4 });
                        }
                    }
                }
            }
            return null;
        }

        private Polyline CreatePolyFromPoints(Point3d[] points)
        {
            var poly = new Polyline()
            {
                Closed = true,
            };
            poly.CreatePolyline(new Point3dCollection(points));
            return poly;
        }

        private bool IsNeighborDoor(Polyline door1, Polyline door2)
        {
            var door1Lines = door1.GetAllLinesInPolyline();
            var door2Lines = door2.GetAllLinesInPolyline();
            var pts1 = door1.Vertices().Cast<Point3d>().ToList();
            var pts2 = door2.Vertices().Cast<Point3d>().ToList();
            foreach (Line line in door1Lines)
            {
                if (door2Lines.Any(o => ThGeometryTool.IsCollinearEx(o.StartPoint, o.EndPoint, line.StartPoint, line.EndPoint, 5)))
                {
                    var pts = pts1.Where(o => !line.IsPointOnLine(o, true, 5)).Union(pts2.Where(o => !line.IsPointOnLine(o, true, 5)));
                    var vectors = pts.Select(o => o - o.GetProjectPtOnLine(line.StartPoint, line.EndPoint));
                    if (vectors.All(o => o.IsParallelTo(vectors.First())))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private List<Curve> GetWalls(Database acdb, string wallLayer, Point3dCollection rangePts, ThMEPOriginTransformer transformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(acdb))
            {
                var wallobjs = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .Where(o => wallLayer.Equals(o.Layer))
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
