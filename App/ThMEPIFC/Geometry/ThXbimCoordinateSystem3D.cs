using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Spatial.Euclidean;
using Xbim.Common.Geometry;

namespace ThMEPIFC.Geometry
{
    public class ThXbimCoordinateSystem3D
    {
        public CoordinateSystem CS { get; private set; }

        public ThXbimCoordinateSystem3D(XbimMatrix3D m)
        {
            CS = new CoordinateSystem(new DenseMatrix(4, 4, m.ToDoubleArray()));
        }
    }
}
