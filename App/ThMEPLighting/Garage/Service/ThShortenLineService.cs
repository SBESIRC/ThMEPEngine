using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThShortenLineService
    {
        private List<Line> Results { get; set; }
        private List<Line> Deals { get; set; }
        public ThShortenParameter ShortenParameter { get; set; }
        private ThShortenLineService(ThShortenParameter shortenParameter)
        {
            ShortenParameter = shortenParameter;
            Results = new List<Line>();
            Deals = new List<Line>();
        }
        public static List<Line> Shorten(ThShortenParameter shortenParameter)
        {
            var instance = new ThShortenLineService(shortenParameter);
            instance.Shorten();
            return instance.Results;
        }
        private void Shorten()
        {
            if (!ShortenParameter.IsValid)
            {
                ShortenParameter.DxLines.ForEach(o => Results.Add(new Line(o.StartPoint, o.EndPoint)));
                return;
            }
            ShortenParameter.DxLines.ForEach(o =>
            {
                var newLine = Shorten(o);
                if (newLine.Length > 5)
                {
                    Results.Add(newLine);
                }
                Deals.Add(o);
            });
        }
        private Line Shorten(Line line)
        {
            var lineVec = line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
            var spExt = line.StartPoint - lineVec.MultiplyBy(ShortenParameter.Distance);
            var epExt = line.EndPoint + lineVec.MultiplyBy(ShortenParameter.Distance);
            var spInters = IntersectPts(new Line(spExt, line.StartPoint));
            var epInters = IntersectPts(new Line(line.EndPoint, epExt));
            var newSp = spInters.Count == 0?line.StartPoint: FindNewSp(spInters, line);
            var newEp = epInters.Count == 0 ? line.EndPoint : FindNewEp(epInters, line);
            return new Line(newSp, newEp);
        }
        private Point3d FindNewSp(Point3dCollection interPts, Line line)
        {
            var vec = interPts[0].GetVectorTo(line.EndPoint).GetNormal();
            var shortPt = interPts[0] + vec.MultiplyBy(ShortenParameter.Distance);
            if(ThGeometryTool.IsPointOnLine(line.StartPoint,line.EndPoint, shortPt))
            {
                var branches = FindBranchPts(interPts[0], shortPt);
                if (branches.Count>0)
                {
                    return branches.First();
                }
                else
                {
                    return shortPt;
                }
            }
            else
            {
                return line.StartPoint;
            }    
        }
        private Point3d FindNewEp(Point3dCollection interPts, Line line)
        {
            var vec = interPts[0].GetVectorTo(line.StartPoint).GetNormal();
            var shortPt = interPts[0] + vec.MultiplyBy(ShortenParameter.Distance);
            if (ThGeometryTool.IsPointOnLine(line.StartPoint, line.EndPoint, shortPt))
            {
                var branches = FindBranchPts(interPts[0], shortPt);
                if (branches.Count>0)
                {
                    return branches.First();
                }
                else
                {
                    return shortPt;
                }
            }
            else
            {
                return line.EndPoint;
            }
        }
        private List<Point3d> FindBranchPts(Point3d intersPt,Point3d shortPt)
        {
            var results = new List<Point3d>();
            var extVec = intersPt.GetVectorTo(shortPt);
            var outline = ThDrawTool.ToOutline(intersPt, 
                shortPt, ThGarageLightCommon.RepeatedPointDistance);
            var objs = new DBObjectCollection();
            Results.ForEach(o => objs.Add(o));
            ShortenParameter.FdxLines.ForEach(o => objs.Add(o));
            ShortenParameter.DxLines
                .Where(o=>!Deals.IsContains(o)).ToList()
                .ForEach(o=> objs.Add(o));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var crossObjs=spatialIndex.SelectCrossingPolygon(outline);
            var unParallels = crossObjs
                .Cast<Line>()
                .Where(o => !extVec.IsParallelToEx(o.StartPoint.GetVectorTo(o.EndPoint)))
                .ToList();
            var shortLine = new Line(intersPt, shortPt);
            unParallels.ForEach(o =>
            {
                var pts = new Point3dCollection();
                shortLine.IntersectWith(o, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                pts.Cast<Point3d>()
                .Where(p=>ThGeometryTool.IsPointInLine(intersPt, shortPt,p))
                .ForEach(p=>results.Add(p));
            });
            return results.OrderBy(o => intersPt.DistanceTo(o)).ToList();
        }
        private Point3dCollection IntersectPts(Line extendLine)
        {
            var pts = new Point3dCollection();
            extendLine.IntersectWith(ShortenParameter.Border, 
                Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            return pts;
        }
    }
}
