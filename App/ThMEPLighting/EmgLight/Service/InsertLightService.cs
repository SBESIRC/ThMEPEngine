using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPLighting.EmgLight.Common;

namespace ThMEPLighting.EmgLight.Service
{
    public static class InsertLightService
    {
        public static void InsertSprayBlock(Dictionary<Polyline, (Point3d, Vector3d)> insertPtInfo, double scale)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportBlock(ThMEPLightingCommon.EmgLightBlockName);
                db.Database.ImportLayer (ThMEPLightingCommon.EmgLightLayerName);

                foreach (var ptInfo in insertPtInfo)
                {
                    db.Database.InsertModel(ptInfo.Value.Item1 + ptInfo.Value.Item2 * scale * 2.25, ptInfo.Value.Item2, new Dictionary<string, string>() { }, scale);
                }
            }
        }

        private static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, Dictionary<string, string> attNameValues, double scale)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir, Vector3d.ZAxis);

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    ThMEPLightingCommon.EmgLightLayerName,
                    ThMEPLightingCommon.EmgLightBlockName,
                    pt,
                    new Scale3d(scale),
                    rotateAngle,
                    attNameValues);
            }
        }


    }
}
