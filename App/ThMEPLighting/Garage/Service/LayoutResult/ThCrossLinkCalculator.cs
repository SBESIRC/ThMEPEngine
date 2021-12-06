using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThCrossLinkCalculator
    {
        /// <summary>
        /// 车道中心和1号线的Binding
        /// CenterSideDicts.Key 和CenterGroupLines.Dictionary.Key有映射关系
        /// 它们来源于双排布置返回的结果
        /// </summary>
        protected Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; }
        /// <summary>
        /// 某个区域灯线按连通性分组的结果
        /// </summary>
        private List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; set; }
        public ThCrossLinkCalculator(
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            CenterSideDicts = centerSideDicts;
        }
        public ThCrossLinkCalculator(
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts,
            List<Tuple<Point3d, Dictionary<Line, Vector3d>>> centerGroupLines)
        {
            CenterSideDicts = centerSideDicts;
            CenterGroupLines = centerGroupLines;
        }

        public List<List<Line>> LinkCableTrayCross()
        {
            var results = new List<List<Line>>();
            var crosses = GetCrosses();
            crosses.ForEach(c =>
            {
                var res = Sort(c);
                // 分区
                var partitions = CreatePartition(res);

                // 收集拐角点
                var turnerPts = new List<Point3d>();
                // 只有分区为偶数
                if (partitions.Count ==4)
                {
                    // 获取中心线附带的边线
                    var sides = GetCenterSides(c);
                    for (int i = 0; i < partitions.Count; i++)
                    {
                        var current = partitions[i];
                        var inters = ThGeometryTool.IntersectWithEx(current.Item1, current.Item2, Intersect.ExtendBoth);
                        if(inters.Count==0)
                        {
                            continue;
                        }
                        var currentArea = CreateParallelogram(current.Item1, current.Item2);
                        var currentSides = GroupSides(currentArea, sides); // 分组
                        var lineRoadService = new ThLineRoadQueryService(currentSides);
                        var cornerPts = lineRoadService.GetCornerPoints();
                        if(cornerPts.Count>0)
                        {
                            turnerPts.Add(cornerPts.OrderBy(p => p.DistanceTo(inters[0])).First());
                        }
                    }
                }

                // 连线
                results.Add(Link(c, turnerPts));
            });
            return results.Where(o=>o.Count>0).ToList();
        }
        protected List<List<Line>> GetCrosses()
        {
            var centerSidesQuery = new ThLineRoadQueryService(CenterSideDicts.Select(o => o.Key).ToList());
            return centerSidesQuery.GetCross();
        }
        private List<Line> Link(List<Line> crosses,List<Point3d> pts)
        {
            var results = new List<Line>();
            if (crosses.Count == 4 && pts.Count == 4)
            {
                var lineDirs = crosses
                    .Select(o => Tuple.Create(o, Query(o)))
                    .Where(o => o.Item2.HasValue)
                    .ToList();
                if (lineDirs.Count == 4)
                {
                    var mainBranch = FindMainBranch(lineDirs.Select(o => Tuple.Create(o.Item1, o.Item2.Value)).ToList());
                    if(mainBranch!=null)
                    {
                        var frame = CreatePolyline(pts);
                        return DrawLines(mainBranch, frame);
                    }
                }
            }
            return results;
        }

        private List<Line> DrawLines(Tuple<Line, Vector3d, Line, Vector3d> mainBranch,Polyline frame)
        {
            var results = new List<Line>();
            var firstIntersPt = mainBranch.Item1.IntersectWithEx(frame);
            var secondIntersPt = mainBranch.Item3.IntersectWithEx(frame);
            if(firstIntersPt.Count==1 && secondIntersPt.Count==1)
            {
                var dir = firstIntersPt[0].GetVectorTo(secondIntersPt[0]);
                if (dir.IsSameDirection(mainBranch.Item2))
                {
                    results = SubtractPointOwnerEdge(frame, secondIntersPt[0]);
                }
                else
                {
                    results = SubtractPointOwnerEdge(frame, firstIntersPt[0]);
                }
            }
            return results;
        }

        private List<Line> SubtractPointOwnerEdge(Polyline polyline,Point3d pt)
        {
            var results = new List<Line>();
            for(int i =0;i<polyline.NumberOfVertices;i++)
            {
                var lineSeg = polyline.GetLineSegmentAt(i);
                if(ThGeometryTool.IsPointOnLine(lineSeg.StartPoint, lineSeg.EndPoint, pt))
                {
                    continue;
                }
                else
                {
                    results.Add(new Line(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            return results;
        }

        private Polyline CreatePolyline(List<Point3d> pts,bool isClosed = true)
        {
            var newPts = new Point3dCollection();
            pts.ForEach(p => newPts.Add(p));
            return newPts.CreatePolyline(isClosed);
        }

        private Tuple<Line,Vector3d,Line,Vector3d> FindMainBranch(List<Tuple<Line,Vector3d>> lineDirs)
        {
            for(int i=0;i< lineDirs.Count-1;i++)
            {
                for (int j = i+1; j < lineDirs.Count; j++)
                {
                    if(IsMainBranch(lineDirs[i].Item1, lineDirs[i].Item2, lineDirs[j].Item1, lineDirs[j].Item2))
                    {
                        return Tuple.Create(lineDirs[i].Item1, lineDirs[i].Item2, lineDirs[j].Item1, lineDirs[j].Item2);
                    }
                }
            }
            return null;
        }

        private bool IsMainBranch(Line first,Vector3d firstDir,Line second,Vector3d secondDir)
        {
           return ThGarageUtils.IsLessThan45Degree(first.StartPoint, first.EndPoint, 
               second.StartPoint, second.EndPoint) && firstDir.IsSameDirection(secondDir);
        }

        private Vector3d? Query(Line line)
        {
            foreach(var item in CenterGroupLines)
            {
                if(item.Item2.ContainsKey(line))
                {
                    return item.Item2[line];
                }
            }
            return null; 
        }

        /// <summary>
        /// 创建平行四边形
        /// </summary>
        /// <param name="a">邻边a</param>
        /// <param name="b">邻边b</param>
        /// <returns></returns>
        protected Polyline CreateParallelogram(Line a, Line b)
        {
            var pts = a.IntersectWithEx(b, Intersect.ExtendBoth);
            if (pts.Count == 0)
            {
                return new Polyline();
            }
            var first = pts[0];
            var second = first.GetNextLinkPt(a.StartPoint, a.EndPoint);
            var four = first.GetNextLinkPt(b.StartPoint, b.EndPoint);
            var vec1 = first.GetVectorTo(second);
            var vec2 = first.GetVectorTo(four);
            var third = first + vec1 + vec2;
            var points = new Point3dCollection() { first, second, third, four };
            return points.CreatePolyline();
        }
        protected List<Line> Sort(List<Line> centers)
        {
            // 把十字路口车道线按照逆时针排序
            var lines = AdjustCrossLines(centers);
            return lines
                .OrderBy(l => NewAngle(l.Value.Angle.RadToAng()))
                .Select(o => o.Key)
                .ToList();
        }
        protected Dictionary<Line, Line> AdjustCrossLines(List<Line> crosses)
        {
            /*            
             *            ^
             *            |
             *         <-- -->
             *            |
             *            v
             */
            var result = new Dictionary<Line, Line>();
            Point3d? centerPt = null;
            for (int i = 1; i < crosses.Count; i++)
            {
                var linkPt = crosses[0].FindLinkPt(crosses[i], ThGarageLightCommon.RepeatedPointDistance);
                if (linkPt.HasValue)
                {
                    centerPt = linkPt;
                    break;
                }
            }
            if (centerPt.HasValue)
            {
                crosses.ForEach(c =>
                {
                    var farwayPt = centerPt.Value.GetNextLinkPt(c.StartPoint, c.EndPoint);
                    var closePt = farwayPt.GetNextLinkPt(c.StartPoint, c.EndPoint);
                    result.Add(c, new Line(closePt, farwayPt));
                });
            }
            else
            {
                crosses.ForEach(c => result.Add(c, new Line(c.StartPoint, c.EndPoint)));
            }
            return result;
        }
        protected double NewAngle(double ang)
        {
            return Math.Floor(ang + 0.5) % 360.0;
        } 
        protected List<Line> GetCenterSides(List<Line> centers)
        {
            var results = new List<Line>();
            centers.ForEach(c =>
            {
                if (CenterSideDicts.ContainsKey(c))
                {
                    results.AddRange(CenterSideDicts[c].Item1);
                    results.AddRange(CenterSideDicts[c].Item2);
                }
            });
            return results;
        }

        protected List<Tuple<Line, Line>> CreatePartition(List<Line> lines)
        {
            var partitions = new List<Tuple<Line, Line>>();
            int count = lines.Count;
            for (int i = 0; i < count; i++)
            {
                var adjacentEdgeA = lines[i];
                var adjacentEdgeB = lines[(i + 1) % count];
                if (!adjacentEdgeA.IsParallelToEx(adjacentEdgeB))
                {
                    partitions.Add(Tuple.Create(adjacentEdgeA, adjacentEdgeB));
                }
            }
            return partitions;
        }
        protected virtual List<Line> GroupSides(Polyline partition, List<Line> sides)
        {
            return sides
                .Where(e => partition.IsContains(e.StartPoint) || partition.IsContains(e.EndPoint))
                .ToList();
        }
    }
}
