using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThShortCenterLineFilter
    {
        private double LimitDistance { get; set; } //可布灯的边的最小长度
        public double OffsetDistance { get; set; } = 0; // 如果是双排，中心线网两边偏的长度

        public ThShortCenterLineFilter(double limitDistance,double offsetDistance)
        {
            LimitDistance = limitDistance;
            OffsetDistance = offsetDistance;
        }

        public List<Line> Filter(List<Line> centers)
        {
            var results = centers.Select(o=>o).ToList();
            while (true)
            {
                var unValid = FindUnValidLine(results);
                if (unValid == null)
                {
                    break;
                }
                else
                {
                    results.Remove(unValid);
                }
            }
            return results; 
        }

        private Line FindUnValidLine(List<Line> lines)
        {
            var queryInstance = ThQueryLineService.Create(lines);
            foreach (Line line in lines)
            {
                var startLinks = queryInstance.Query(line.StartPoint,
                        ThGarageLightCommon.RepeatedPointDistance);
                startLinks.Remove(line);
                var endLinks = queryInstance.Query(line.EndPoint,
                   ThGarageLightCommon.RepeatedPointDistance);
                endLinks.Remove(line);
                if (startLinks.Count == 0 && endLinks.Count > 0)
                {
                    if(endLinks.Where(o=>!IsValid(line,o)).Any())
                    {
                        return line;
                    }
                }
                else if (endLinks.Count == 0 && startLinks.Count > 0)
                {
                    if (startLinks.Where(o => !IsValid(line, o)).Any())
                    {
                        return line;
                    }
                }
                else if(startLinks.Count == 0 && endLinks.Count == 0)
                {
                    if(line.Length<LimitDistance)
                    {
                        return line;
                    }
                }
            }
            return null;
        }
        private bool IsValid(Line source,Line target)
        {
            /*               
             *          |\
             *          | \
             *          |  \
             *          |   \(source)
             *          |
             *       (target)
             */
            var ang = source.LineDirection().GetAngleTo(target.LineDirection());
            double l = OffsetDistance / Math.Sin(ang);
            return (source.Length - l) >= LimitDistance;
        }
    }
}
