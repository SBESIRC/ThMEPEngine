using Linq2Acad;
using DotNetARX;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public static partial class ThTCHDbExtension
    {
        public static ThRawIfcFlowSegmentData LoadCableCarrierSegmentFromDb(this Database database, ObjectId tch, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var curve = GetCurve(tch);
                return new ThRawIfcFlowSegmentData()
                {
                    Geometry = GetTCHCableCarrierSegmentGeometry(curve, matrix),
                };
            }
        }

        public static ThRawIfcFlowFittingData LoadTCHCableCarrierFitting(this Database database, ObjectId tch, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var curve = GetCurve(tch);
                return new ThRawIfcFlowFittingData()
                {
                    Geometry = GetTCHCableCarrierFittingGeometry(curve, matrix),
                };
            }
        }

        private static Curve GetTCHCableCarrierSegmentGeometry(Curve curve, Matrix3d matrix)
        {
            var line = new Line(curve.StartPoint, curve.EndPoint);
            return line.GetTransformedCopy(matrix) as Curve;
        }

        private static Curve GetTCHCableCarrierFittingGeometry(Curve curve, Matrix3d matrix)
        {
            var vertices = new Point3dCollection();
            for (double parameter = curve.StartParam; parameter <= curve.EndParam; parameter += 1.0)
            {
                vertices.Add(curve.GetPointAtParameter(parameter));
            }
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(vertices);
            return pline.GetTransformedCopy(matrix) as Curve;
        }
    }
}
