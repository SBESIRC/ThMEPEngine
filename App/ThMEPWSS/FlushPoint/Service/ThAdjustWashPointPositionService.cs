using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
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
        public double Width { get; set; }
        public double Length { get; set; }

        public ThAdjustWashPointPositionService(List<Polyline> columns,List<Polyline> parkingStalls,List<Entity> walls)
        {
            Width = 2500;  //2.5m
            Length = 5500; //5.0m-5.5m
            WallSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
            ColumnSpatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            ParkingStallSpatialIndex = new ThCADCoreNTSSpatialIndex(parkingStalls.ToCollection());
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
            for (int i = 0; i < column.NumberOfVertices; i++)
            {
                var lineSegment = column.GetLineSegmentAt(i);
                var dir = lineSegment.StartPoint.GetVectorTo(lineSegment.EndPoint).GetPerpendicularVector();
                var position = ThGeometryTool.GetMidPt(lineSegment.StartPoint, lineSegment.EndPoint);
                collector.Add(position);
                var extendPt = position + dir.GetNormal().MultiplyBy(5.0);
                if (column.Contains(extendPt))
                {
                    dir = dir.Negate();
                }
                if (CanLayout(position, dir))
                {
                    return position;
                }
            }
            return collector.Count > 0 ? collector[0] : null;
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
    }
}
