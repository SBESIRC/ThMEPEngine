using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThMergeLightLineService
    {
        private List<Curve> Results { get; set; }
        private Polyline Border { get; set; }
        private List<Line> Lines { get; set; }
        private ThMergeLightLineService(Polyline border,List<Line> centerLines)
        {
            Border = border;
            Lines = centerLines;
            Results = new List<Curve>();
        }
        protected ThMergeLightLineService()
        {
        }
        public static List<Curve> Merge(Polyline border, List<Line> centerLines,double mergeRange)
        {
            var instance = new ThMergeLightLineService(border, centerLines);
            instance.Merge(mergeRange);
            return instance.Results;
        }
        private void Merge(double mergeRange)
        {
            var auxiliaryLines = new List<List<Line>>();
            var mainLines = new List<List<Line>>();
            var laneLine = new ParkingLinesService();
            laneLine.parkingLineTolerance = mergeRange;
            //目前会将传入的线延长2mm

            var cleanService = new ThLaneLineCleanService();
            var objs = cleanService.Clean(Lines.ToCollection());
            var handleLines = objs.Cast<Line>().Where(o => o.Length > 2).ToList();

            mainLines = laneLine.CreateNodedParkingLines(Border, handleLines, out auxiliaryLines);
            mainLines.ForEach(o =>
            {
                if (o.Count == 1)
                {
                    Results.Add(o[0]);
                }
                else if (o.Count > 1)
                {
                    var polyline = laneLine.CreateParkingLineToPolylineByTol(o);
                    Results.Add(polyline);
                }
            });
            auxiliaryLines.ForEach(o =>
            {
                if (o.Count == 1)
                {
                    Results.Add(o[0]);
                }
                else
                {
                    var polyline = laneLine.CreateParkingLineToPolylineByTol(o);
                    Results.Add(polyline);
                }
            });
            //Results.Clear();
            var simplifyCurves = new DBObjectCollection();
            Results.ForEach(x =>
            {
                if (x is Polyline polyline)
                {
                    var mergedLines = ThLaneLineEngine.Explode(new DBObjectCollection() { polyline });
                    mergedLines = ThLaneLineMergeExtension.Merge(mergedLines);
                    foreach (var mLine in mergedLines)
                    {
                        simplifyCurves.Add(mLine as Curve);
                    }
                    
                }
                else
                {
                    simplifyCurves.Add(x);
                }
            });
            var lines = ThLaneLineExtendEngine.Extend(simplifyCurves);
            lines = ThLaneLineMergeExtension.Merge(lines);
            lines = ThLaneLineEngine.Noding(lines);
            lines = ThLaneLineEngine.CleanZeroCurves(lines);
            Results.Clear();
            mainLines = laneLine.CreateNodedParkingLines(Border, lines.Cast<Line>().ToList(), out auxiliaryLines);
            mainLines.ForEach(o =>
            {
                if (o.Count == 1)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines">传入的线不允许有重合</param>
        /// <param name="twoLineOuterAngLimited">合并两直线外角小于ang</param>
        /// <returns></returns>
        public static List<List<Line>> Merge(List<Line> lines)
        {
            var instance = new ThMergeLightLineService();            
            return instance.DoMerge(lines);
        }

        public static List<Line> MergeNewLines(List<Line> lines)
        {
            var groups = Merge(lines);
            return groups.SelectMany(o => MergeLines(o)).ToList();
        }

        private static List<Line> MergeLines(List<Line> lines)
        {
            // lines 已经是有序的了
            var results = new List<Line>();
            var collinearGroups = new List<List<Line>>();
            for (int i = 0; i < lines.Count; i++)
            {
                var links = new List<Line> { lines[i] };
                int j = i + 1;
                for (; j < lines.Count; j++)
                {
                    if (lines[j].IsCollinear(links.Last(), 1.0) && lines[j].FindLinkPt(links.Last()).HasValue)
                    {
                        links.Add(lines[j]);
                    }
                    else
                    {
                        break;
                    }
                }
                i = j - 1;
                collinearGroups.Add(links);
            }
            collinearGroups.ForEach(l =>
            {
                if (l.Count == 1)
                {
                    results.Add(l[0].Clone() as Line);
                }
                else
                {
                    var ptPair = l.GetCollinearMaxPts();
                    results.Add(new Line(ptPair.Item1, ptPair.Item2));
                }
            });
            return results;
        }

        protected List<List<Line>> DoMerge(List<Line> lines)
        {
            var newList = lines.Select(l => l).ToList();
            var results = new List<List<Line>>();
            while (newList.Count > 0)
            {
                var current = newList[0];
                newList.RemoveAt(0);
                var links = new List<Line>() { current };
                if(newList.Count>0)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(newList.ToCollection());
                    FindLinks(links, current.StartPoint, true, spatialIndex);
                    FindLinks(links, current.EndPoint, false, spatialIndex);
                }                
                results.Add(links);
                newList = newList.Where(l => !links.Contains(l)).ToList();
            }
            return results;
        }

        private void FindLinks(List<Line> lines,Point3d portPt,bool findStart, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var current = findStart ? lines.First() : lines.Last();

            // 查询portPt连接的线
            var linkLines = Query(portPt, spatialIndex);

            // 过滤
            linkLines = Filter(lines, linkLines, current);

            // 按外角从小到大排序
            linkLines = linkLines.OrderBy(l => ThGarageUtils.CalculateTwoLineOuterAngle(
                current.StartPoint, current.EndPoint, l.StartPoint, l.EndPoint)).ToList();

            if (linkLines.Count > 0)
            {
                if(findStart)
                {
                    lines.Insert(0, linkLines.First());
                    Point3d findPt = portPt.GetNextLinkPt(lines.First().StartPoint, lines.First().EndPoint);
                    FindLinks(lines, findPt, findStart, spatialIndex);
                }
                else
                {
                    lines.Add(linkLines.First());
                    Point3d findPt = portPt.GetNextLinkPt(lines.Last().StartPoint, lines.Last().EndPoint);
                    FindLinks(lines, findPt, findStart, spatialIndex);
                }
            }
            else
            {
                return;
            }
        }
        protected virtual List<Line> Filter(List<Line> lines, List<Line> linkLines,Line current)
        {
            // 过滤（1、将在lines中的线去掉;2、线与current的外角要满足指定的范围）
            return linkLines
                .Where(l => !lines.Contains(l))
                .Where(l => IsQualified(current, l))
                .ToList();
        }
        private bool IsQualified(Line first,Line second)
        {
            // 计算两直线的外角
            var outerAng = ThGarageUtils.CalculateTwoLineOuterAngle(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint);

            return outerAng - ThGarageLightCommon.LineOuterAngLimited <= 1e-5;
        }

        private List<Line> Query(Point3d portPt, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            Polyline envelope = portPt.CreateSquare(ThGarageLightCommon.RepeatedPointDistance);
            var searchObjs = spatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs
                .OfType<Line>()
                .Where(o => ThGarageLightUtils.IsLink(o, portPt, ThGarageLightCommon.RepeatedPointDistance))
                .ToList();
        }
    }
    public class ThPriorityMergeLightLineService: ThMergeLightLineService
    {
        private ThPriorityMergeLightLineService()
        {
        }
        public static new List<List<Line>> Merge(List<Line> lines)
        {
            var instance = new ThPriorityMergeLightLineService();
            return instance.DoMerge(lines);
        }

        protected override List<Line> Filter(List<Line> lines, List<Line> linkLines, Line current)
        {
            // 过滤1、将在lines中的线去掉;
            linkLines = linkLines
                .Where(l => !lines.Contains(l)).ToList();
            if (linkLines.Count ==1)
            {
                // 度为1 ，接受
                return linkLines;
            }
            else
            {
                // 选择外角在一定范围的线
                return base.Filter(lines, linkLines, current);
            }
        }
    }
}
