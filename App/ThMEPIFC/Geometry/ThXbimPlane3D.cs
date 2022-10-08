using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;

namespace ThMEPIFC.Geometry
{
    public class ThXbimPlane3D
    {
        public Plane Geometry { get;private set; }

        public ThXbimPlane3D(XbimPoint3D rootPoint, XbimVector3D normal)
        {
            Geometry = new Plane(rootPoint.ToPoint3D(), normal.ToUnitVector3D());
        }
    }
}
