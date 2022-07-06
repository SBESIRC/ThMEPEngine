using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Common
{
    public class ThLightGraphService
    {
        protected Point3d Start { get; set; }
        /// <summary>
        /// 用传入的线创建的边
        /// 遍历完后某些边可能是未被遍历过的
        /// </summary>
        protected List<ThLightEdge> Edges { get; set; }
        protected ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public List<ThLinkPath> Links { get; set; }
        /// <summary>
        /// 创建当前图所有的边
        /// </summary>
        public List<ThLightEdge> GraphEdges
        {
            get
            {
                return Links.SelectMany(l => l.Edges).ToList();
            }
        }
        public Point3d StartPoint
        {
            get
            {
                return Start;
            }
        }

        public ThLightGraphService(List<ThLightEdge> edges, Point3d start)
        {
            Edges = edges;
            Start = start;
            Links = new List<ThLinkPath>();
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(edges.Select(o => o.Edge).ToList());
        }

        public static ThLightGraphService Build(List<ThLightEdge> edges, Point3d start)
        {
            var instance = new ThLightGraphService(edges, start);
            instance.Build();
            return instance;
        }

        public virtual void Build()
        {
            //根据编号第一个端口点，查找起始边
            var startEdge = FindStartEdge();
            if (startEdge == null)
            {
                return;
            }
            //查找以此边为起始边的直链
            //直线端点相互连接的链条
            var links = new List<ThLightEdge> { startEdge };
            Find(links, Start);
            UpdateEdge(links, Start);
            var linkPath = new ThLinkPath
            {
                Start = Start,
                Edges = links,
                IsMain = true
            };
            Links.Add(linkPath);
            Travese(links);
        }

        protected virtual void Travese(List<ThLightEdge> lightEdges)
        {
            foreach (var lightEdge in lightEdges)
            {
                foreach (var branch in lightEdge.MultiBranch)
                {
                    if (branch.Item2.IsTraversed)
                    {
                        continue;
                    }
                    var branchLinks = new List<ThLightEdge> { branch.Item2 };
                    Find(branchLinks, branch.Item1);
                    UpdateEdge(branchLinks, branch.Item1);
                    var linkPath = new ThLinkPath
                    {
                        Start = branch.Item1,
                        Edges = branchLinks,
                        PreEdge = lightEdge
                    };
                    Links.Add(linkPath);
                    Travese(branchLinks);
                }
            }
        }

        protected virtual void UpdateEdge(List<ThLightEdge> lightEdges, Point3d start)
        {
            //先将直链设为已遍历
            lightEdges.ForEach(o => o.IsTraversed = true);
            lightEdges.ForEach(o =>
            {
                start = UpdateEdge(o, start);
            });
        }

        protected virtual Point3d UpdateEdge(ThLightEdge lightEdge, Point3d start)
        {
            //找出第一根边上的分支
            BuildMultiBranch(lightEdge, start); //获取一条边下一端的支路
            Point3d nextPt = GetNextLinkPt(lightEdge, start);
            BuildMultiBranch(lightEdge, nextPt); //获取一条边下一端的支路
            lightEdge.Update(start);
            return nextPt;
        }

        protected virtual ThLightEdge FindStartEdge()
        {
            var edges = Edges.Where(o =>
            o.Edge.IsLink(Start, ThGarageLightCommon.RepeatedPointDistance)).ToList();
            return edges.Count > 0 ? edges.First() : null;
        }

        /// <summary>
        /// 查找相连的线
        /// </summary>
        /// <param name="links"></param>
        /// <param name="start"></param>
        protected virtual void Find(List<ThLightEdge> links, Point3d start)
        {
            //当Degree为零，或碰到已遍历的边结束
            Point3d findPt = GetNextLinkPt(links[links.Count - 1], start);
            var portEdges = SearchEdges(findPt, ThGarageLightCommon.RepeatedPointDistance);
            portEdges = portEdges
                .Where(o => !links.Contains(o))
                .Where(o => !o.IsTraversed).ToList();
            if (portEdges.Count == 0)
            {
                return;
            }
            var neighbourEdge = FindNeighbourEdge(links[links.Count - 1], portEdges);
            if (neighbourEdge == null)
            {
                return;
            }
            links.Add(neighbourEdge);
            Find(links, findPt);
        }

        protected virtual List<ThLightEdge> SearchEdges(Point3d portPt, double length)
        {
            Polyline envelope = ThDrawTool.CreateSquare(portPt, length * 2.0);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            var linkLines = searchObjs
                .Cast<Line>()
                .Where(o => ThGarageLightUtils.IsLink(o, portPt, length))
                .ToList();
            return Edges
                .Where(o => linkLines.Contains(o.Edge))
                .ToList();
        }

        protected virtual ThLightEdge FindNeighbourEdge(ThLightEdge currentEdge, List<ThLightEdge> linkEdges)
        {
            var collinearEdges = linkEdges.Where(o => ThGeometryTool.IsCollinearEx(
                  currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint,
                  o.Edge.StartPoint, o.Edge.EndPoint)).ToList();
            if (collinearEdges.Count > 0)
            {
                return collinearEdges.OrderByDescending(o => o.Edge.Length).First();
            }
            var unCollinearEdges = linkEdges.Where(o => !ThGeometryTool.IsCollinearEx(
                  currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint,
                  o.Edge.StartPoint, o.Edge.EndPoint)).ToList();
            if (unCollinearEdges.Count == 1)
            {
                //对于多个分支，后期可增强权重逻辑，选择优先通道
                return unCollinearEdges[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 查找主分支连接的次分支
        /// </summary>
        /// <param name="mainEdge">主分支</param>
        /// <param name="portPt">主分支的端点</param>
        protected virtual void BuildMultiBranch(ThLightEdge mainEdge, Point3d portPt)
        {
            //在portPt处查找相连的分支
            var branchRes = new List<Tuple<Point3d, ThLightEdge>>();
            Point3d startPt = mainEdge.Edge.StartPoint;
            Point3d endPt = mainEdge.Edge.EndPoint;
            var vec = startPt.GetVectorTo(endPt).GetNormal();
            startPt = startPt - vec.MultiplyBy(ThGarageLightCommon.RepeatedPointDistance);
            endPt = endPt + vec.MultiplyBy(ThGarageLightCommon.RepeatedPointDistance);
            Polyline outline = ThDrawTool.ToOutline(startPt, endPt, 2.0);
            var objs = SpatialIndex.SelectCrossingPolygon(outline);
            objs.Remove(mainEdge.Edge);
            //找到与portPt连接的点
            var linkEdges = Edges
                .Where(o => objs.Contains(o.Edge))
                .Where(o => o.Edge.IsLink(portPt));
            //与mainEdge共线的边
            var collinearEdges = linkEdges
                .Where(o => ThGeometryTool.IsCollinearEx(
                    mainEdge.Edge.StartPoint, mainEdge.Edge.EndPoint,
                    o.Edge.StartPoint, o.Edge.EndPoint));
            //与mainEdge不共线的边
            var unCollinearEdges = linkEdges
                .Where(o => !ThGeometryTool.IsCollinearEx(
                    mainEdge.Edge.StartPoint, mainEdge.Edge.EndPoint,
                    o.Edge.StartPoint, o.Edge.EndPoint));
            if ((collinearEdges.Count() + unCollinearEdges.Count()) <= 1)
            {
                //如果端点只连接了一条边，则返回
                return;
            }
            else
            {
                var edges = new List<ThLightEdge>();
                edges.AddRange(collinearEdges.Where(o => o.IsTraversed == false));
                edges.AddRange(unCollinearEdges.Where(o => o.IsTraversed == false));
                edges.ForEach(o =>
                {
                    var pts = BuildBranchPt(mainEdge, o);
                    if (pts.Count > 0)
                    {
                        branchRes.Add(Tuple.Create(pts[0], o));
                    }
                });
                mainEdge.MultiBranch.AddRange(branchRes.OrderBy(o => portPt.DistanceTo(o.Item1)).ToList());
            }
        }

        protected virtual Point3dCollection BuildBranchPt(ThLightEdge mainBranch, ThLightEdge secondaryBranch)
        {
            var pts = new Point3dCollection();
            mainBranch.Edge.IntersectWith(secondaryBranch.Edge,
                Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            return pts;
        }
        protected virtual Point3d GetNextLinkPt(ThLightEdge lightEdge, Point3d start)
        {
            Point3d preEdgeSp = lightEdge.Edge.StartPoint;
            Point3d preEdgeEp = lightEdge.Edge.EndPoint;
            return start.DistanceTo(preEdgeSp) < start.DistanceTo(preEdgeEp) ?
                preEdgeEp : preEdgeSp;
        }
    }

    public class ThLinkPath
    {
        public Point3d Start { get; set; }
        public ThLightEdge PreEdge { get; set; }
        public List<ThLightEdge> Edges { get; set; }
        public bool IsMain { get; set; }
        public ThLinkPath()
        {
            Edges = new List<ThLightEdge>();
        }
        public double Length
        {
            get
            {
                double sum = 0;
                Edges.ForEach(p => sum += p.Edge.Length);
                return sum;
            }
        }
    }
}
