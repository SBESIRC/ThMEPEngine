using System;
using System.Collections.Generic;
using System.Linq;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using QuickGraph;
using ThCADCore.NTS;

namespace ThMEPHVAC.Duct
{
    public class ThDuctVertex : IEquatable<ThDuctVertex>
    {
        public Point3d Position { get; set; }

        public ThDuctVertex(Point3d point)
        {
            Position = point;
        }

        public bool Equals(ThDuctVertex other)
        {
            var tolerance = new Tolerance(0.1, 0.1);
            return Position.IsEqualTo(other.Position, tolerance);
        }
    }

    public class ThDuctEdge<TVertex> : Edge<TVertex> where TVertex : ThDuctVertex
    {
        public List<ThDraught> DraughtInfomation { get; set; }
        public double AirVolume { get; set; }
        public double TotalVolumeInEdgeChain { get; set; }
        public int DraughtCount { get; set; }
        public double EdgeLength { get; private set; }
        public ThDuctEdge(TVertex source, TVertex target) : base(source, target)
        {
            EdgeLength = source.Position.DistanceTo(target.Position);
        }
    }

    public class ThDuctGraphEngine
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> Graph { get; set; }
        public ThDuctVertex GraphStartVertex { get; set; }

        public ThDuctGraphEngine()
        {
            Graph = new AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>(false);
        }

        private ThDuctVertex AddVerticesGen(Line segment, bool ifFirstSearch, ref Point3d refPoint)
        {
            Point3d sourcepoint = segment.EndPoint.DistanceTo(refPoint) < segment.StartPoint.DistanceTo(refPoint) ? segment.EndPoint : segment.StartPoint;
            Point3d targetpoint = segment.EndPoint.DistanceTo(refPoint) > segment.StartPoint.DistanceTo(refPoint) ? segment.EndPoint : segment.StartPoint;
            ThDuctVertex source = null;
            ThDuctVertex target = new ThDuctVertex(targetpoint);
            if (ifFirstSearch)
            {
                source = new ThDuctVertex(sourcepoint);
                Graph.AddVertex(source);
            }
            else
            {
                source = Graph.Vertices.Where(v => v.Position.IsEqualTo(sourcepoint,new Tolerance(0.1,0.1))).First();
            }
            Graph.AddVertex(target);
            Graph.AddEdge(new ThDuctEdge<ThDuctVertex>(source, target));
            refPoint = targetpoint;
            return source;
        }

        private void DoBuildGraph(Point3d searchpoint, Line currentline)
        {
            var poly = new Polyline();
            poly.CreatePolygon(searchpoint.ToPoint2D(), 4, 10);

            //执行循环探测
            var results = SpatialIndex.SelectCrossingPolygon(poly);
            results.Remove(currentline);
            if (results.Count == 0)
            {
                return;
            }

            foreach (Line result in results)
            {
                AddVerticesGen(result, false, ref searchpoint);
                DoBuildGraph(searchpoint, result);
            }
        }

        public void BuildGraph(DBObjectCollection lines, Point3d searchpoint)
        {
            SpatialIndex = new ThCADCoreNTSSpatialIndex(lines);

            //首先单独处理起始段,探测点为用户指定的起点，第一轮探测
            var poly = new Polyline();
            Matrix3d mt = Active.Editor.CurrentUserCoordinateSystem;
            poly.CreatePolygon(searchpoint.TransformBy(mt).ToPoint2D(), 4, 10);
            var results = SpatialIndex.SelectCrossingPolygon(poly);
            if (results.Count == 0 || results.Count > 1)
            {
                return;
            }
            GraphStartVertex = AddVerticesGen(results[0] as Line, true, ref searchpoint);

            //更新探测点到起始线的终点后，再执行循环探测
            DoBuildGraph(searchpoint, results[0] as Line);
        }
    }
}
