using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;

namespace ThMEPLighting.Garage.Service
{
    public class ThMergeLightCenterLines
    {
        private List<Curve> Results { get; set; }
        private Polyline Border { get; set; }
        private List<Line> CenterLines { get; set; }
        private ThMergeLightCenterLines(Polyline border,List<Line> centerLines)
        {
            Border = border;
            CenterLines = centerLines;
            Results = new List<Curve>();
        }
        public static List<Curve> Merge(Polyline border, List<Line> centerLines,double mergeRange)
        {
            var instance = new ThMergeLightCenterLines(border, centerLines);
            instance.Merge(mergeRange);
            return instance.Results;
        }
        private void Merge(double mergeRange)
        {
            var auxiliaryLines = new List<List<Line>>();
            var mainLines = new List<List<Line>>();
            var laneLine = new ParkingLinesService();
            laneLine.parkingLineTolerance = mergeRange;
            //目前会将传入的线延长2mm
            mainLines = laneLine.CreateNodedParkingLines(Border, CenterLines, out auxiliaryLines);
            mainLines.ForEach(o =>
            {
                if(o.Count==1)
                {
                    Results.Add(o[0]);
                }
                else if (o.Count > 1)
                {
                    var polyline = laneLine.CreateParkingLineToPolylineByTol(o);
                    Results.Add(polyline);
                }                
            });
            var simplifyCurves = new DBObjectCollection();
            Results.ForEach(x =>
            {
                if (x is Polyline polyline)
                {
                    var objs = ThLaneLineEngine.Simplify(new DBObjectCollection() { x });
                    simplifyCurves.Add(objs[0] as Curve);
                }
                else
                {
                    simplifyCurves.Add(x);
                }
            });
            var lines = ThLaneLineUnionEngine.Union(simplifyCurves);
            lines = ThLaneLineExtendEngine.Extend(lines);
            lines = ThLaneLineMergeExtension.Merge(lines);
            mainLines = laneLine.CreateNodedParkingLines(Border, CenterLines, out auxiliaryLines);
            mainLines.ForEach(o =>
            {
                if (o.Count == 1)
                {
                    Results.Add(o[0]);
                }
                else if (o.Count > 1)
                {
                    var polyline = laneLine.CreateParkingLineToPolyline(o);
                    Results.Add(polyline);
                }
            });

            auxiliaryLines.ForEach(o =>
            {
                if(o.Count==1)
                {
                    Results.Add(o[0]);
                }
                else
                {
                    var polyline = laneLine.CreateParkingLineToPolyline(o);
                    Results.Add(polyline);
                }                           
            });
        }
    }
}
