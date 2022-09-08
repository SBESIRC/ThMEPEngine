using System;
using Linq2Acad;
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
                Curve GetTransformedCurve()
                {
                    var line = new Line(curve.StartPoint, curve.EndPoint);
                    return line.GetTransformedCopy(matrix) as Curve;
                }
                return new ThRawIfcFlowSegmentData()
                {
                    Geometry = GetTransformedCurve(),
                };
            }
        }

        public static ThRawIfcFlowFittingData LoadTCHCableCarrierFitting(this Database database, ObjectId tch, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }
    }
}
