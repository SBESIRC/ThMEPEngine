using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;

namespace ThMEPIFC.Geometry
{
    public class ThXbimCircle3D
    {
        public Circle3D Geometry { get; private set; }

        public ThXbimCircle3D(XbimPoint3D p1, XbimPoint3D p2, XbimPoint3D p3)
        {
            Geometry = Circle3D.FromPoints(p1.ToPoint3D(), p2.ToPoint3D(), p3.ToPoint3D());
        }

        public bool IsClockWise()
        {
            return Geometry.Axis.DotProduct(new Vector3D(0, 0, 1)) < 0;
        }
    }
}
