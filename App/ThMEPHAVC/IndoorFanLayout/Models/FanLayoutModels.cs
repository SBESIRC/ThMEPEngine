using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;

namespace ThMEPHVAC.IndoorFanLayout.Models
{
    class DivisionRoomArea
    {
        public DivisionArea divisionArea { get; }
        public DivisionRoomArea(DivisionArea division)
        {
            this.divisionArea = division;
            this.RoomLayoutAreas = new List<Polyline>();
            this.RealIntersectAreas = new List<Polyline>();
            this.FanLayoutAreaResult = new List<DivisionLayoutArea>();
        }
        public string GroupId { get; set; }
        public string UscGroupId { get; set; }
        public Vector3d GroupDir { get; set; }
        public double NeedLoad { get; set; }
        public double RealLoad { get; set; }
        public int NeedFanCount { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int GetFanCount()
        {
            int i = 0;
            if (null == this.FanLayoutAreaResult || this.FanLayoutAreaResult.Count < 1)
                return i;
            foreach (var item in this.FanLayoutAreaResult)
            {
                i += item.FanLayoutResult.Count;
            }
            return i;
        }
        public List<Polyline> RealIntersectAreas { get; }
        public List<Polyline> RoomLayoutAreas { get; }
        public List<DivisionLayoutArea> FanLayoutAreaResult { get; }
    }
    class DivisionLayoutArea
    {
        public Vector3d LayoutDir { get; set; }
        public int RowId { get; set; }
        public int ColumnId { get; set; }
        public List<Polyline> LayoutAreas { get; }
        public List<FanLayoutRect> FanLayoutResult { get; }
        public DivisionLayoutArea(List<Polyline> polylines)
        {
            this.LayoutAreas = new List<Polyline>();
            this.FanLayoutResult = new List<FanLayoutRect>();
            if (null == polylines)
                return;
            foreach (var item in polylines)
            {
                if (item == null || item.Area < 100)
                    continue;
                this.LayoutAreas.Add(item);
            }
        }
    }
    class FanLayoutRect
    {
        public string FanId { get; }
        public string FanLayoutName { get; set; }
        public FanLayoutRect(Polyline fanPolyline, double width,Vector3d lengthDir)
        {
            this.FanId = Guid.NewGuid().ToString();
            this.FanPolyLine = fanPolyline;
            this.LengthDirctor = lengthDir;
            this.InnerVentRects = new List<FanInnerVentRect>();
            this.LengthLines = new List<Line>();
            this.CenterPoint = IndoorFanCommon.PolylinCenterPoint(fanPolyline);
            var lines = IndoorFanCommon.GetPolylineCurves(fanPolyline);
            foreach (var item in lines)
            {
                if (item is Line line)
                {
                    if (Math.Abs(line.CurveDirection().DotProduct(lengthDir)) > 0.5)
                    {
                        this.LengthLines.Add(line);
                    }
                }
            }
            this.Length = this.LengthLines.First().Length;
            this.Width = width;
        }
        public List<Line> LengthLines { get; }
        public double Length { get; }
        public double Width { get; }
        public Polyline FanPolyLine { get; }
        public Point3d CenterPoint { get; set; }
        public Vector3d LengthDirctor { get; }
        public Vector3d FanDirection { get; set; }
        public List<FanInnerVentRect> InnerVentRects { get; }
    }
    class FanInnerVentRect
    {
        public string VentId { get; }
        public Polyline VentPolyline { get; }
        public Point3d CenterPoint { get; }
        public FanInnerVentRect(Polyline polyline) 
        {
            this.VentId = Guid.NewGuid().ToString();
            this.VentPolyline = polyline;
            this.CenterPoint = IndoorFanCommon.PolylinCenterPoint(polyline);
        }
        
    }
}
