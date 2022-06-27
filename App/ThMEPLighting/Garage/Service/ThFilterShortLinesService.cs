using System;
using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThFilterShortLinesService
    {
        private List<Line> Lines { get; set; }
        private double LimitDistance { get; set; }
        private ThQueryLineService QueryInstance { get; set; }
        public ThFilterShortLinesService(List<Line> lines, double limitDistance)
        {
            Lines = lines;
            LimitDistance = limitDistance;
            QueryInstance = ThQueryLineService.Create(Lines);
        }
        /// <summary>
        /// 过滤孤立的短线
        /// </summary>
        /// <returns></returns>
        public List<Line> FilterIsolatedLine()
        {
            var results = new List<Line>();
            Lines.ForEach(o =>
            {
                if (o.Length >= LimitDistance)
                {
                    results.Add(o);
                }
                else
                {
                    var startLinks = QueryInstance.Query(o.StartPoint,
                        ThGarageLightCommon.RepeatedPointDistance);
                    startLinks.Remove(o);
                    var endLinks = QueryInstance.Query(o.EndPoint,
                       ThGarageLightCommon.RepeatedPointDistance);
                    endLinks.Remove(o);
                    if (startLinks.Count > 0 || endLinks.Count > 0)
                    {
                        results.Add(o);
                    }
                }
            });
            return results;
        }
        /// <summary>
        /// 对于较短直线,一端未连接,一端连着另一根共线的直线，且连接处有分支线
        /// </summary>
        /// <returns></returns>
        public List<Line> FilterMainCenterLine()
        {
            /*
             *    (a)      (b)
             *  -----|-------------
             *       |
             *       | (c)
             *       |
             *   a 太端，需要过滤
             */
            var results = new List<Line>();
            Lines.ForEach(o =>
            {
                if (o.Length >= LimitDistance)
                {
                    results.Add(new Line(o.StartPoint, o.EndPoint));
                }
                else
                {
                    var startLinks = QueryInstance.Query(o.StartPoint,
                        ThGarageLightCommon.RepeatedPointDistance);
                    startLinks.Remove(o);
                    var endLinks = QueryInstance.Query(o.EndPoint,
                       ThGarageLightCommon.RepeatedPointDistance);
                    endLinks.Remove(o);
                    if (startLinks.Count == 0 && endLinks.Count >= 2)
                    {
                        //判断末端有连接的共线线，且还有分支线，则丢弃
                        if (FindCollinears(o, endLinks) && FindUnCollinears(o, endLinks))
                        {
                            //丢弃
                        }
                        else
                        {
                            results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else if (startLinks.Count >= 2 && endLinks.Count == 0)
                    {
                        //判断起始端有连接的共线线，且还有分支线，则舍弃
                        if (FindCollinears(o, startLinks) && FindUnCollinears(o, startLinks))
                        {
                            //丢弃
                        }
                        else
                        {
                            results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else
                    {
                        results.Add(new Line(o.StartPoint, o.EndPoint));
                    }
                }
            });
            return results;
        }
        /// <summary>
        /// 对于较短直线,一端未连接,一端在T型
        /// </summary>
        /// <returns></returns>
        public List<Line> FilterTTypeCenterLine()
        {
            /*
             *         |
             *         |
             *         |
             *         |--（a太短）
             *         |
             *         |
             *         |
             */
            var results = new List<Line>();
            Lines.ForEach(o =>
            {
                if (o.Length >= LimitDistance)
                {
                    results.Add(new Line(o.StartPoint, o.EndPoint));
                }
                else
                {
                    var startLinks = QueryInstance.Query(o.StartPoint,
                        ThGarageLightCommon.RepeatedPointDistance);
                    startLinks.Remove(o);
                    var endLinks = QueryInstance.Query(o.EndPoint,
                       ThGarageLightCommon.RepeatedPointDistance);
                    endLinks.Remove(o);
                    if (startLinks.Count == 0 && endLinks.Count >= 2)
                    {
                        //起始端未连接任何线，末端有连接
                        //末端连接的线中有一对相连，共线，且不重叠
                        if (JudgeLinksIsValid(endLinks))
                        {
                            //丢弃
                        }
                        else
                        {
                            results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else if (startLinks.Count >= 2 && endLinks.Count == 0)
                    {
                        //末端未连接任何线，起始端有连接
                        //起始端连接的线中有一对相连，共线，且不重叠
                        if (JudgeLinksIsValid(startLinks))
                        {
                            //丢弃
                        }
                        else
                        {
                            results.Add(new Line(o.StartPoint, o.EndPoint));
                        }
                    }
                    else
                    {
                        results.Add(new Line(o.StartPoint, o.EndPoint));
                    }
                }
            });
            return results;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Line> FilterElbowCenterLine()
        {
            var results  = new List<Line>();
            Lines.ForEach(o =>
            {
                if (o.Length >= LimitDistance)
                {
                    results.Add(new Line(o.StartPoint, o.EndPoint));
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
                    if (startLinks.Count == 0 && endLinks.Count == 1)
                    {
                        //起始端未连接任何线，末端有连接
                        //末端连接的线只有一根，且有角度
                        if (JudgeLinksIsElbow(o, endLinks[0]))
                        {
                            //丢弃
                        }
                        else
                        {
                            results.Add(o);
                        }
                    }
                    else if (startLinks.Count == 1 && endLinks.Count == 0)
                    {
                        //末端未连接任何线，起始端有连接
                        //起始端连接的线中有一对相连，共线，且不重叠
                        if (JudgeLinksIsElbow(o, startLinks[0]))
                        {
                            //丢弃
                        }
                        else
                        {
                            results.Add(o);
                        }
                    }
                    else
                    {
                        results.Add(o);
                    }
                }
            });
            return results;
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
                .Where(o => !ThGeometryTool.IsCollinearEx(line.StartPoint, line.EndPoint, o.StartPoint, o.EndPoint))
                .Any();
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
                        lines[i].EndPoint, lines[j].StartPoint, lines[j].EndPoint))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool JudgeLinksIsElbow(Line first, Line second)
        {
            var firstVec = first.StartPoint.GetVectorTo(first.EndPoint);
            var secondVec = second.StartPoint.GetVectorTo(second.EndPoint);
            if (firstVec.IsParallelToEx(secondVec))
            {
                return false;
            }
            return true;
        }
    }  
}
