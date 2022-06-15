using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class LineOnLine
    {
        public static Line LineIsOnLine(Line valve, List<Line> pipeLineList)
        {
            foreach(var pipe in pipeLineList)
            {
                var tolerance = 10;
                var f1 = pipe.PointOnLine(valve.StartPoint, false, tolerance);
                var f2 = pipe.PointOnLine(valve.EndPoint, false, tolerance);
                if (f1 && f2)
                {
                    return pipe;
                }
            }
            return new Line();
        }

        public static void LineSplit(Line valve, Line pipe, List<Line> pipeLineList)
        {
            pipeLineList.Remove(pipe);
            pipeLineList.Add(valve);
            if (valve.StartPoint.DistanceTo(pipe.StartPoint) < valve.EndPoint.DistanceTo(pipe.StartPoint))
            {
                pipeLineList.Add(new Line(pipe.StartPoint, valve.StartPoint));
                pipeLineList.Add(new Line(pipe.EndPoint, valve.EndPoint));
            }
            else
            {
                pipeLineList.Add(new Line(pipe.StartPoint, valve.EndPoint));
                pipeLineList.Add(new Line(pipe.EndPoint, valve.StartPoint));
            }
        }
    }
}
