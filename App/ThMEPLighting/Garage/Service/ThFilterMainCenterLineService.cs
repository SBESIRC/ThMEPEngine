using System.Linq;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThFilterMainCenterLineService: ThFilterShortLinesService
    {
        //对于较短直线,一端未连接,一端连着另一根共线的直线，且连接处有分支线
        private ThFilterMainCenterLineService(List<Line> lines,double limitDistance)
            :base(lines, limitDistance)
        {
        }
        public static List<Line> Filter(List<Line> lines, double limitDistance)
        {
            var instance = new ThFilterMainCenterLineService(lines, limitDistance);
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
                    if(startLinks.Count == 0 && endLinks.Count >= 2)
                    {
                        //判断末端有连接的共线线，且还有分支线，则丢弃
                        if(FindCollinears(o,endLinks) && FindUnCollinears(o,endLinks))
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
                        //判断起始端有连接的共线线，且还有分支线，则舍弃
                        if (FindCollinears(o, startLinks) && FindUnCollinears(o, startLinks))
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
        private bool FindCollinears(Line line, List<Line> links)
        {
            links.Remove(line);
            return links
                .Where(o => line.IsCollinearLinkAndNotOverlap(o))
                .Any();
        }
        private bool FindUnCollinears(Line line, List<Line> links)
        {
            links.Remove(line);
            return links
                .Where(o => !ThGeometryTool.IsCollinearEx(line.StartPoint,line.EndPoint,o.StartPoint,o.EndPoint))
                .Any();
        }
    }
}
