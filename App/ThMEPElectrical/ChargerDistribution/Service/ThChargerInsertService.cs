using System.Collections.Generic;

using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPElectrical.ChargerDistribution.Common;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public class ThChargerInsertService
    {
        public static ObjectId Insert(string layer, string name, Point3d position, Scale3d scale, double rotation)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layer,
                    name,
                    position,
                    scale,
                    rotation,
                    new Dictionary<string, string>());
            }
        }

        public static BlockReference InsertDimension(string layer, string name, Point3d position, Scale3d scale, double rotation, string number)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var dictionary = new Dictionary<string, string>
                {
                    { ThChargerDistributionCommon.Circuit_Number_1, number },
                    { ThChargerDistributionCommon.Circuit_Number_2, "" },
                };
                var objectId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layer,
                    name,
                    position,
                    scale,
                    rotation,
                    dictionary);
                return acadDatabase.Element<BlockReference>(objectId, true);
            }
        }
    }
}
