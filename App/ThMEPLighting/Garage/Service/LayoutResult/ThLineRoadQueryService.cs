using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPEngineCore.CAD;
using ThCADExtension;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 传入的线不能重叠、重复...
    /// 理想的状态是首尾连接
    /// </summary>
    public class ThLineRoadQueryService
    {
        private List<Line> Lines { get; set; }
        private ThQueryLineService LineQuery { get; set; }
        private double EnvelopeLength { get; set; }

        public ThLineRoadQueryService(List<Line> lines)
        {
            Lines = lines;
            EnvelopeLength = ThGarageLightCommon.RepeatedPointDistance;
            LineQuery = ThQueryLineService.Create(Lines);
        }
        /// <summary>
        /// 十字
        /// </summary>
        /// <returns></returns>
        public List<List<Line>> GetCross()
        {
            var results = new List<List<Line>>();
            Lines.ForEach(c =>
            {
               var startLinks = Query(c.StartPoint, EnvelopeLength);
                if(startLinks.Count==4)
                {
                    if(!IsIn(startLinks, results))
                    {
                        results.Add(startLinks);
                    }
                }
                var endLinks = Query(c.EndPoint, EnvelopeLength);
                if (endLinks.Count == 4)
                {
                    if (!IsIn(endLinks, results))
                    {
                        results.Add(endLinks);
                    }
                }
            });
            return results;
        }
        /// <summary>
        /// 三叉
        /// </summary>
        /// <returns></returns>
        public List<List<Line>> GetThreeWay()
        {
            var results = new List<List<Line>>();
            Lines.ForEach(c =>
            {
                var startLinks = Query(c.StartPoint, EnvelopeLength);
                if (startLinks.Count == 3)
                {
                    if (!IsIn(startLinks, results))
                    {
                        results.Add(startLinks);
                    }
                }
                var endLinks = Query(c.EndPoint, EnvelopeLength);
                if (endLinks.Count == 3)
                {
                    if (!IsIn(endLinks, results))
                    {
                        results.Add(endLinks);
                    }
                }
            });
            return results;
        }

        /// <summary>
        /// 弯道
        /// </summary>
        /// <returns></returns>
        public List<List<Line>> GetCorner()
        {
            var results = new List<List<Line>>();
            Lines.ForEach(c =>
            {
                var startLinks = Query(c.StartPoint, EnvelopeLength);
                if (startLinks.Count == 2)
                {
                    if (IsElbow(startLinks[0], startLinks[1],1.0) && !IsIn(startLinks, results))
                    {
                        results.Add(startLinks);
                    }
                }
                var endLinks = Query(c.EndPoint, EnvelopeLength);
                if (endLinks.Count == 2)
                {
                    if (IsElbow(endLinks[0], endLinks[1], 1.0) && !IsIn(endLinks, results))
                    {
                        results.Add(endLinks);
                    }
                }
            });
            return results;
        }

        private bool IsElbow(Line first,Line second,double tolerance)
        {
            return !first.IsCollinear(second, tolerance);
        }

        public List<Point3d> GetCornerPoints()
        {
            var results = new List<Point3d>();
            var cornerLines = GetCorner();
            cornerLines.ForEach(o =>
            {
                if(o.Count==2)
                {
                    var pts = ThGeometryTool.IntersectWithEx(o[0], o[1], Intersect.ExtendBoth);
                    if(pts.Count==1)
                    {
                        results.Add(pts[0]);
                    }
                }
            });
            return results;
        }

        /// <summary>
        /// T型
        /// </summary>
        /// <returns></returns>
        public List<List<Line>> GetTType()
        {
            throw new NotSupportedException();
        }

        private List<Line> Query(Point3d pt, double envelopeLength)
        {
            return LineQuery.Query(pt, envelopeLength);
        }

        private bool IsIn(List<Line> lines, List<List<Line>> list)
        {
           return list.Where(o => IsEqual(o, lines)).Any();
        }
        private bool IsEqual(List<Line> line1s ,List<Line> line2s)
        {
            if (line1s.Count == line2s.Count)
            {
                foreach (Line line in line2s)
                {
                    if (!line1s.Contains(line))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
