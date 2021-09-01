using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThFilterTTypeCenterLineService: ThFilterShortLinesService
    {
        //对于较短直线,一端未连接,一端在T型
        private ThFilterTTypeCenterLineService(List<Line> lines,double limitDistance)
            :base(lines, limitDistance)
        {            
        }
        public static List<Line> Filter(List<Line> lines, double limitDistance)
        {
            var instance = new ThFilterTTypeCenterLineService(lines, limitDistance);
            instance.Filter();
            return instance.Results;
        }

        protected override void Filter()
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
                    if(startLinks.Count==0 && endLinks.Count>=2)
                    {      
                        //起始端未连接任何线，末端有连接
                        //末端连接的线中有一对相连，共线，且不重叠
                        if(JudgeLinksIsValid(endLinks))
                        {
                            //丢弃
                        }
                        else
                        {
                            Results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else if(startLinks.Count >= 2 && endLinks.Count == 0)
                    {
                        //末端未连接任何线，起始端有连接
                        //起始端连接的线中有一对相连，共线，且不重叠
                        if (JudgeLinksIsValid(startLinks))
                        {
                            //丢弃
                        }
                        else
                        {
                            Results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else
                    {
                        Results.Add(new Line(o.StartPoint, o.EndPoint));
                    }
                }
            });
        }
        private bool JudgeLinksIsValid(List<Line> lines)
        {
            //灯线一段连接的线多余两根，且其中有两根灯线相连且外角在一定范围之内
            //符合T型的样式
            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (ThGarageLightUtils.IsLink(lines[i], lines[j]) && 
                        ThGarageUtils.IsLessThan45Degree(lines[i].StartPoint, 
                        lines[i].EndPoint, lines[j].StartPoint,lines[j].EndPoint))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
