using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThColliearLineMerger
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private DBObjectCollection Lines { get; set; }
        private double ColliearTolerance = 1.0;
        private double ExtendTolerance = 1.0;
        public ThColliearLineMerger(DBObjectCollection lines)
        {
            Lines= lines;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(lines);
        }

        public DBObjectCollection Merge()
        {
            var groups = Group();
            return groups
                .Select(o => CreateLine(o))
                .ToCollection();
        }

        public List<DBObjectCollection> Group()
        {
            var results = new List<DBObjectCollection>();
            // 按共线分组
            var lineGroups = GroupByRectangle(Lines, ColliearTolerance * 2);
            // 再对组内的线按连接关系分组
            lineGroups.ForEach(g => results.AddRange(GroupByLink(g, ExtendTolerance)));
            return results;
        }

        private List<DBObjectCollection> GroupByLink(DBObjectCollection lines,double extendTolerance=3.0)
        {
            var results = new List<DBObjectCollection>();
            // 获取组内每根线的相邻物体
            var groups = new List<DBObjectCollection>();
            lines.OfType<Line>().ForEach(l =>
            {
                var dir = l.LineDirection();
                var newSp = l.StartPoint - dir.MultiplyBy(extendTolerance);
                var newEp = l.EndPoint + dir.MultiplyBy(extendTolerance);
                var objs = Query(newSp, newEp, ColliearTolerance * 2)
                .OfType<DBObject>()
                .Where(o => lines.Contains(o)).ToCollection();
                groups.Add(objs);
            });

            // 组与组合并
            while(groups.Count>0)
            {
                var firstGroup = groups.First();
                groups.RemoveAt(0);

                bool isIslated = true;
                for(int i=0;i< groups.Count;i++)
                {
                    if(IsIntersect(firstGroup, groups[i]))
                    {
                        isIslated = false;
                        groups[i].OfType<DBObject>().ForEach(e =>
                        {
                            if (!firstGroup.Contains(e))
                            {
                                firstGroup.Add(e);
                            }
                        });
                        groups.RemoveAt(i);
                        i--;
                    }
                }
                if(isIslated)
                {
                    results.Add(firstGroup);
                }
                else
                {
                    groups.Add(firstGroup);
                }
            }
            return results;
        }

        private bool IsIntersect(DBObjectCollection first,DBObjectCollection second)
        {
            // first 和 second 是否有交集
            foreach(DBObject obj in first)
            {
                if(second.Contains(obj))
                {
                    return true;
                }
            }
            return false;
        }

        private List<DBObjectCollection> GroupByRectangle(DBObjectCollection lines,double width)
        {
            var results = new List<DBObjectCollection>();
            var extents = CalculateRange(lines);
            var diagonal = CalculateDiagonal(extents) + 2.0;
            while(lines.Count>0)
            {
                var first = lines.OfType<Line>().First();
                if (Math.Abs(first.StartPoint.Y - 127397.0) <= 5.0 && Math.Abs(first.StartPoint.Y - first.EndPoint.Y) <= 5.0)
                {

                }
                var direction = first.LineDirection();
                var midPt = first.StartPoint.GetMidPt(first.EndPoint);
                var newSp = midPt - direction.MultiplyBy(diagonal);
                var newEp = midPt + direction.MultiplyBy(diagonal);
                var parallelGroups = Query(newSp, newEp, width)
                    .OfType<Line>().Where(second=> lines.Contains(second))
                    .OfType<Line>().Where(second => IsApproximateCollinear(first, second, width / 2.0))
                    .ToCollection();
                parallelGroups = parallelGroups.OfType<DBObject>().Where(o => !results.Where(r => r.Contains(o)).Any()).ToCollection();
                results.Add(parallelGroups);
                parallelGroups.OfType<DBObject>().ForEach(o => lines.Remove(o));
            }

            return results;
        }

        private bool IsApproximateCollinear(Line first,Line second,double dis)
        {
            var spDis1 = first.GetClosestPointTo(second.StartPoint,true).DistanceTo(second.StartPoint);
            var epDis2 = first.GetClosestPointTo(second.EndPoint, true).DistanceTo(second.EndPoint);
            return spDis1 <= dis && epDis2 <= dis;
        }

        private DBObjectCollection Query(Point3d sp,Point3d ep,double width)
        {
            var outline = CreateOutline(sp,ep,width);
            var results = SpatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return results;
        }

        private Polyline CreateOutline(Point3d sp, Point3d ep, double width)
        {
            return ThDrawTool.ToRectangle(sp, ep, width); 
        }

        private double CalculateDiagonal(Extents2d extents)
        {
            var xLen = extents.MaxPoint.X - extents.MinPoint.X;
            var yLen = extents.MaxPoint.Y - extents.MinPoint.Y;
            return Math.Sqrt(Math.Pow(xLen,2)+Math.Pow(yLen,2));
        }

        private Extents2d CalculateRange(DBObjectCollection objs)
        {
            var pts = new Point3dCollection();
            objs.OfType<Line>().ForEach(l =>
                {
                    pts.Add(l.StartPoint);
                    pts.Add(l.EndPoint);
                });
            var minX = pts.OfType<Point3d>().OrderBy(p => p.X).FirstOrDefault().X;
            var minY = pts.OfType<Point3d>().OrderBy(p => p.Y).FirstOrDefault().Y;
            var maxX = pts.OfType<Point3d>().OrderByDescending(p => p.X).FirstOrDefault().X;
            var maxY = pts.OfType<Point3d>().OrderByDescending(p => p.Y).FirstOrDefault().Y;
            return new Extents2d(minX, minY, maxX, maxY);
        }
        private Line CreateLine(DBObjectCollection lines)
        {
            var ptPair = ThGeometryTool.GetCollinearMaxPts(lines.OfType<Line>().ToList());
            return new Line(ptPair.Item1, ptPair.Item2);
        }
    }
}
