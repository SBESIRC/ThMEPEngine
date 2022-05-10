using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThAdjustWashPointPositionService
    {
        private ThCADCoreNTSSpatialIndex WallSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ColumnSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ParkingStallSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ObstacleSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex RoomSpatialIndex { get; set; }
        // 用于查询柱子旁边的墙
        private const double MaximumWallThickness = 2000;
        // 用于过滤面积很小的Polygon
        private const double SmallAreaTolerance = 1.0;
        // 用于过滤点位布在柱子上最小边的长度
        private const double CanLayoutEdgeMinimumLength = 20.0;
        /// <summary>
        /// 车位宽度 2.5m
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 车位长度 5.0m-5.5m
        /// </summary>
        public double Length { get; set; } 
        public double ColumnBufferLength { get; set; } // 柱子往外扩的长度
        public double WallDetectLength { get; set; } // 探测墙的长度

        public ThAdjustWashPointPositionService(
            List<Polyline> columns,
            List<Polyline> parkingStalls,
            List<Entity> walls,
            List<Entity> rooms,
            List<Entity> obstacles)
        {
            Width = 2500;  
            Length = 5500;
            ColumnBufferLength = 300.0;
            WallDetectLength = 100000;
            WallSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
            ColumnSpatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            ParkingStallSpatialIndex = new ThCADCoreNTSSpatialIndex(parkingStalls.ToCollection());
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(obstacles.ToCollection());
            RoomSpatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
        }

        public void Adjust(List<Point3d> washPoints)
        {
            for (int i = 0; i < washPoints.Count; i++)
            {
                var pt = washPoints[i];
                var square = ThDrawTool.CreateSquare(pt, 5.0);
                var objs = ColumnSpatialIndex.SelectCrossingPolygon(square);
                if (objs.Count > 0)
                {
                    var column = objs.Cast<Polyline>().OrderBy(o => o.Distance(pt)).First();
                    // 获取柱子所有边的朝向
                    var outerEdgeDirs = BuildEdgeOuterDirection(column);                    
                    washPoints[i] = Adjust(outerEdgeDirs); 
                }
            }
        }

        private List<Tuple<string, Point3d, Point3d, Vector3d>> BuildEdgeOuterDirection(Polyline column)
        {
            var results = new List<Tuple<string, Point3d, Point3d, Vector3d>>();
            var walls = GetNeibourWalls(column);
            var objs = GetCanLayoutAreas(column, walls);
            var polys = GetPolylines(objs);
            polys.ForEach(p =>
            {
                for (int i = 0; i < p.NumberOfVertices; i++)
                {
                    var lineSegment = p.GetLineSegmentAt(i);
                    if (lineSegment.Length < CanLayoutEdgeMinimumLength)
                    {
                        continue;
                    }
                    var dir = lineSegment.StartPoint.GetVectorTo(lineSegment.EndPoint).GetPerpendicularVector();
                    if (dir.IsCodirectionalTo(Vector3d.ZAxis) || dir.IsCodirectionalTo(Vector3d.ZAxis.Negate()))
                    {
                        continue;
                    }
                    var position = ThGeometryTool.GetMidPt(lineSegment.StartPoint, lineSegment.EndPoint);
                    var extendPt = position + dir.GetNormal().MultiplyBy(5.0);
                    if(!column.Contains(extendPt) && !IsIn(extendPt,walls) &&!IsOnWalls(extendPt,2.0))
                    {
                        // 布置点不能在柱子里，不能在墙里，不能在墙线上
                        results.Add(Tuple.Create(Guid.NewGuid().ToString(), lineSegment.StartPoint, lineSegment.EndPoint, dir));
                        continue;
                    }
                    // 换个方向
                    extendPt = position + dir.Negate().GetNormal().MultiplyBy(5.0);
                    if (!column.Contains(extendPt) && !IsIn(extendPt, walls) && !IsOnWalls(extendPt, 2.0))
                    {
                        // 布置点不能在柱子里，不能在墙里，不能在墙线上
                        results.Add(Tuple.Create(Guid.NewGuid().ToString(), lineSegment.StartPoint, lineSegment.EndPoint, dir));
                        continue;
                    }
                }
            });
            polys.ForEach(p => p.Dispose());
            return results.OrderByDescending(o=>o.Item2.DistanceTo(o.Item3)).ToList();
        }

        private List<Polyline> GetPolylines(DBObjectCollection objs)
        {
            return objs.OfType<Entity>().SelectMany(o =>
            {
                var results = new List<Polyline>();
                if (o is Polyline polyline)
                {
                    results.Add(polyline);
                }
                else if (o is MPolygon mPolygon)
                {
                    results.Add(mPolygon.Shell());
                    results.AddRange(mPolygon.Holes());
                }
                return results;
            }).ToList();
        }

        private DBObjectCollection GetCanLayoutAreas(Polyline column,DBObjectCollection walls)
        {
            if(walls.Count>0)
            {
                return Subtraction(column, walls);
            }
            else
            {
                var results = new DBObjectCollection();
                results.Add(column.Clone() as Polyline);
                return results;
            }
        }

        private DBObjectCollection GetNeibourWalls(Polyline column)
        {
            var length = GetMaximumLengthSegment(column);
            var bufferL = Math.Max(length, MaximumWallThickness);
            var objs = column.Buffer(bufferL, false);
            var polys = objs.OfType<Polyline>().Where(o => o.Area > 1.0).OrderBy(o => o.Area);
            return polys.Count() > 0 ? QueryWalls(polys.First()) : QueryWalls(column);
        }

        private DBObjectCollection Subtraction(Entity entity, DBObjectCollection objs)
        {
            var garbages = new DBObjectCollection();
            var polygon1s = Difference(entity, objs, false);
            garbages = garbages.Union(polygon1s);

            var polygon2s = MakeValid(polygon1s); //解决自交的Case
            garbages = garbages.Union(polygon2s);

            var polygon3s = Simplify(polygon2s); //合并重复线
            garbages = garbages.Union(polygon2s);

            var results = Normalize(polygon3s); //合并重复线

            garbages = garbages.Difference(results);
            garbages.MDispose();
            return results;
        }

        private DBObjectCollection Difference(Entity polygon,
            DBObjectCollection polygons,bool keepHole)
        {
            var results = ThCADCoreNTSEntityExtension.Difference(
                polygon, polygons, keepHole);            
            return Clean(results);
        }

        private DBObjectCollection Simplify(DBObjectCollection polygons)
        {            
            var simplifer = new ThPolygonalElementSimplifier();
            var results = simplifer.Simplify(polygons);            
            return Clean(results);
        }

        private DBObjectCollection Normalize(DBObjectCollection polygons)
        {
            var simplifer = new ThPolygonalElementSimplifier();
            var results = simplifer.Normalize(polygons);
            return Clean(results);
        }

        private DBObjectCollection Clean(DBObjectCollection objs)
        {
            var garbages = new DBObjectCollection();
            garbages = garbages.Union(objs);
            var results = RemoveDBpoints(objs); // 过滤 DBPoint
            results = results.FilterSmallArea(SmallAreaTolerance); //清除面积为零
            results = DuplicatedRemove(results); //去重
            garbages = garbages.Difference(results);
            garbages.MDispose();
            return results;
        }

        private DBObjectCollection MakeValid(DBObjectCollection polygons)
        {
            var simplifer = new ThPolygonalElementSimplifier();            
            var results =  simplifer.MakeValid(polygons);            
            return Clean(results);
        }

        private DBObjectCollection RemoveDBpoints(DBObjectCollection objs)
        {
            return objs.OfType<Entity>().Where(e => !(e is DBPoint)).ToCollection();
        }

        private DBObjectCollection DuplicatedRemove(DBObjectCollection objs)
        {
            return ThCADCoreNTSGeometryFilter.GeometryEquality(objs);
        }

        private DBObjectCollection QueryWalls(Entity polygon)
        {
            return WallSpatialIndex.SelectCrossingPolygon(polygon);
        }

        private double GetMaximumLengthSegment(Polyline poly)
        {
            double maxLength = 0.0;
            for(int i=0;i<poly.NumberOfVertices-1;i++)
            {
                var segmentType = poly.GetSegmentType(i);
                if (segmentType == SegmentType.Line)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    if(lineSeg.Length> maxLength)
                    {
                        maxLength = lineSeg.Length;
                    }
                }                
            }
            return maxLength;
        }      
        
        private bool IsIn(Point3d pt ,DBObjectCollection polygons)
        {
            return polygons.OfType<Entity>().Where(e => e.EntityContains(pt)).Any();
        }

        private bool IsOnWalls(Point3d pt,double length=1.0)
        {
            var outline = pt.CreateSquare(length);
            return QueryWalls(outline).Count>0;
        }

        private Point3d Adjust(List<Tuple<string, Point3d, Point3d, Vector3d>> outerEdgeDirs)
        {            
            // step1: 过滤没有与车位碰撞的边
            var noConflictParkingStalls = GetNotConflictToParkintStallEdges(outerEdgeDirs);
            if(noConflictParkingStalls.Count==1)
            {
                // 若经过每一步后只剩下一个可选边，则直接布在这边上
                var first = Query(outerEdgeDirs,noConflictParkingStalls).First();
                return first.Item2.GetMidPt(first.Item3);
            }
            else if(noConflictParkingStalls.Count==0)
            {
                // 若每一步后一条可选边都剩不下，则在上一步筛选后的基础上任选一边布置
                var first = outerEdgeDirs.First();
                return first.Item2.GetMidPt(first.Item3);
            }
            // step2: 过滤没有与障碍物碰撞的边
            var noConflictObstacles = GetNotConflictToObstaclesEdges(Query(outerEdgeDirs, noConflictParkingStalls));
            if(noConflictObstacles.Count==1)
            {
                // 若经过每一步后只剩下一个可选边，则直接布在这边上
                var first = Query(outerEdgeDirs, noConflictObstacles).First();
                return first.Item2.GetMidPt(first.Item3);
            }
            else if(noConflictObstacles.Count == 0)
            {
                // 若每一步后一条可选边都剩不下，则在上一步筛选后的基础上任选一边布置
                var first = Query(outerEdgeDirs, noConflictParkingStalls).First();
                return first.Item2.GetMidPt(first.Item3);
            }
            // step3: 剩下的根据距离墙较远的边排序
            var farestWallEdgeSorts = OrderByDistanceToNearestEdge(Query(outerEdgeDirs,noConflictObstacles),WallDetectLength);
            if (farestWallEdgeSorts.Count > 0)
            {
                // 若经过每一步后只剩下一个可选边，则直接布在这边上
                var first = farestWallEdgeSorts.First();
                return first.Item2.GetMidPt(first.Item3);
            }
            else
            {
                var first = outerEdgeDirs.First();
                return first.Item2.GetMidPt(first.Item3);
            }
        }

        private List<string> GetNotConflictToParkintStallEdges(List<Tuple<string, Point3d, Point3d, Vector3d>> edges)
        {
            var results = new List<string>();
            edges.ForEach(item =>
            {
                var checkArea = BuildCheckArea(item.Item2, item.Item3, item.Item4);
                var parkingStalls = FindParkingStalls(checkArea);
                if (parkingStalls.Count ==0)
                {
                    results.Add(item.Item1);
                }
                checkArea.Dispose();
            });
            return results;
        }

        private List<string> GetNotConflictToObstaclesEdges(List<Tuple<string, Point3d, Point3d, Vector3d>> edges)
        {
            var results = new List<string>();
            edges.ForEach(item =>
            {
                var checkArea = BuildCheckArea(item.Item2, item.Item3, item.Item4);
                var parkingStalls = FindObstacles(checkArea);
                if (parkingStalls.Count == 0)
                {
                    results.Add(item.Item1);
                }
                checkArea.Dispose();
            });
            return results;
        }

        private List<Tuple<string, Point3d, Point3d, Vector3d>> OrderByDistanceToNearestEdge(
            List<Tuple<string, Point3d, Point3d, Vector3d>> edges, double detectLength)
        {
            // 按照距离柱子边的长度，从小到大排序
            return edges.OrderBy(o => DistanceToNearestWallEdge(o.Item2.GetMidPt(o.Item3), o.Item4, detectLength)).ToList();
        }
        
        private List<Tuple<string, Point3d, Point3d, Vector3d>> Query(List<Tuple<string, Point3d, Point3d, Vector3d>> edges,List<string> ids)
        {
            return edges.Where(o => ids.Contains(o.Item1)).ToList();
        }
        private double DistanceToNearestWallEdge(Point3d pt,Vector3d dir,double findLength)
        {
            // 查找距离哪个墙的边最近
            var walls = FindWalls(pt, dir, findLength);
            if (walls.Count > 0)
            {
                var res = DistanceToNearestEdge(pt, walls);
                return res.HasValue ? res.Value : double.MaxValue;
            }
            return double.MaxValue;
        }

        private double DistanceToNearestRoomEdge(Point3d pt, Vector3d dir, double findLength)
        {
            // 查找距离哪个房间的边最近
            var rooms = FindRooms(pt, dir, findLength);
            if (rooms.Count > 0)
            {
                var res = DistanceToNearestEdge(pt, rooms);
                return res.HasValue?res.Value: double.MaxValue;
            }
            return double.MaxValue;
        }
        private double? DistanceToNearestEdge(Point3d pt, DBObjectCollection boundaries)
        {
            if (boundaries.Count == 0)
            {
                return null;
            }
            var edges = boundaries.Cast<Entity>().SelectMany(e => ToLines(e)).ToList();
            var disList = edges
                .Select(o => pt.DistanceTo(o.GetClosestPointTo(pt, false)));
            return disList.OrderBy(o => o).First();
        }
        private List<Line> ToLines(Entity entity)
        {
            //要设置分割长度TesslateLength
            var results = new List<Line>();
            if (entity is Polyline polyline)
            {
                results.AddRange(polyline.ToLines());
            }
            else if (entity is MPolygon mPolygon)
            {
                results.AddRange(mPolygon.Loops().SelectMany(l => l.ToLines()));
            }
            else if (entity is Circle circle)
            {
                results.AddRange(circle.Tessellate(5.0).ToLines());
            }
            else if (entity is Line line)
            {
                results.Add(line);
            }
            else
            {
                throw new NotSupportedException();
            }
            return results.Where(o=>o.Length>1.0).ToList();
        }

        private bool CanLayout(Point3d segSp,Point3d segEp, Vector3d dir)
        {
            var area = BuildCheckArea(segSp, segEp, dir);
            var parkingStalls  = FindParkingStalls(area);
            if(parkingStalls.Count>0)
            {
                return false;
            }
            var obstacles = FindObstacles(area);
            if (obstacles.Count > 0)
            {
                return false;
            }
            return true;
        }
        private bool CanLayout(Point3d position, Vector3d dir)
        {
            var targetPt = position + dir.GetNormal().MultiplyBy(Length);
            var rect = ThDrawTool.ToRectangle(position, targetPt, 1.0);
            var objs = ParkingStallSpatialIndex.SelectCrossingPolygon(rect);
            if(objs.Count>0)
            {
                return false;
            }
            objs = WallSpatialIndex.SelectCrossingPolygon(rect);
            if(objs.Count > 0)
            {
                return false;
            }
            return true;
        }
        private Point3dCollection BuildCheckArea(Point3d lineSp, Point3d lineEp, Vector3d dir,double innerDis=5.0)
        {
            var pts = new Point3dCollection();
            var vec = lineSp.GetVectorTo(lineEp);
            var length = vec.Length * 0.1;
            var newLineSp = lineSp + vec.GetNormal().MultiplyBy(length);
            var newLineEp = lineEp + vec.Negate().GetNormal().MultiplyBy(length);

            var pt1 = newLineSp + dir.GetNormal().MultiplyBy(ColumnBufferLength);
            var pt2 = newLineEp + dir.GetNormal().MultiplyBy(ColumnBufferLength);
            var pt3 = newLineEp - dir.GetNormal().MultiplyBy(innerDis);
            var pt4 = newLineSp - dir.GetNormal().MultiplyBy(innerDis);

            pts.Add(pt1);
            pts.Add(pt2);
            pts.Add(pt3);
            pts.Add(pt4);
            return pts;
        }

        private DBObjectCollection FindObstacles(Point3dCollection pts)
        {
            return ObstacleSpatialIndex.SelectCrossingPolygon(pts);
        }

        private DBObjectCollection FindParkingStalls(Point3dCollection pts)
        {
            return ParkingStallSpatialIndex.SelectCrossingPolygon(pts);
        }

        private DBObjectCollection FindWalls(Point3d pt, Vector3d dir,double findLength)
        {
            var targetPt = pt + dir.GetNormal().MultiplyBy(findLength); 
            var rect = ThDrawTool.ToRectangle(pt, targetPt, 1.0);
            return WallSpatialIndex.SelectCrossingPolygon(rect);
        }
        private DBObjectCollection FindRooms(Point3d pt, Vector3d dir, double findLength)
        {
            var targetPt = pt + dir.GetNormal().MultiplyBy(findLength);
            var rect = ThDrawTool.ToRectangle(pt, targetPt, 1.0);
            return RoomSpatialIndex.SelectCrossingPolygon(rect);
        }
    }
}
