using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Engine
{
    public class ThLaneLineGraphEngine:IDisposable
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private Point3d Start { get; set; }
        public AdjacencyGraph<ThVertex, ThEdge<ThVertex>> Graph { get; set; }
        public ThVertex GraphStartVertex
        {
            get
            {
                return GetVertex(Start);
            }
        }

        public ThLaneLineGraphEngine()
        {
            Graph = new AdjacencyGraph<ThVertex, ThEdge<ThVertex>>(false);
        }

        private Point3d AddVertices(Line segment, Point3d startPt)
        {
            Point3d sourcepoint = segment.EndPoint.DistanceTo(startPt) < segment.StartPoint.DistanceTo(startPt) ? segment.EndPoint : segment.StartPoint;
            Point3d targetpoint = segment.EndPoint.DistanceTo(startPt) > segment.StartPoint.DistanceTo(startPt) ? segment.EndPoint : segment.StartPoint;
            ThVertex source = GetVertex(sourcepoint);
            if(source==null)
            {
                source = new ThVertex(sourcepoint);
                Graph.AddVertex(source);
            }
            ThVertex target = GetVertex(targetpoint);
            if (target == null)
            {
                target = new ThVertex(targetpoint);
                Graph.AddVertex(target);
            }
            if(!IsContains(sourcepoint,targetpoint))
            {                
                Graph.AddEdge(new ThEdge<ThVertex>(source, target));
                return targetpoint; //返回线的下一个点继续查找
            }
            else
            {
                //此边已存在，不需要再继续访问下去
                return sourcepoint;
            }
        }

        private void DoBuildGraph(Point3d searchpoint)
        {
            var poly = searchpoint.CreateSquare(2.0);
            //执行循环探测
            var results = SpatialIndex.SelectCrossingPolygon(poly);
            if (results.Count == 0)
            {
                return;
            }
            foreach (Line line in results)
            {
                var startPt = new Point3d(searchpoint.X, searchpoint.Y, searchpoint.Z);
                var nextPt=AddVertices(line, startPt);
                if(nextPt.DistanceTo(startPt)<=1.0)
                {
                    return;
                }
                else
                {
                    DoBuildGraph(nextPt);
                }
            }
        }
        public void BuildGraph(DBObjectCollection lines, Point3d start)
        {
            Start = start;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            //更新探测点到起始线的终点后，再执行循环探测
            DoBuildGraph(start);
        }
        private bool IsContains(Point3d start,Point3d endPt)
        {
            return Graph.Edges.Where(o =>
             (o.Source.Position.DistanceTo(start) <= 1.0 && o.Target.Position.DistanceTo(endPt) <= 1.0) ).Any();
        }
        public void Dispose()
        {
        }
        public ThVertex GetVertex(Point3d pt)
        {
            var res = Graph.Vertices.Where(o => o.Position.DistanceTo(pt) <= 1.0);
            return res.Count() > 0 ? res.First() : null;
        }
    }
}
