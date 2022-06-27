using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThLayoutPointOptimizeService
    {
        private Dictionary<Line, List<Point3d>> LayOutPoints {get;set;}
        private double Distance { get; set; }
        private ThCADCoreNTSKdTree KdTree { get; set; }
        private ThCADCoreNTSSpatialIndex EdgeSpatialIndex { get; set; }
        public ThLayoutPointOptimizeService(Dictionary<Line,List<Point3d>> layOutPoints,double distance)
        {
            Distance = distance;
            LayOutPoints = layOutPoints;
            BuildKdTree();
            BuildEdgeSpatialIndex();
        }

        public void Optimize()
        {
            var collector = new List<Point3d>();
            LayOutPoints.ForEach(l =>
            {
                if(l.Value.Count>0)
                {
                    if(IsRemove(l.Value.First()))
                    {
                        collector.Add(l.Value.First());
                    }
                    if (l.Value.Count > 1 && IsRemove(l.Value.Last()))
                    {
                        collector.Add(l.Value.Last());
                    }
                }
            });
            RemoveFilterPts(collector);
        }

        private bool IsRemove(Point3d pt)
        {
            var ownerEdge = FindEdge(pt);
            var groups = Query(pt);
            if(groups.Contains(pt))
            {
                groups.Remove(pt);
            }
            foreach(Point3d neibour in groups)
            {
                if(IsOnSameEdge(pt, neibour) && IsPtAtCorner(pt))
                {
                    return true;
                }
                var neibourEdge = FindEdge(neibour);
                if (neibourEdge.Length > ownerEdge.Length &&
                    ownerEdge.FindLinkPt(neibourEdge).HasValue)
                {
                    return false;
                }
            }
            return false;
        }

        private bool IsOnSameEdge(Point3d first,Point3d second)
        {
            return LayOutPoints
                .Where(o => o.Value.Contains(first) && o.Value.Contains(second))
                .Any();
        }

        private bool IsPtAtCorner(Point3d lightPt)
        {
            var edge = FindEdge(lightPt);
            var lightPts = FindLightPts(edge);
            var refPt = FindReferencePt(lightPt, lightPts);
            if(refPt.HasValue)
            {
                if (IsClosePort(edge.StartPoint, lightPt, refPt.Value))
                {
                    return IsCornerPt(edge.StartPoint);
                }
                else if(IsClosePort(edge.EndPoint, lightPt, refPt.Value))
                {
                    return IsCornerPt(edge.EndPoint);
                }
            }           
            return false;
        }

        private bool IsCornerPt(Point3d pt)
        {
            var eges = QueryEdges(pt);
            if(eges.Count==2)
            {
                var first = eges.OfType<Line>().First();
                var second = eges.OfType<Line>().Last();
                if(!ThGarageUtils.IsLessThan45Degree(first.StartPoint,first.EndPoint,second.StartPoint,second.EndPoint))
                {
                    return true;
                }
            }
            return false;
        }

        private DBObjectCollection QueryEdges(Point3d pt)
        {
            var envelop = ThDrawTool.CreateSquare(pt, ThGarageLightCommon.RepeatedPointDistance);
            return EdgeSpatialIndex.SelectCrossingPolygon(envelop);
        }

        private Point3d? FindReferencePt(Point3d pt,List<Point3d> lightPts)
        {        
            var index = lightPts.IndexOf(pt);
            if (lightPts.Count > 1)
            {
                var refPt = pt;
                if (index == 0)
                {
                    return lightPts[index + 1];
                }
                else if (index == lightPts.Count - 1)
                {
                    return lightPts[index - 1];
                }
            }
            return null;
        }

        private bool IsClosePort(Point3d linePort,Point3d pt1,Point3d pt2)
        {
            return pt1.DistanceTo(linePort) < pt2.DistanceTo(linePort);
        }

        private Point3dCollection Query(Point3d pt)
        {
            var res = KdTree.Nodes.Where(o => o.Value.Contains(pt));
            return res.Count() > 0 ? res.First().Value : new Point3dCollection();
        }

        private void RemoveFilterPts(List<Point3d> pts)
        {
            // 移除要过滤的点
            LayOutPoints.ForEach(o =>
            {
                o.Value.ForEach(p =>
                {
                    if (pts.Contains(p))
                    {
                        o.Value.Remove(p);
                    }
                });
            });
        }

        private List<Point3d> Handle(Point3dCollection pts)
        {
            if(pts.Count < 2)
            {
                return new List<Point3d>();
            }
            else
            {
                return Filter(pts);
            }
        }

        private List<Point3d> Filter(Point3dCollection pts)
        {
            var results = new List<Point3d>();
            var groups = GroupByLink(pts);
            if(groups.Count==1 && groups[0].Count==2)
            {
                var edgeDicts = BuildBelongedEdges(groups[0]);
                if(edgeDicts.Count==2)
                {
                    results.AddRange(edgeDicts.OrderBy(o => o.Key.Length).First().Value);
                }
            }
            return results;
        }

        private Dictionary<Line,List<Point3d>> BuildBelongedEdges(Point3dCollection pts)
        {
            var result = new Dictionary<Line, List<Point3d>>();
            pts.OfType<Point3d>().ForEach(p => 
            {
                var edge = FindEdge(p);
                if(!result.ContainsKey(edge))
                {
                    result.Add(edge, new List<Point3d>() { p });
                }
                else
                {
                    result[edge].Add(p);
                }
            });
            return result;
        }

        private List<Point3dCollection> GroupByLink(Point3dCollection pts)
        {
            var results = new List<Point3dCollection>();
            for (int i = 0; i < pts.Count-1; i++)
            {
                if(results.Where(o => o.Contains(pts[i])).Any())
                {
                    continue;
                }
                var groups = new Point3dCollection() { pts [i]};
                var iEdge = FindEdge(pts[i]);
                for (int j = i+1; j < pts.Count; j++)
                {
                    var jEdge = FindEdge(pts[j]);
                    if (IsLink(iEdge,jEdge))
                    {
                        groups.Add(pts[j]);
                    }
                }
                results.Add(groups);
            }
            return results;
        }

        private bool IsLink(Line first,Line second)
        {
            return first.FindLinkPt(second, ThGarageLightCommon.RepeatedPointDistance).HasValue;
        }

        private Line FindEdge(Point3d pt)
        {
            return LayOutPoints.Where(o => o.Value.Contains(pt)).First().Key;
        }

        private List<Point3d> FindLightPts(Line edge)
        {
            if(LayOutPoints.ContainsKey(edge))
            {
                return LayOutPoints[edge];
            }
            else
            {
                return new List<Point3d>();
            }
        }

        private void BuildKdTree()
        {
            KdTree = new ThCADCoreNTSKdTree(Distance);
            LayOutPoints.SelectMany(o => o.Value).ToList().ForEach(p =>
            {
                KdTree.InsertPoint(p);
            });
        }
        private void BuildEdgeSpatialIndex()
        {
            var lines = LayOutPoints.Select(o => o.Key).ToCollection();
            EdgeSpatialIndex = new ThCADCoreNTSSpatialIndex(lines);
        }
    }
}
