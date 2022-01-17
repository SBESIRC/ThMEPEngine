using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThCADExtension;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThLinkWireFilter
    {
        private DBObjectCollection LinkWires { get; set; }
        private ThQueryPointService PointQuery { get; set; }
        private ThCADCoreNTSSpatialIndex WireSpatialIndex { get; set; }
        public ThLinkWireFilter(DBObjectCollection linkWires, Point3dCollection lightPositions)
        {
            LinkWires = linkWires;
            PointQuery = new ThQueryPointService(lightPositions.OfType<Point3d>().ToList());
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(linkWires);
        }
        public DBObjectCollection FilterBranch(List<Line> edges)
        {
            var garbages = new DBObjectCollection();
            edges.GetThreeWays().Where(o=>o.Count==3).ForEach(o =>
            {
                var pairs = o.GetLinePairs();
                var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
                if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
                {
                    var branch = o.FindBranch(mainPair.Item1, mainPair.Item2);
                    var linkPt = mainPair.Item1.FindLinkPt(branch);
                    var line = FindBranchCloseWire(branch,linkPt.Value);
                    if (line != null)
                    {
                        garbages.Add(line);
                    }
                }
            });
            return Remove(LinkWires, garbages);
        }
        public DBObjectCollection FilterBranch(List<Tuple<Line, Point3d>> branchPtPairs)
        {
            var garbages = new DBObjectCollection();
            branchPtPairs.ForEach(o =>
            {
                var line = FindBranchCloseWire(o.Item1, o.Item2);
                if (line != null)
                {
                    garbages.Add(line);
                }
            });
            return Remove(LinkWires, garbages);
        }
        public DBObjectCollection FilterElbow(List<Line> edges)
        {
            var garbages = new DBObjectCollection();
            edges.GetElbows().Where(o => o.Count == 2).ForEach(o =>
            {
                
                if (!o[0].IsLessThan45Degree(o[1]))
                {
                    var linkPt = o[0].FindLinkPt(o[1]);
                    var line1 = FindBranchCloseWire(o[0], linkPt.Value);
                    if (line1 != null)
                    {
                        garbages.Add(line1);
                    }
                    var line2 = FindBranchCloseWire(o[1], linkPt.Value);
                    if (line2 != null)
                    {
                        garbages.Add(line2);
                    }
                }
            });
            return Remove(LinkWires, garbages);
        }
        private Line FindBranchCloseWire(Line branch,Point3d crossPt)
        {
            var newBranch = branch.ExtendLine(-1.0);
            var outline = CreateOutline(newBranch.StartPoint, newBranch.EndPoint,
                ThGarageLightCommon.RepeatedPointDistance);
            var lines = Query(outline).OfType<Line>()
                .Where(o => branch.IsCollinear(o, 1.0)).ToList();
            var firstLightPos = Point3d.Origin;
            if(FindBranchCloseLight(branch, crossPt, out firstLightPos))
            {
                lines = Sort(lines, crossPt, firstLightPos);
                lines = Filter(lines, crossPt, firstLightPos);
            }
            return lines.Count > 0 ? lines.First() : null;
        }

        private bool FindBranchCloseLight(Line branch, Point3d crossPt,out Point3d findPt)
        {
            findPt = Point3d.Origin;
            var pts = Query(branch);            
            if(pts.Count>0)
            {
                pts = pts.OrderBy(o => o.DistanceTo(crossPt)).ToList();
                findPt = pts.First();
                return true;
            }
            return false;
        }

        private List<Line> Sort(List<Line> lines, Point3d sp, Point3d ep)
        {
            // 根据sp到ep的方向
            return lines
                .OrderBy(e => e.GetMidPt().GetProjectPtOnLine(sp, ep).DistanceTo(sp))
                .ToList();
        }

        private List<Line> Filter(List<Line> lines, Point3d sp, Point3d ep)
        {
            var line = new Line(sp, ep);
            var results = lines
                .Where(o => o.GetMidPt().IsPointOnCurve(line, ThGarageLightCommon.RepeatedPointDistance))
                .ToList();
            line.Dispose();
            return results;
        }

        private Polyline CreateOutline(Point3d start,Point3d end,double width)
        {
            return ThDrawTool.ToOutline(start, end, 1.0);
        }

        private DBObjectCollection Query(Polyline outline)
        {
            return WireSpatialIndex.SelectCrossingPolygon(outline);
        }

        private List<Point3d> Query(Line edge)
        {
            return PointQuery.Query(edge);
        }

        private DBObjectCollection Remove(DBObjectCollection linkWires,DBObjectCollection garbages)
        {
            return linkWires
                .OfType<Entity>()
                .Where(o => !garbages.Contains(o))
                .ToCollection();
        }

        private DBObjectCollection FindFilterLines(DBObjectCollection linkWires)
        {
            var garbages = new DBObjectCollection();
            var threeWays = linkWires.OfType<Line>().ToList().GetThreeWays();
            threeWays
                .Where(o => o.Count == 3)
                .ForEach(o =>
                {
                    var branch = FilterLines(o);
                    if (branch != null)
                    {
                        garbages.Add(branch);
                    }
                });
            return garbages;
        }

        private Line FilterLines(List<Line> lines)
        {
            var pairs = lines.GetLinePairs();
            var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
            if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
            {
                return lines.FindBranch(mainPair.Item1, mainPair.Item2);
            }
            else
            {
                return null;
            }
        }
    }
}
