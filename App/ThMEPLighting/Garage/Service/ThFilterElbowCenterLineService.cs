using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThFilterElbowCenterLineService : ThFilterShortLinesService
    {
        //对于较短直线,一端未连接,一端在T型
        private ThFilterElbowCenterLineService(List<Line> lines,double limitDistance)
            :base(lines, limitDistance)
        {            
        }
        public static List<Line> Filter(List<Line> lines, double limitDistance)
        {
            var instance = new ThFilterElbowCenterLineService(lines, limitDistance);
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
                    //一端未连接，一端连接线
                    if(startLinks.Count==0 && endLinks.Count==1)
                    {      
                        //起始端未连接任何线，末端有连接
                        //末端连接的线只有一根，且有角度
                        if(JudgeLinksIsElbow(o,endLinks[0]))
                        {
                            //丢弃
                        }
                        else
                        {
                            Results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else if(startLinks.Count ==1 && endLinks.Count == 0)
                    {
                        //末端未连接任何线，起始端有连接
                        //起始端连接的线中有一对相连，共线，且不重叠
                        if (JudgeLinksIsElbow(o,startLinks[0]))
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
        private bool JudgeLinksIsElbow(Line first,Line second)
        {
            var firstVec = first.StartPoint.GetVectorTo(first.EndPoint);
            var secondVec = second.StartPoint.GetVectorTo(second.EndPoint);
            if(firstVec.IsParallelToEx(secondVec))
            {
                return false;
            }
            return true;
        }
    }
}
