using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindCenterPairLinesService: ThFindSideLinesService
    {
        private ThFindCenterPairLinesService(ThFindSideLinesParameter findParameter)
            :base(findParameter)
        {           
        }
        public static new Dictionary<Line, List<Line>> Find(ThFindSideLinesParameter findParameter)
        {
            var instance = new ThFindCenterPairLinesService(findParameter);
            instance.FindSide();
            return instance.SideLinesDic;
        }
        private void FindSide()
        {
            var unFindLines = new List<Line>();
            var s = FindParameter.CenterLines.Select(x => x.Length).ToList();
            FindParameter.CenterLines.ForEach(o =>
                {
                    var lines = FilterSideByVertical(o);
                    if (lines.Count == 2)
                    {
                        SideLinesDic.Add(o, lines);
                    }
                    else
                    {
                        unFindLines.Add(o);
                    }
                });
            unFindLines.ForEach(o =>
            {
                var lines = FilterSideByHorizontal(o);
                if (lines.Count == 2)
                {
                    SideLinesDic.Add(o, lines);
                }                
            });
        }
        private List<Line> FilterSideByVertical(Line center)
        {
            var results = new List<Line>();
            var penpendVec = center.StartPoint.GetVectorTo(center.EndPoint).GetNormal().GetPerpendicularVector();
            var midPt = ThGeometryTool.GetMidPt(center.StartPoint, center.EndPoint);
            var verSp = midPt + penpendVec.MultiplyBy(FindParameter.HalfWidth + SideTolerance);
            var verEp = midPt - penpendVec.MultiplyBy(FindParameter.HalfWidth + SideTolerance);
            var upLines = FilterSide(center, verSp, midPt);
            var downLines = FilterSide(center, verEp, midPt);
            if(upLines.Count==1 && downLines.Count == 1)
            {
                results.AddRange(upLines);
                results.AddRange(downLines);
                return results;
            }
            else
            {
                return results;
            }
        }
        private List<Line> FilterSideByHorizontal(Line center)
        {
            var results = new List<Line>();
            var outline = ThDrawTool.ToOutline(center.StartPoint, center.EndPoint, FindParameter.HalfWidth + SideTolerance);
            var objs = SideSpatialIndex.SelectCrossingPolygon(outline);
            var filterObjs= objs
               .Cast<Line>()
               .Where(o => o.Length > 0.0)
               .Where(o => FindParameter.SideLines.Contains(o))
               .Where(o => ThGeometryTool.IsParallelToEx(center.LineDirection(), o.LineDirection()))
               .Where(o=>center.HasCommon(o))
               .Where(o => DistanceIsValid(center, o))
               .Where(o=> !IsExisted(o))
               .ToList();
            if(filterObjs.Count==2)
            {
                double distance = filterObjs[0].Distance(filterObjs[1]);
                if(distance > FindParameter.HalfWidth)
                {
                    return filterObjs;
                }
                else
                {
                    return results;
                }
            }
            else
            {
                return results;
            }
        }
        private List<Line> FilterSide(Line center,Point3d pt)
        {
            Polyline outline = pt.CreateSquare(1.0);
            var objs = SideSpatialIndex.SelectCrossingPolygon(outline);
            return objs
                .Cast<Line>()
                .Where(o => o.Length > 0.0)
                .Where(o => FindParameter.SideLines.Contains(o))
                .Where(o => ThGeometryTool.IsParallelToEx(center.LineDirection(), o.LineDirection()))
                .Where(o => DistanceIsValid(center, o))
                .Where(o=> !IsExisted(o))
                .ToList();
        }
        private List<Line> FilterSide(Line center, Point3d sp,Point3d ep)
        {
            Polyline outline = ThDrawTool.ToRectangle(sp,ep,1.0);
            var objs = SideSpatialIndex.SelectCrossingPolygon(outline);
            return objs
                .Cast<Line>()
                .Where(o => o.Length > 0.0)
                .Where(o => FindParameter.SideLines.Contains(o))
                .Where(o => ThGeometryTool.IsParallelToEx(center.LineDirection(), o.LineDirection()))
                .Where(o => DistanceIsValid(center, o))
                .Where(o => !IsExisted(o))
                .ToList();
        }
        private bool IsExisted(Line line)
        {
            return SideLinesDic.Where(o => o.Value.IsContains(line)).Any();
        }
    }
}
