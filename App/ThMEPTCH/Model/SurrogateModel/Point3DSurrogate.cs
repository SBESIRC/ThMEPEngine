using Autodesk.AutoCAD.Geometry;
using ProtoBuf;

namespace ThMEPTCH.Model.SurrogateModel
{
    [ProtoContract]
    public struct Point3DSurrogate
    {
        public Point3DSurrogate(double x, double y, double z) : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        [ProtoMember(1)]
        public double X { get; set; }
        [ProtoMember(2)]
        public double Y { get; set; }
        [ProtoMember(3)]
        public double Z { get; set; }

        public static implicit operator Point3d(Point3DSurrogate surrogate)
        {
            return new Point3d(surrogate.X, surrogate.Y, surrogate.Z);
        }

        public static implicit operator Point3DSurrogate(Point3d point)
        {
            return new Point3DSurrogate(point.X, point.Y, point.Z);
        }
    }
}
