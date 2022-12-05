using System;
using QuikGraph;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.ChargerDistribution.Group
{
    public class ThChargerGraphNode : IEquatable<ThChargerGraphNode>
    {
        public Point3d Point { get; set; }

        public ThChargerGraphNode(Point3d point)
        {
            Point = point;
        }

        public bool Equals(ThChargerGraphNode other)
        {
            return this.Point.IsEqualTo(other.Point, new Tolerance(1e-5, 1e-5));
        }
    }

    public class ThChargerGraphEdge<T> : EquatableEdge<T> where T : ThChargerGraphNode
    {
        public ThChargerGraphEdge(T source, T target) : base(source, target)
        {
        }

        public ThChargerGraphEdge<T> Inverse()
        {
            return new ThChargerGraphEdge<T>(Target, Source);
        }
    }
}
