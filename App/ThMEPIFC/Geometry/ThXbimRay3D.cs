using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;

namespace ThMEPIFC.Geometry
{
    public class ThXbimRay3D
    {
        public Ray3D Geometry { get; private set; }

        public ThXbimRay3D(XbimPoint3D throughPoint, XbimVector3D direction)
        {
            Geometry = new Ray3D(throughPoint.ToPoint3D(), direction.ToUnitVector3D());
        }
    }
}
