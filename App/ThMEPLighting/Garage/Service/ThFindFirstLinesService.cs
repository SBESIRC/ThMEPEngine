using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindFirstLinesService
    {
        private List<ThWireOffsetData> WireOffsetDatas { get; set; }
        private List<Tuple<Curve, Curve, Curve>> OffsetCurves { get; set; }
        private double OffsetDis { get; set; }
        private ThFindFirstLinesService(List<Tuple<Curve, Curve, Curve>> offsetCurves,double offsetDis)
        {
            OffsetCurves = offsetCurves;
            OffsetDis = offsetDis;
            WireOffsetDatas = new List<ThWireOffsetData>();
        }
        public static List<ThWireOffsetData> Find(
            List<Tuple<Curve, Curve, Curve>> offsetCurves, double offsetDis)
        {
            var instance = new ThFindFirstLinesService(offsetCurves, offsetDis);
            instance.Find();
            return instance.WireOffsetDatas;
        }
        private void Find()
        {
            OffsetCurves.ForEach(o =>
            { 
                var pairs=Find(o);
                pairs.ForEach(p =>
                {
                    var wireData = new ThWireOffsetData
                    {
                        Center = p.Item1,
                        First = p.Item2,
                        Second = p.Item3,
                        IsDX = true
                    };
                    WireOffsetDatas.Add(wireData);
                });
            });
        }
        private List<Tuple<Line, Line, Line>> Find(Tuple<Curve, Curve, Curve> pairs)
        {
            var groups = new List<Tuple<Line, Line, Line>>();
            if (pairs.Item1 is Line line)
            {
                var centerLine = line.Normalize();
                var leftLine = (pairs.Item2 as Line).Normalize();
                var rightLine = (pairs.Item3 as Line).Normalize();
                groups.Add(Tuple.Create(centerLine.Normalize(), leftLine, rightLine));
            }
            else if (pairs.Item1 is Polyline polyline)
            {
                var centerLines = Expode(polyline);
                var leftLines = Expode(pairs.Item2 as Polyline);
                var rightLines = Expode(pairs.Item3 as Polyline);
                centerLines.ForEach(o =>
                    {
                        var first = FindSideLine(o, leftLines);
                        var second = FindSideLine(o, rightLines);
                        if (first != null && second!=null)
                        {
                            groups.Add(Tuple.Create(o.Normalize(), first.Normalize(), second.Normalize()));
                        }
                    });
            }
            else
            {
                throw new NotSupportedException();
            }
            return groups;
        }
        private List<Line> Expode(Polyline polyline)
        {
            var results = new List<Line>();
            var objs = new DBObjectCollection();
            polyline.Explode(objs);
            objs.Cast<Line>().ForEach(o => results.Add(o.Normalize()));
            return results;
        }
        private Line FindSideLine(Line center,List<Line> lines)
        {
            var spatialIndex = ThGarageLightUtils.BuildSpatialIndex(lines);
            var midPt = ThGeometryTool.GetMidPt(center.StartPoint, center.EndPoint);
            var centerVec = center.StartPoint.GetVectorTo(center.EndPoint).GetNormal();
            var perpendVec = centerVec.GetPerpendicularVector();
            var upPt = midPt + perpendVec.MultiplyBy(OffsetDis);
            var downPt = midPt - perpendVec.MultiplyBy(OffsetDis);
            var upLines = FilterLines(upPt, centerVec, spatialIndex);
            var downLines = FilterLines(downPt, centerVec, spatialIndex);
            if(upLines.Count==1 && downLines.Count==0)
            {
                return upLines[0];
            }
            else if(upLines.Count == 0 && downLines.Count == 1)
            {
                return downLines[0];
            }
            else
            {
                return null;
            }
        }
        private List<Line> FilterLines(Point3d pt, Vector3d centerVec, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var outline = ThDrawTool.CreateSquare(pt, 5.0);
            var objs = spatialIndex.SelectCrossingPolygon(outline);
            return objs
                .Cast<Line>()
                .Where(o => ThGeometryTool.IsParallelToEx(
                    centerVec, o.StartPoint.GetVectorTo(o.EndPoint)))
                .ToList();
        }
    }
}
