using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsRecoder
    {
        public static void Attach_start_param(ObjectId id, ThMEPHVACParam param)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.is_redraw},
                    { (int)DxfCode.ExtendedDataAsciiString, param.port_num},
                    { (int)DxfCode.ExtendedDataAsciiString, param.air_speed},
                    { (int)DxfCode.ExtendedDataAsciiString, param.air_volume},
                    { (int)DxfCode.ExtendedDataAsciiString, param.high_air_volume},
                    { (int)DxfCode.ExtendedDataAsciiString, param.elevation},
                    { (int)DxfCode.ExtendedDataAsciiString, param.main_height},
                    { (int)DxfCode.ExtendedDataAsciiString, param.scale},
                    { (int)DxfCode.ExtendedDataAsciiString, param.scenario},
                    { (int)DxfCode.ExtendedDataAsciiString, param.port_size},
                    { (int)DxfCode.ExtendedDataAsciiString, param.port_name},
                    { (int)DxfCode.ExtendedDataAsciiString, param.port_range},
                    { (int)DxfCode.ExtendedDataAsciiString, param.in_duct_size}
                };
                id.AddXData(ThHvacCommon.RegAppName_Duct_Param, value_list);
            }
        }
        public static Handle Create_duct_group(ObjectIdList geo_ids,
                                               ObjectIdList flg_ids,
                                               ObjectIdList center_ids,
                                               ObjectIdList ports_ids,
                                               ObjectIdList ext_ports_ids,
                                               DuctModifyParam param)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = Collect_ids(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids);
                if (ids.Count == 0)
                    return ObjectId.Null.Handle;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.start_handle.ToString()},
                    { (int)DxfCode.ExtendedDataAsciiString, param.type},
                    { (int)DxfCode.ExtendedDataAsciiString, param.air_volume.ToString("0.00")},
                    { (int)DxfCode.ExtendedDataAsciiString, param.elevation.ToString("0.00")},
                    { (int)DxfCode.ExtendedDataAsciiString, param.duct_size},
                };
                
                id.AddXData(ThHvacCommon.RegAppName_Duct_Info, value_list);

                double width = ThMEPHVACService.Get_width(param.duct_size);
                var widths = new List<double>() { width, width };
                SetPortInfoXdata(ports_ids, ext_ports_ids, widths);
                param.handle = id.Handle;
                return id.Handle;
            }
        }
        public static Handle Create_group( ObjectIdList geo_ids,
                                           ObjectIdList flg_ids,
                                           ObjectIdList center_ids,
                                           ObjectIdList ports_ids,
                                           ObjectIdList ext_ports_ids,
                                           EntityModifyParam param)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = Collect_ids(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids);
                if (ids.Count == 0)
                    return ObjectId.Null.Handle;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, param.start_id.ToString()},
                    { (int)DxfCode.ExtendedDataAsciiString, param.type}
                };
                id.AddXData(ThHvacCommon.RegAppName_Duct_Info, value_list);
                SetPortInfoXdata(ports_ids, ext_ports_ids, param.port_widths);

                return id.Handle;
            }
        }
        public static void Create_vt_elbow_group(ObjectIdList geo_ids,
                                                 ObjectIdList flg_ids,
                                                 ObjectIdList center_ids,
                                                 ObjectIdList ports_ids,
                                                 ObjectIdList ext_ports_ids)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = Collect_ids(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids);
                if (ids.Count == 0)
                    return ;
                var id = GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), ids);
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, id.Handle},
                    { (int)DxfCode.ExtendedDataAsciiString, "Vertical_elbow"}
                };
                id.AddXData(ThHvacCommon.RegAppName_Duct_Info, value_list);
                SetPortInfoXdata(ports_ids);
            }
        }
        private static void SetPortInfoXdata(ObjectIdList ports_ids)
        {
            //set port xdata
            for (int i = 0; i < ports_ids.Count; ++i)
            {
                var port_value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, "Port"},
                    { (int)DxfCode.ExtendedDataAsciiString, i.ToString()},
                };

                ports_ids[i].AddXData(ThHvacCommon.RegAppName_Duct_Info, port_value_list);
            }
        }
        private static void SetPortInfoXdata(ObjectIdList ports_ids, ObjectIdList ext_ports_ids, List<double> widths)
        {
            //set port xdata
            for (int i = 0; i < ports_ids.Count; ++i)
            {
                var port_value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, "Port"},
                    { (int)DxfCode.ExtendedDataAsciiString, i.ToString()},
                    { (int)DxfCode.ExtendedDataAsciiString, widths[i].ToString()}
                };

                ports_ids[i].AddXData(ThHvacCommon.RegAppName_Duct_Info, port_value_list);
            }

            //set port ext xdata
            for (int i = 0; i < ext_ports_ids.Count; ++i)
            {
                var ext_port_value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, "PortExt"},
                    { (int)DxfCode.ExtendedDataAsciiString, i.ToString()},
                };

                ext_ports_ids[i].AddXData(ThHvacCommon.RegAppName_Duct_Info, ext_port_value_list);
            }
        }
        private static ObjectIdList Collect_ids(ObjectIdList geo_ids, 
                                                ObjectIdList flg_ids, 
                                                ObjectIdList center_ids,
                                                ObjectIdList ports_ids,
                                                ObjectIdList ext_ports_ids)
        {
            var ids = new ObjectIdList();
            ids.AddRange(geo_ids);
            ids.AddRange(flg_ids);
            ids.AddRange(center_ids);
            ids.AddRange(ports_ids);
            ids.AddRange(ext_ports_ids);
            return ids;
        }
        private static ObjectIdList Collect_ids(ObjectIdList geo_ids, ObjectIdList flg_ids, ObjectIdList center_ids)
        {
            var ids = new ObjectIdList();
            ids.AddRange(geo_ids);
            ids.AddRange(flg_ids);
            ids.AddRange(center_ids);
            return ids;
        }
        private static ObjectIdList Collect_ids(ObjectIdList geo_ids, ObjectIdList flg_ids)
        {
            var ids = new ObjectIdList();
            ids.AddRange(geo_ids);
            ids.AddRange(flg_ids);
            return ids;
        }
    }
}