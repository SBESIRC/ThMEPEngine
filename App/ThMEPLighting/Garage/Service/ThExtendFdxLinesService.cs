using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 双排布置，需要
    /// </summary>
    public class ThExtendFdxLinesService
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex SideSpatialIndex { get; set; }
        private Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; }
        public ThExtendFdxLinesService(Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            CenterSideDicts = centerSideDicts;
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(
                CenterSideDicts.Select(o => o.Key).ToList());
        }
        public List<Line> Extend(List<Line> fdxLines)
        {
            var results = new List<Line>();
            fdxLines.ForEach(o =>
            {
                var fdxLine = new Line(o.StartPoint,o.EndPoint);
                Extend(fdxLine);
                results.Add(new Line(fdxLine.StartPoint, fdxLine.EndPoint));
            });
            return results;
        }

        public List<Line> Shorten(List<Line> fdxLines)
        {
            var results = new List<Line>();
            fdxLines.ForEach(o =>
            {
                var fdxLine = new Line(o.StartPoint, o.EndPoint);
                Shorten(fdxLine);
                results.Add(new Line(fdxLine.StartPoint, fdxLine.EndPoint));
            });
            return results;
        }

        private void Extend(Line fdxLine)
        {
            Point3d sp = fdxLine.StartPoint;
            Point3d ep = fdxLine.EndPoint;
            Extend(fdxLine, sp);
            Extend(fdxLine, ep);
        }
        private void Extend(Line fdxLine, Point3d pt)
        {
            var linkLines = SearchLines(pt, 5.0);
            linkLines = linkLines
                .Where(o => !ThGeometryTool.IsCollinearEx(
                  fdxLine.StartPoint, fdxLine.EndPoint, o.StartPoint, o.EndPoint))
                .Where(o => IsClosed(fdxLine, o))
                .ToList();
            if (linkLines.Count == 0)
            {
                return;
            }
            if (linkLines.Count == 1)
            {
                if(IsCenterHasBothSides(linkLines[0]))
                {
                    Extend(fdxLine, pt, linkLines[0]);
                }
            }
            else if (linkLines.Count == 2)
            {
                if (ThGeometryTool.IsCollinearEx(
                    linkLines[0].StartPoint,
                    linkLines[0].EndPoint,
                    linkLines[1].StartPoint,
                    linkLines[1].EndPoint))
                {
                    if(IsCenterHasBothSides(linkLines[0]))
                    {
                        Extend(fdxLine, pt, linkLines[0]);
                    }
                    else if(IsCenterHasBothSides(linkLines[1]))
                    {
                        Extend(fdxLine, pt, linkLines[1]);
                    }
                }
                else
                {
                    var extLine = SelectExtendLine(fdxLine, linkLines.Where(o => IsCenterHasBothSides(o)).ToList());
                    if (extLine!=null)
                    {
                        Extend(fdxLine, pt, extLine);
                    }
                }
            }
            else
            {
                var extLine = SelectExtendLine(fdxLine, linkLines.Where(o => IsCenterHasBothSides(o)).ToList());
                if (extLine != null)
                {
                    Extend(fdxLine, pt, extLine);
                }
            }
        }

        private Line SelectExtendLine(Line fdx,List<Line> dxs,double tolerance=10.0)
        {
            var vec = fdx.LineDirection();
            // 先选择垂直的
            var firstFilter = dxs.Where(o=> Math.Abs(vec.GetAngleTo(o.LineDirection()).RadToAng()-90.0)<=1.0);
            if(firstFilter.Count()>0)
            {
                return firstFilter.OrderBy(o=> Math.Abs(vec.GetAngleTo(o.LineDirection()).RadToAng() - 90.0)).First();
            }
            var secondFilter = dxs.Where(o => Math.Abs(vec.GetAngleTo(o.LineDirection()).RadToAng() - 90.0) <= tolerance)
                .OrderBy(o=> Math.Abs(vec.GetAngleTo(o.LineDirection()).RadToAng() - 90.0));

            if (secondFilter.Count()>0)
            {
                return secondFilter.First();
            }
            return null;
        }
        private void Shorten(Line fdxLine)
        {
            Point3d sp = fdxLine.StartPoint;
            Point3d ep = fdxLine.EndPoint;
            Shorten(fdxLine, sp);
            Shorten(fdxLine, ep);
        }
        private void Shorten(Line fdxLine, Point3d pt)
        {
            var linkLines = SearchLines(pt, 5.0);
            linkLines = linkLines
                .Where(o => !ThGeometryTool.IsCollinearEx(
                  fdxLine.StartPoint, fdxLine.EndPoint, o.StartPoint, o.EndPoint))
                .Where(o => IsClosed(fdxLine, o))
                .ToList();
            if (linkLines.Count == 0)
            {
                return;
            }
            if (linkLines.Count == 1)
            {
                if (IsCenterHasBothSides(linkLines[0]))
                {
                    Shorten(fdxLine, linkLines[0]);
                }
            }
            else if (linkLines.Count == 2)
            {
                if (ThGeometryTool.IsCollinearEx(
                    linkLines[0].StartPoint,
                    linkLines[0].EndPoint,
                    linkLines[1].StartPoint,
                    linkLines[1].EndPoint))
                {
                    if (IsCenterHasBothSides(linkLines[0]))
                    {
                        Shorten(fdxLine, linkLines[0]);
                    }
                    else if (IsCenterHasBothSides(linkLines[1]))
                    {
                        Shorten(fdxLine, linkLines[1]);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (linkLines.Count > 2)
            {
                throw new NotSupportedException();
            }
        }

        private void Extend(Line fdx,Point3d fdxPortPt, Line center)
        {
            var firstSides = CenterSideDicts[center].Item1;                       
            var firstInters = new Point3dCollection();
            fdx.IntersectWith(firstSides[0], Intersect.ExtendBoth, firstInters, IntPtr.Zero, IntPtr.Zero);
            
            var secondSides = CenterSideDicts[center].Item2;
            var secondInters = new Point3dCollection();
            fdx.IntersectWith(secondSides[0], Intersect.ExtendBoth, secondInters, IntPtr.Zero, IntPtr.Zero);
            if (firstInters.Count == 0 && secondInters.Count == 0)
            {
                return;
            }            
            bool isStart = fdxPortPt.DistanceTo(fdx.StartPoint) < fdxPortPt.DistanceTo(fdx.EndPoint);
            var dir = isStart ? fdx.LineDirection().Negate() : fdx.LineDirection();
            Point3d extentPt = fdxPortPt;
            if (ThGeometryTool.IsPointOnLine(firstInters[0], secondInters[0], fdx.StartPoint) &&
               ThGeometryTool.IsPointOnLine(firstInters[0], secondInters[0], fdx.EndPoint))
            {
                // 非灯线在双排线里               
                if (fdxPortPt.GetVectorTo(firstInters[0]).IsCodirectionalTo(dir))
                {
                    if(firstSides.Where(o=> firstInters[0].IsPointOnLine(o)).Any())
                    {
                        extentPt = firstInters[0];
                    }
                }
                else
                {
                    if (secondSides.Where(o => secondInters[0].IsPointOnLine(o)).Any())
                    {
                        extentPt = secondInters[0];
                    }
                }                
            }
            else
            {                
                if (fdxPortPt.GetVectorTo(firstInters[0]).IsCodirectionalTo(dir))
                {
                    if (firstSides.Where(o => firstInters[0].IsPointOnLine(o)).Any())
                    {
                        extentPt = firstInters[0];
                    }
                    else if(secondSides.Where(o => secondInters[0].IsPointOnLine(o)).Any())
                    {
                        extentPt = secondInters[0];
                    }
                }
                else
                {
                    if (secondSides.Where(o => secondInters[0].IsPointOnLine(o)).Any())
                    {
                        extentPt = secondInters[0];
                    }
                    else if(firstSides.Where(o => firstInters[0].IsPointOnLine(o)).Any())
                    {
                        extentPt = firstInters[0];
                    }    
                }
                
            }
            if (extentPt.DistanceTo(fdxPortPt) > 1.0)
            {
                if (isStart)
                {
                    fdx.StartPoint = extentPt;
                }
                else
                {
                    fdx.EndPoint = extentPt;
                }
            }
        }

        private void Shorten(Line fdx, Line center)
        {
            var firstSides = CenterSideDicts[center].Item1;
            var secondSides = CenterSideDicts[center].Item2;
            ThLinkElbowService.Shorten(fdx, firstSides[0], secondSides[0]);
        }

        private bool IsCenterHasBothSides(Line center)
        {
            return CenterSideDicts[center].Item1.Count > 0 &&
                   CenterSideDicts[center].Item2.Count > 0;
        }

        private List<Line> SearchLines(Point3d portPt, double length)
        {
            Polyline envelope = ThDrawTool.CreateSquare(portPt, length);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs.Cast<Line>().ToList();
        }
        private bool IsClosed(Line fdx, Line center)
        {
            var pts = new Point3dCollection();
            fdx.IntersectWith(center, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            if(pts.Count>0)
            {
                return true;
            }
            //获取第一根线的起点距离第二根线的最近点
            Point3d pt = center.GetClosestPointTo(fdx.StartPoint, false);
            if (pt.DistanceTo(fdx.StartPoint)<=ThGarageLightCommon.RepeatedPointDistance)
            {
                return true;
            }
            //获取第一根线的终点距离第二根线的最近点
            pt = center.GetClosestPointTo(fdx.EndPoint, false);
            if (pt.DistanceTo(fdx.EndPoint) <= ThGarageLightCommon.RepeatedPointDistance)
            {
                return true;
            }
            return false;
        }
    }
}
