using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ThMEPIFC.Geometry
{
    public class ThXbimCoordinateSystem3D
    {
        public CoordinateSystem CS { get; private set; }

        public static ThXbimCoordinateSystem3D Identity => new ThXbimCoordinateSystem3D();

        public ThXbimCoordinateSystem3D()
        {
            CS = CreateCoordinateSystem(XbimMatrix3D.Identity);
        }

        public ThXbimCoordinateSystem3D(ThTCHMatrix3d m)
        {
            CS = CreateCoordinateSystem(ToXbimMatrix3D(m));
        }

        private CoordinateSystem CreateCoordinateSystem(XbimMatrix3D m)
        {
            return new CoordinateSystem(new DenseMatrix(4, 4, m.ToDoubleArray()));
        }

        private XbimMatrix3D ToXbimMatrix3D(ThTCHMatrix3d m)
        {
            return new XbimMatrix3D(
                m.Data11, m.Data12, m.Data13, m.Data14,
                m.Data21, m.Data22, m.Data23, m.Data24,
                m.Data31, m.Data32, m.Data33, m.Data34,
                m.Data41, m.Data42, m.Data43, m.Data44);
        }
    }
}
