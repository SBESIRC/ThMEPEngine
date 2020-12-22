using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThLightGraphService
    {
        private Point3d Start { get; set; }
        private List<ThLightEdge> Edges { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }

        public List<ThLinkPath> Links { get; set; }
        private ThLightGraphService(List<ThLightEdge> edges, Point3d start)
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
        private void Build()
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
                Path = links,
                IsMain = true
            };
            Links.Add(linkPath);
            Travese(links);
        }
        private void Travese(List<ThLightEdge> lightEdges)
        {
            foreach (var lightEdge in lightEdges)
            {
                foreach (var branch in lightEdge.MultiBranch)
                {
                    if(branch.Item2.IsTraversed)
                    {
                        continue;
                    }
                    var branchLinks = new List<ThLightEdge> { branch.Item2 };
                    Find(branchLinks, branch.Item1);
                    UpdateEdge(branchLinks, branch.Item1);
                    var linkPath = new ThLinkPath
                    {
                        Start = branch.Item1,
                        Path = branchLinks,
                        PreEdge= lightEdge
                    };
                    Links.Add(linkPath);
                    Travese(branchLinks);
                }
            }
        }
        private void UpdateEdge(List<ThLightEdge> lightEdges, Point3d start)
        {
            //先将直链设为已遍历
            lightEdges.ForEach(o => o.IsTraversed = true);
            lightEdges.ForEach(o =>
            {
                start=UpdateEdge(o, start);
            });            
        }
        private Point3d UpdateEdge(ThLightEdge lightEdge,Point3d start)
        {
            //找出第一根边上的分支
            Point3d nextPt = GetNextLinkPt(lightEdge, start);
            BuildMultiBranch(lightEdge, nextPt); //获取一条边下一端的支路
            lightEdge.Update(start);
            return nextPt;
        }
        private ThLightEdge FindStartEdge()
        {
            var edges = Edges.Where(o => 
            o.Edge.IsLink(Start,ThGarageLightCommon.RepeatedPointDistance)).ToList();
            return edges.Count == 1 ? edges.First() : null;
        }
        /// <summary>
        /// 查找相连的线
        /// </summary>
        /// <param name="links"></param>
        /// <param name="start"></param>
        private void Find(List<ThLightEdge> links, Point3d start)
        {
            Point3d findPt = GetNextLinkPt(links[links.Count - 1], start);
            var portEdges = SearchEdges(findPt, ThGarageLightCommon.RepeatedPointDistance);
            portEdges = portEdges
                .Where(o => !links.Contains(o))
                .Where(o=>!o.IsTraversed).ToList();
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
        private List<ThLightEdge> SearchEdges(Point3d portPt, double length)
        {
            Polyline envelope = ThDrawTool.CreateSquare(portPt, length);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            var linkLines = searchObjs
                .Cast<Line>()
                .Where(o => ThGarageLightUtils.IsLink(o, portPt))
                .ToList();
            return Edges
                .Where(o => linkLines.Contains(o.Edge))
                .ToList();
        }
        private ThLightEdge FindNeighbourEdge(ThLightEdge currentEdge, List<ThLightEdge> linkEdges)
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
                return unCollinearEdges[0];
            }
            return null;
        }
        /// <summary>
        /// 查找主分支连接的次分支
        /// </summary>
        /// <param name="mainEdge">主分支</param>
        /// <param name="portPt">主分支的端点</param>
        private void BuildMultiBranch(ThLightEdge mainEdge,Point3d portPt)
        {
            //在portPt处查找相连的分支
            var branchRes = new List<Tuple<Point3d, ThLightEdge>>();
            Point3d startPt = mainEdge.Edge.StartPoint;
            Point3d endPt = mainEdge.Edge.EndPoint;
            var vec = startPt.GetVectorTo(endPt).GetNormal();
            startPt = startPt - vec.MultiplyBy(ThGarageLightCommon.RepeatedPointDistance);
            endPt = endPt + vec.MultiplyBy(ThGarageLightCommon.RepeatedPointDistance);
            Polyline outline = ThDrawTool.ToOutline(startPt,endPt,2.0);
            var objs = SpatialIndex.SelectCrossingPolygon(outline);
            objs.Remove(mainEdge.Edge);
            var linkEdges = Edges
                .Where(o => objs.Contains(o.Edge))
                .Where(o => o.Edge.IsLink(portPt));
            var collinearEdges = linkEdges
                .Where(o => ThGeometryTool.IsCollinearEx(
                    mainEdge.Edge.StartPoint, mainEdge.Edge.EndPoint,
                    o.Edge.StartPoint, o.Edge.EndPoint));
            var unCollinearEdges = linkEdges
                .Where(o => !ThGeometryTool.IsCollinearEx(
                    mainEdge.Edge.StartPoint, mainEdge.Edge.EndPoint,
                    o.Edge.StartPoint, o.Edge.EndPoint))
                .Where(o=>o.IsTraversed==false);
            bool ynBuildBranch = false;
            if (collinearEdges.Count()>0)
            {
                //如果末端有共线，则其它不共线为分支
                ynBuildBranch = true;
            }
            else 
            {
                //如果末端无共线，则不共线数量大于1为分支
                if (unCollinearEdges.Count() > 1)
                {
                    ynBuildBranch = true;
                }
            }
            if(ynBuildBranch)
            {
                unCollinearEdges.ForEach(o =>
                {
                    var pts = BuildBranchPt(mainEdge, o);
                    if (pts.Count > 0)
                    {
                        branchRes.Add(Tuple.Create(pts[0], o));
                    }
                });
                mainEdge.MultiBranch = branchRes.OrderBy(o => portPt.DistanceTo(o.Item1)).ToList();
            }
        }
        private Point3dCollection BuildBranchPt(ThLightEdge mainBranch, ThLightEdge secondaryBranch)
        {
            var pts = new Point3dCollection();
            mainBranch.Edge.IntersectWith(secondaryBranch.Edge,
                Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            return pts;
        }
        private Point3d GetNextLinkPt(ThLightEdge lightEdge,Point3d start)
        {
            Point3d preEdgeSp = lightEdge.Edge.StartPoint;
            Point3d preEdgeEp = lightEdge.Edge.EndPoint;
            return start.DistanceTo(preEdgeSp) < start.DistanceTo(preEdgeEp)?
                preEdgeEp : preEdgeSp;
        }
    }
    public class ThLinkPath
    {
        public Point3d Start { get; set; }
        public ThLightEdge PreEdge { get; set; }
        public List<ThLightEdge> Path { get; set; }
        public bool IsMain { get; set; }
        public ThLinkPath()
        {
            Path = new List<ThLightEdge>();
        }
    }
}
