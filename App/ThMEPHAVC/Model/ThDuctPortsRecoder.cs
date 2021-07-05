using System;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsRecoder
    {
        public static ObjectId Create_duct_group(ObjectIdList geo_ids,
                                                 ObjectIdList flg_ids,
                                                 ObjectIdList center_ids,
                                                 Duct_modify_param param)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var ids = Collect_ids(geo_ids, flg_ids, center_ids);
                if (ids.Count == 0)
                    return ObjectId.Null;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.identity_info.start_id},
                    { (int)DxfCode.ExtendedDataAsciiString, id.ToString()},
                    { (int)DxfCode.ExtendedDataAsciiString, param.air_volume.ToString("0.00")},
                    { (int)DxfCode.ExtendedDataAsciiString, param.duct_size}
                };
                for (int i = 0; i < param.identity_info.pos.Count; ++i)
                {
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, param.identity_info.pos[i].ToString());
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, param.identity_info.pos_ext[i].ToString());
                }
                id.AddXData("Duct", value_list);
                return id;
            }
        }
        public static ObjectId Create_reducing_group(ObjectIdList geo_ids,
                                                     ObjectIdList flg_ids,
                                                     ObjectIdList center_ids,
                                                     Duct_modify_param param)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var ids = Collect_ids(geo_ids, flg_ids, center_ids);
                if (ids.Count == 0)
                    return ObjectId.Null;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.identity_info.start_id},
                    { (int)DxfCode.ExtendedDataAsciiString, id.ToString()},
                };
                for (int i = 0; i < param.identity_info.pos.Count; ++i)
                {
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, param.identity_info.pos[i].ToString());
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, param.identity_info.pos_ext[i].ToString());
                }
                id.AddXData("Reducing", value_list);
                return id;
            }
        }
        public static ObjectId Create_group(ObjectIdList geo_ids,
                                            ObjectIdList flg_ids,
                                            ObjectIdList center_ids,
                                            string entity_name,
                                            Entity_modify_param param)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var ids = Collect_ids(geo_ids, flg_ids, center_ids);
                if (ids.Count == 0)
                    return ObjectId.Null;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.start_id},
                    { (int)DxfCode.ExtendedDataAsciiString, id.ToString()},
                };
                for (int i = 0; i < param.pos.Count; ++i)
                {
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, param.pos[i].ToString());
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, param.pos_ext[i].ToString());
                }
                id.AddXData(entity_name, value_list);
                return id;
            }
        }
        private static ObjectIdList Collect_ids(ObjectIdList geo_ids, ObjectIdList flg_ids, ObjectIdList center_ids)
        {
            var ids = new ObjectIdList();
            ids.AddRange(geo_ids);
            ids.AddRange(flg_ids);
            ids.AddRange(center_ids);
            return ids;
        }
    }
}