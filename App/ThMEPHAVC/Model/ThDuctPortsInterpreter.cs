using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsInterpreter
    {
        public static void Get_basic_param(ObjectId[] obj_list, out DuctPortsParam param, out Point2d start_point)
        {
            var list = Do_get_value_list(obj_list, ThHvacCommon.RegAppName_Duct_Param);
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
            {
                param = new DuctPortsParam();
                start_point = Point2d.Origin;
                return;
            }
            param = new DuctPortsParam
            {
                scale = (string)values.ElementAt(0).Value,
                scenario = (string)values.ElementAt(1).Value,
                elevation = Double.Parse((string)values.ElementAt(2).Value),
                main_height = Double.Parse((string)values.ElementAt(3).Value),
                port_range = (string)values.ElementAt(4).Value,
            };
            var blk = obj_list[0].GetEntity() as BlockReference;
            start_point = blk.Position.ToPoint2D();
        }
        public static void Get_shapes(out List<Entity_modify_param> shapes)
        {
            shapes = new List<Entity_modify_param>();
            var shapeIds = ThDuctPortsReadComponent.Read_shape_ids();
            foreach(var id in shapeIds)
                shapes.Add(Get_shape_by_id(id));
        }
        public static Entity_modify_param Get_shape_by_id(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var entity_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_entity_param(entity_list, id.Handle);
        }
        public static void Get_ducts(out List<Duct_modify_param> ducts)
        {
            ducts = new List<Duct_modify_param>();
            var ductIds = ThDuctPortsReadComponent.Read_duct_ids();
            foreach (var id in ductIds)
                ducts.Add(Get_duct_by_id(id));
        }
        public static Duct_modify_param Get_duct_by_id(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var duct_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_duct_param(duct_list, id.Handle);
        }
        public static void Get_valves(out List<Valve_modify_param> valves)
        {
            valves = new List<Valve_modify_param>();
            var valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风阀");
            foreach (var id in valveIds)
                valves.Add(Get_valve_param(id, "风阀"));
        }
        public static void Get_ports(out List<Port_modify_param> ports)
        {
            ports = new List<Port_modify_param>();
            var valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风口-AI研究中心");
            foreach (var id in valveIds)
                ports.Add(Get_port_param(id));
        }
        public static void Get_texts(out List<Text_modify_param> texts)
        {
            texts = new List<Text_modify_param>();
            var textIds = ThDuctPortsReadComponent.Read_texts();
            foreach (var text in textIds)
                texts.Add(Get_text_param(text));
        }
        private static Text_modify_param Get_text_param(DBText text)
        {
            return new Text_modify_param(text.Handle,
                                         text.Bounds.Value.CenterPoint().ToPoint2D(),
                                         text.TextString,
                                         text.Rotation,
                                         text.Position);
        }
        private static Valve_modify_param Get_valve_param(ObjectId id, string valve_name)
        {
            var param = new Valve_modify_param();
            ThDuctPortsDrawService.Get_valve_dyn_block_properity(id, out Point3d insert_p, out double width, 
                    out double height, out double text_angle, out double rotate_angle, out string valve_visibility);
            var dir_vec = ThDuctPortsService.Get_dir_vec_by_angle(rotate_angle - Math.PI * 0.5);
            var vertical_r = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            param.handle = id.Handle;
            param.valve_name = valve_name;
            param.valve_layer = id.GetBlockLayer();
            param.valve_visibility = valve_visibility;
            param.insert_p = insert_p.ToPoint2D();
            param.judge_p = param.insert_p - 0.5 * width * vertical_r;
            param.rotate_angle = rotate_angle;
            param.width = width;
            param.height = height;
            param.text_angle = text_angle;
            return param;
        }
        public static Port_modify_param Get_port_param(ObjectId id)
        {
            var param = new Port_modify_param();
            ThDuctPortsDrawService.Get_port_dyn_block_properity(id, out Point3d pos, out string port_range, 
                out double port_height, out double port_width, out double rotate_angle);
            param.handle = id.Handle;
            param.rotate_angle = rotate_angle;
            param.port_width = port_width;
            param.port_height = port_height;
            param.port_range = port_range;
            param.pos = pos.ToPoint2D();
            return param;
        }
        public static Duct_modify_param Get_duct_param(TypedValueList list, Handle group_handle)
        {
            var param = new Duct_modify_param();
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
                return param;
            int inc = 0;
            param.handle = group_handle;
            param.start_handle = ThDuctPortsService.Covert_obj_to_handle(values.ElementAt(inc++).Value);
            param.type = (string)values.ElementAt(inc++).Value;
            if (param.type != "Duct")
                return param;
            param.air_volume = Double.Parse((string)values.ElementAt(inc++).Value);
            param.duct_size = (string)values.ElementAt(inc++).Value;
            using (var db = AcadDatabase.Active())
            {
                var id = db.Database.GetObjectId(false, group_handle, 0);
                var portIndex2PositionDic = ThDuctPortsReadComponent.GetPortsOfGroup(id);
                var portExtIndex2PositionDic = ThDuctPortsReadComponent.GetPortExtsOfGroup(id);
                param.sp = portIndex2PositionDic["0"].Item1.ToPoint2D();
                param.ep = portIndex2PositionDic["1"].Item1.ToPoint2D();
            }                
            return param;
        }
        private static Entity_modify_param Get_entity_param(TypedValueList list, Handle group_handle)
        {
            var param = new Entity_modify_param();
            if (list.Count > 0)
            {
                var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                int inc = 0;
                param.handle = group_handle;
                param.start_id = ThDuctPortsService.Covert_obj_to_handle(values.ElementAt(inc++).Value);
                param.type = (string)values.ElementAt(inc++).Value;
                if (!values.Any() || param.type == "Duct")
                    return param;
                using (var db = AcadDatabase.Active())
                {
                    var id = db.Database.GetObjectId(false, group_handle, 0);
                    var portIndex2PositionDic = ThDuctPortsReadComponent.GetPortsOfGroup(id);
                    var portExtIndex2PositionDic = ThDuctPortsReadComponent.GetPortExtsOfGroup(id);
                    for (int i = 0; i < portIndex2PositionDic.Count; ++i)
                    {
                        param.pos.Add(portIndex2PositionDic[i.ToString()].Item1.ToPoint2D());
                        param.pos_ext.Add(portExtIndex2PositionDic[i.ToString()].ToPoint2D());
                        param.port_widths.Add(Double.Parse(portIndex2PositionDic[i.ToString()].Item2));
                    }
                }
            }
            return param;
        }
        public static TypedValueList Do_get_value_list(IEnumerable<ObjectId> g_ids, string reg_app_name)
        {
            var list = new TypedValueList();
            foreach (var g_id in g_ids)
            {
                list = g_id.GetXData(reg_app_name);
                if (list == null)
                    continue;
                break;
            }
            return list;
        }
        public static string Get_entity_type(ObjectId id)
        {
            var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
            if (list != null)
            {
                var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                if (values.Any())
                {
                    return (string)values.ElementAt(1).Value;
                }
            }
            return String.Empty;
        }
        public static TypedValueList Get_value_list(ObjectId[] obj_ids)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var id in obj_ids)
                {
                    var g_ids = id.GetGroups();
                    if (g_ids == null)
                        continue;
                    list = Do_get_value_list(g_ids, ThHvacCommon.RegAppName_Duct_Info);
                    if (list == null)
                        continue;
                    if (list.Count != 0)
                        break;
                }
                return list;
            }
        }
    }
}