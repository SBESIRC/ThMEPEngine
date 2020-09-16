using NetTopologySuite;
using NetTopologySuite.Precision;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPrecisionExtension
    {
        public static Geometry ToFixedNTSLineString(this Polyline polyLine)
        {
            var geometry = polyLine.ToNTSLineString();
            // 精度模型选用Fixed模型（四舍五入，仅保留整数部分）
            var model = NtsGeometryServices.Instance.CreatePrecisionModel(PrecisionModels.Fixed);
            var operation = new PrecisionReducerCoordinateOperation(model, false);
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(operation.Edit(geometry.Coordinates, geometry));
        }
    }
}
