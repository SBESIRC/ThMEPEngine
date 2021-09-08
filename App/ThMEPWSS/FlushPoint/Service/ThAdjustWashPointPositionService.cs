using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThAdjustWashPointPositionService
    {
        private ThCADCoreNTSSpatialIndex WallSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ColumnSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ParkingStallSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ObstacleSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex RoomSpatialIndex { get; set; }
        /// <summary>
        /// 车位宽度 2.5m
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 车位长度 5.0m-5.5m
        /// </summary>
        public double Length { get; set; }
        public double BufferLength { get; set; }

        public ThAdjustWashPointPositionService(
            List<Polyline> columns,
            List<Polyline> parkingStalls,
            List<Entity> walls,
            List<Entity> rooms,
            List<Entity> obstacles)
        {
            Width = 2500;  
            Length = 5500;
            BufferLength = 300.0;
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
                    var newPt = Adjust(column); 
                    if(newPt!=null)
                    {
                        washPoints[i] = newPt.Value;
                    }
                }
            }
        }
        private Point3d? Adjust(Polyline column)
        {
            var collector = new List<Point3d>();
            var validPtDict = new Dictionary<Point3d,Vector3d>();
            for (int i = 0; i < column.NumberOfVertices; i++)
            {
                var lineSegment = column.GetLineSegmentAt(i);
                if(lineSegment.Length<1e-4)
                {
                    continue;
                }
                var dir = lineSegment.StartPoint.GetVectorTo(lineSegment.EndPoint).GetPerpendicularVector();
                var position = ThGeometryTool.GetMidPt(lineSegment.StartPoint, lineSegment.EndPoint);
                collector.Add(position);
                var extendPt = position + dir.GetNormal().MultiplyBy(5.0);
                if (column.Contains(extendPt))
                {
                    dir = dir.Negate();
                }
                if (CanLayout(lineSegment.StartPoint, lineSegment.EndPoint, dir))
                {
                    validPtDict.Add(position, dir);
                }
            }
            if(validPtDict.Count == 0)
            {
                return collector.Count > 0 ? (Point3d?)collector[0] : null;
            }
            else if(validPtDict.Count == 1)
            {
                return validPtDict.First().Key;
            }
            else
            {
                return validPtDict
                    .OrderByDescending(o => DistanceToNearestEdge(o.Key, o.Value, 5000000)) //尽可能多选点
                    .First().Key;
            }
        }

        private double DistanceToNearestEdge(Point3d pt,Vector3d dir,double findLength)
        {
           var walls =  FindWalls(pt, dir, findLength);
            var rooms = FindRooms(pt, dir, findLength);
            if (walls.Count==0 && rooms.Count==0)
            {
                return double.MaxValue;
            }
            var boundaries = walls.Union(rooms);
            var edges = boundaries.Cast<Entity>().SelectMany(e => ToLines(e)).ToList();
            var disList = edges
                .Select(o => pt.DistanceTo(o.GetClosestPointTo(pt,false)));
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
            var area = CheckArea(segSp, segEp, dir);
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
        private Point3dCollection CheckArea(Point3d lineSp, Point3d lineEp, Vector3d dir)
        {
            var pts = new Point3dCollection();
            var vec = lineSp.GetVectorTo(lineEp);
            var length = vec.Length * 0.1;
            var newLineSp = lineSp + vec.GetNormal().MultiplyBy(length);
            var newLineEp = lineEp + vec.Negate().GetNormal().MultiplyBy(length);

            var pt1 = newLineSp + dir.GetNormal().MultiplyBy(BufferLength);
            var pt2 = newLineEp + dir.GetNormal().MultiplyBy(BufferLength);
            var pt3 = newLineEp - dir.GetNormal().MultiplyBy(50.0);
            var pt4 = newLineSp - dir.GetNormal().MultiplyBy(50.0);

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
