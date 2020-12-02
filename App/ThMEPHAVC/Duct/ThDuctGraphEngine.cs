using System.Linq;
using DotNetARX;
using QuickGraph;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using QuickGraph.Algorithms;
using QuickGraph.Serialization;
using System.Collections.Generic;
using AcHelper;
using System;
using System.Runtime.Serialization;

namespace ThMEPHAVC.Duct
{
    public class ThDuctVertex
    {
        public double XPosition { get; set; }
        public double YPosition { get; set; }
        //public Point3d Position { get; set; }

        public double DistanceTo(ThDuctVertex targetvertex) 
        {
            return Math.Pow(Math.Pow(this.XPosition - targetvertex.XPosition, 2) + Math.Pow(this.YPosition - targetvertex.YPosition, 2),0.5);
        }

        public bool Equals(Point3d point)
        {
            return this.XPosition == point.X && this.YPosition == point.Y;
        }

        public bool IsSameVertexTo(ThDuctVertex samevertex)
        {
            return Math.Abs(this.XPosition - samevertex.XPosition) < 0.1 && Math.Abs(this.YPosition - samevertex.YPosition) < 0.1;
        }

        public Point3d VertexToPoint3D()
        {
            return new Point3d(XPosition, YPosition, 0);
        }
    }

    public class ThDuctEdge<TVertex> : Edge<TVertex> where TVertex : ThDuctVertex
    {
        public List<ThDraught> DraughtInfomation { get; set; }
        public double AirVolume { get; set; }
        public double TotalVolumeInEdgeChain { get; set; }
        public double EdgeLength { get; set; }
        public int DraughtCount { get; set; }
        public ThDuctEdge(TVertex source, TVertex target) : base(source, target)
        {
            EdgeLength = source.DistanceTo(target);
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
            var source = new ThDuctVertex();
            var target = new ThDuctVertex()
            {
                XPosition = targetpoint.X,
                YPosition = targetpoint.Y,
            };
            if (ifFirstSearch)
            {
                source = new ThDuctVertex()
                {
                    XPosition = sourcepoint.X,
                    YPosition = sourcepoint.Y,
                };
                Graph.AddVertex(source);
            }
            else
            {
                source = Graph.Vertices.Where(v => v.Equals(sourcepoint)).First();
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
