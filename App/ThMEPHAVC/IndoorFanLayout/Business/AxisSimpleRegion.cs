using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AxisSimpleRegion
    {
        List<Line> _axisLines;
        List<Arc> _axisArcs;
        List<Polyline> _allColumns;
        AxisLineSimpleRegion lineSimpleRegion;
        AxisArcSimpleRegion arcSimpleRegion;
        public AxisSimpleRegion(List<Curve> curves, List<Polyline> columns) 
        {
            lineSimpleRegion = new AxisLineSimpleRegion();
            arcSimpleRegion = new AxisArcSimpleRegion();
            _axisLines = new List<Line>();
            _axisArcs = new List<Arc>();
            _allColumns = new List<Polyline>();
            if (null != curves && curves.Count > 0) 
            {
                //清除近乎零长度的对象（length≤10mm（梁线为40））
                //Z值归零（当直线夹点Z值不为零时需要处理）
                foreach (var curve in curves)
                {
                    if (curve.GetLength() < 10)
                        continue;
                    if (curve is Line line)
                    {
                        var sp = line.StartPoint;
                        var ep = line.EndPoint;
                        sp = new Point3d(sp.X, sp.Y, 0);
                        ep = new Point3d(ep.X, ep.Y, 0);
                        _axisLines.Add(new Line(sp, ep));
                    }
                    else if (curve is Arc arc)
                    {
                        var center = arc.Center;
                        center = new Point3d(center.X, center.Y, 0);
                        _axisArcs.Add(new Arc(center, arc.Radius, arc.StartAngle, arc.EndAngle));
                    }
                }
            }
            if (null != columns && columns.Count > 0)
            {
                foreach (var column in columns)
                    _allColumns.Add(column);
            }
        }
        public List<DivisionArea> AxisSimpleResults(double mergeSpacing, double findColumnTolerance, List<AreaRegionType> areaRequire) 
        {
            var axisLineGroup = new AxisLineGroup(_axisLines, _axisArcs);
            var axisGroups = axisLineGroup.GetLineGroups();

            return GroupLineToRegion(axisGroups,mergeSpacing,findColumnTolerance);
        }
        public List<DivisionArea> GroupLineToRegion(List<AxisGroupResult> lineGroupResults, double mergeSpacing,double findColumnTolerance) 
        {
            var results = new List<DivisionArea>();
            foreach (var group in lineGroupResults) 
            {
                var areas = new List<DivisionArea>();
                if (group.IsArc)
                {
                    areas = arcSimpleRegion.AxisRegionResult(group, mergeSpacing);
                }
                else 
                {
                    areas = lineSimpleRegion.AxisRegionResult(group, mergeSpacing);
                }
                if (areas == null || areas.Count < 1)
                    continue;
                results.AddRange(areas);
            }
            return results;
        }
    }
}
