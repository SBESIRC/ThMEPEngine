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
        public static List<Curve> Merge(Polyline border, List<Line> centerLines)
        {
            var instance = new ThMergeLightCenterLines(border, centerLines);
            instance.Merge();
            return instance.Results;
        }
        private void Merge()
        {
            var auxiliaryLines = new List<List<Line>>();
            var mainLines = new List<List<Line>>();
            var laneLine = new ParkingLinesService();
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
