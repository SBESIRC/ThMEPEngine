using System;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsRecoder
    {
        public static Handle CreateDuctGroup(ObjectIdList geoIds, ObjectIdList flgIds, ObjectIdList centerIds, DuctModifyParam param)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = CollectIds(geoIds, flgIds, centerIds);
                if (ids.Count == 0)
                    return ObjectId.Null.Handle;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var valueList = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.type},
                    { (int)DxfCode.ExtendedDataAsciiString, param.airVolume.ToString("0.00")},
                    { (int)DxfCode.ExtendedDataAsciiString, param.elevation.ToString("0.00")},
                    { (int)DxfCode.ExtendedDataAsciiString, param.ductSize},
                };
                
                id.AddXData(ThHvacCommon.RegAppName_Duct_Info, valueList);
                param.handle = id.Handle;
                return id.Handle;
            }
        }
        public static Handle CreateGroup(ObjectIdList geoIds, ObjectIdList flgIds, ObjectIdList centerIds, string type)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = CollectIds(geoIds, flgIds, centerIds);
                if (ids.Count == 0)
                    return ObjectId.Null.Handle;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList { { (int)DxfCode.ExtendedDataAsciiString, type} };
                id.AddXData(ThHvacCommon.RegAppName_Duct_Info, value_list);

                return id.Handle;
            }
        }
        public static void CreateVtElbowGroup(ObjectIdList geoIds, ObjectIdList flgIds, ObjectIdList centerIds)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = CollectIds(geoIds, flgIds, centerIds);
                if (ids.Count == 0)
                    return ;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, id.Handle},
                    { (int)DxfCode.ExtendedDataAsciiString, "Vertical_elbow"}
                };
                id.AddXData(ThHvacCommon.RegAppName_Duct_Info, value_list);
            }
        }
        private static ObjectIdList CollectIds(ObjectIdList geoIds, ObjectIdList flgIds, ObjectIdList centerIds)
        {
            var ids = new ObjectIdList();
            ids.AddRange(geoIds);
            ids.AddRange(flgIds);
            ids.AddRange(centerIds);
            return ids;
        }
    }
}