using System;
using Autodesk.AutoCAD.Geometry;
using QuikGraph;

namespace ThMEPHVAC.Duct
{
    public class ThDuctVertex : IEquatable<ThDuctVertex>
    {
        public Point3d Position { get; set; }
        public bool IsStartVertexOfGraph { get; set; }


        public ThDuctVertex(Point3d point)
        {
            Position = point;
            IsStartVertexOfGraph = false;
        }

        public bool Equals(ThDuctVertex other)
        {
            var tolerance = new Tolerance(0.1, 0.1);
            return Position.IsEqualTo(other.Position, tolerance);
        }
    }

    public class ThDuctEdge<TVertex> : Edge<TVertex> where TVertex : ThDuctVertex
    {
        public double AirVolume { get; set; }
        public double EdgeLength { get; private set; }
        public double SourceShrink { get; set; }
        public double TargetShrink { get; set; }
        public ThDuctEdge(TVertex source, TVertex target) : base(source, target)
        {
            EdgeLength = source.Position.DistanceTo(target.Position);
        }
    }
}
