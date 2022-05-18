using System.Collections.Generic;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThFilterIsolatedLineService : ThFilterShortLinesService
    {
        //过滤较短直线（两端未连接）
        private ThFilterIsolatedLineService(List<Line> lines,double limitDistance)
            :base(lines, limitDistance)
        {
        }
        public static List<Line> Filter(List<Line> lines, double limitDistance)
        {
            var instance = new ThFilterIsolatedLineService(lines, limitDistance);
            instance.Filter();
            return instance.Results;
        }
        protected override void Filter()
        {
            Lines.ForEach(o =>
            {
                if (o.Length >= LimitDistance)
                {
                    Results.Add(o);
                }
                else
                {
                    var startLinks = QueryInstance.Query(o.StartPoint,
                        ThGarageLightCommon.RepeatedPointDistance);
                    startLinks.Remove(o);
                    var endLinks = QueryInstance.Query(o.EndPoint,
                       ThGarageLightCommon.RepeatedPointDistance);
                    endLinks.Remove(o);
                    if(startLinks.Count > 0 || endLinks.Count > 0)
                    {
                        Results.Add(o);
                    }
                }
            });
        }
    }
}
