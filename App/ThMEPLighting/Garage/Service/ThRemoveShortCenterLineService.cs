using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThRemoveShortCenterLineService
    {
        //对于较短直线,一端未连接,一端在T型、十字处
        public List<Line> Results { get; set; }
        public List<Line> Lines { get; set; }
        public double LimitDistance { get; set; }
        private ThQueryLineService QueryInstance { get; set; }
        private ThRemoveShortCenterLineService(List<Line> lines,double limitDistance)
        {
            Lines = lines;
            LimitDistance = limitDistance;
            Results = new List<Line>();
            QueryInstance = ThQueryLineService.Create(Lines);
        }
        public static List<Line> Remove(List<Line> lines, double limitDistance)
        {
            var instance = new ThRemoveShortCenterLineService(lines, limitDistance);
            instance.Remove();
            return instance.Results;
        }
        private void Remove()
        {
            Lines.ForEach(o =>
            {
                if (o.Length >= LimitDistance)
                {
                    Results.Add(new Line(o.StartPoint, o.EndPoint));
                }
                else
                {
                    var startLinks = QueryInstance.Query(o.StartPoint,
                        ThGarageLightCommon.RepeatedPointDistance);
                    startLinks.Remove(o);
                    var endLinks = QueryInstance.Query(o.EndPoint,
                       ThGarageLightCommon.RepeatedPointDistance);
                    endLinks.Remove(o);
                    if (startLinks.Count>0 && endLinks.Count>0)
                    {
                        Results.Add(new Line(o.StartPoint, o.EndPoint));
                    }
                }
            });
        }
    }
}
