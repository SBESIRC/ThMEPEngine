using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsInterpreter
    {
        public static void Get_basic_param(ObjectId[] obj_list, out ThMEPHVACParam param, out Point2d start_point)
        {
            var list = Do_get_value_list(obj_list, ThHvacCommon.RegAppName_Duct_Param);
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
            {
                param = new ThMEPHVACParam();
                start_point = Point2d.Origin;
                return;
            }
            int inc = 0;
            if (values.Count() != 12)
                throw new NotImplementedException("Tringle parameter error");
            param = new ThMEPHVACParam
            {
                is_redraw = (string)values.ElementAt(inc++).Value == "True",
                port_num = Int32.Parse((string)values.ElementAt(inc++).Value),
                air_speed = Double.Parse((string)values.ElementAt(inc++).Value),
                air_volume = Double.Parse((string)values.ElementAt(inc++).Value),
                elevation = Double.Parse((string)values.ElementAt(inc++).Value),
                main_height = Double.Parse((string)values.ElementAt(inc++).Value),
                scale = (string)values.ElementAt(inc++).Value,
                scenario = (string)values.ElementAt(inc++).Value,
                port_size = (string)values.ElementAt(inc++).Value,
                port_name = (string)values.ElementAt(inc++).Value,
                port_range = (string)values.ElementAt(inc++).Value,
                in_duct_size = (string)values.ElementAt(inc++).Value,
            };
            var blk = obj_list[0].GetEntity() as BlockReference;
            start_point = blk.Position.ToPoint2D();
        }
        public static Entity_modify_param Get_shape_by_id(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var entity_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_entity_param(entity_list, id.Handle);
        }
        public static void Get_vt_elbow(out List<VT_elbow_modify_param> vt_elbows)
        {
            vt_elbows = new List<VT_elbow_modify_param>();
            var vt_elbows_ids = ThDuctPortsReadComponent.Read_group_ids_by_type("Vertical_elbow");
            foreach (var id in vt_elbows_ids)
            {
                var param = Get_vt_elbow_by_id(id);
                if (param.handle != ObjectId.Null.Handle)
                    vt_elbows.Add(param);
            }
        }
        public static Duct_modify_param Get_duct_by_id(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var duct_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_duct_param(duct_list, id.Handle);
        }
        public static VT_elbow_modify_param Get_vt_elbow_by_id(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var duct_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_vt_elbow_param(duct_list, id.Handle);
        }
        public static void Get_valves(out List<Valve_modify_param> valves)
        {
            valves = new List<Valve_modify_param>();
            var valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风阀");
            foreach (var id in valveIds)
                valves.Add(Get_valve_param(id, "风阀"));
            valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("防火阀");
            foreach (var id in valveIds)
                valves.Add(Get_valve_param(id, "防火阀"));
        }
        public static void Get_ducts_dic(out Dictionary<Polyline, Duct_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Duct_modify_param>();
            var ductIds = ThDuctPortsReadComponent.Read_group_ids_by_type("Duct");
            var tor = new Tolerance(1.5, 1.5);
            foreach (var id in ductIds)
            {
                var param = Get_duct_by_id(id);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep, tor))
                    continue;
                var poly = Create_duct_extends(param);
                dic.Add(poly, param);                    
            }
        }
        public static void Get_shapes_dic(out Dictionary<Polyline, Entity_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Entity_modify_param>();
            var id2geo_dic = ThDuctPortsReadComponent.Read_group_id2geo_dic();
            foreach (var id in id2geo_dic.Keys)
            {
                var param = Get_shape_by_id(id);
                if (param.handle != ObjectId.Null.Handle)
                    dic.Add(id2geo_dic[id], param);
            }
        }
        private static Polyline Create_duct_extends(Duct_modify_param param)
        {
            var dir_vec = (param.ep - param.sp).GetNormal();
            var sp2 = param.sp - dir_vec;
            var ep2 = param.ep + dir_vec;
            var sp = new Point3d(sp2.X, sp2.Y, 0);
            var ep = new Point3d(ep2.X, ep2.Y, 0);
            var l = new Line(sp, ep);
            var width = ThMEPHVACService.Get_width(param.duct_size);
            return ThMEPHVACService.Get_line_extend(l, width + 2);//对管段的外包框上下左右都扩1
        }        
        public static void Get_texts_dic(out Dictionary<Polyline, Text_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Text_modify_param>();
            var texts = ThDuctPortsReadComponent.Read_duct_texts();
            foreach (var t in texts)
            {
                var poly = new Polyline();
                poly.CreateRectangle(t.Bounds.Value.MinPoint.ToPoint2D(), t.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_text_param(t));
            }
        }
        public static void Get_hose_bounds(out DBObjectCollection list)
        {
            list = new DBObjectCollection();
            var hoseIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风机软接");
            foreach (var id in hoseIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                list.Add(poly);
            }
        }
        public static void Get_fan_dic(out Dictionary<Polyline, Fan_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Fan_modify_param>();
            var fanIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("轴流风机");
            fanIds.AddRange(ThDuctPortsReadComponent.Read_blk_ids_by_name("离心风机"));
            foreach (var id in fanIds)
            {
                var fan = new ThDbModelFan(id);
                var ext_len = Math.Max(fan.FanInlet.Width, fan.FanOutlet.Width);
                var p1 = fan.FanInletBasePoint.ToPoint2D();
                var p2 = fan.FanOutletBasePoint.ToPoint2D();
                var dir_vec = (p1 - p2).GetNormal();
                var l_vec = ThMEPHVACService.Get_left_vertical_vec(dir_vec);
                var r_vec = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var poly = new Polyline();
                poly.CreateRectangle(p1 + l_vec * 0.5 * ext_len, p2 + r_vec * 0.5 * ext_len);
                dic.Add(poly, Get_fan_param(id));
            }
        }
        public static void Get_valves_dic(out Dictionary<Polyline, Valve_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Valve_modify_param>();
            var valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风阀");
            foreach (var id in valveIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_valve_param(id, "风阀"));
            }
            valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("防火阀");
            foreach (var id in valveIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_valve_param(id, "防火阀"));
            }
        }
        public static void Get_holes_dic(out Dictionary<Polyline, Hole_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Hole_modify_param>();
            var holeIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("洞口");
            foreach (var id in holeIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_hole_param(id, "洞口"));
            }
        }
        public static void Get_muffler_dic(out Dictionary<Polyline, Muffler_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Muffler_modify_param>();
            var holeIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("阻抗复合式消声器");
            foreach (var id in holeIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_muffler_param(id, "阻抗复合式消声器"));
            }
        }
        public static void Get_ports_dic(out Dictionary<Polyline, Port_modify_param> dic)
        {
            dic = new Dictionary<Polyline, Port_modify_param>();
            var holeIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风口-AI研究中心");
            foreach (var id in holeIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_port_param(id));
            }
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
            var textIds = ThDuctPortsReadComponent.Read_duct_texts();
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
        private static Fan_modify_param Get_fan_param(ObjectId id)
        {
            var param = new Fan_modify_param(id.GetBlockName());
            return param;
        }
        private static Valve_modify_param Get_valve_param(ObjectId id, string valve_name)
        {
            var param = new Valve_modify_param();
            ThDuctPortsDrawService.Get_valve_dyn_block_properity(id, out Point3d insert_p, out double width,
                    out double height, out double text_angle, out double rotate_angle, out string valve_visibility);
            var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(rotate_angle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
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
        private static Hole_modify_param Get_hole_param(ObjectId id, string hole_name)
        {
            var param = new Hole_modify_param();
            ThDuctPortsDrawService.Get_hole_dyn_block_properity(id, out Point3d insert_p, out double len, out double width, out double rotate_angle);
            param.handle = id.Handle;
            param.hole_name = hole_name;
            param.hole_layer = id.GetBlockLayer();
            param.insert_p = insert_p.ToPoint2D();
            param.len = len;
            param.width = width;
            param.rotate_angle = rotate_angle;
            return param;
        }
        private static Muffler_modify_param Get_muffler_param(ObjectId id, string muffler_name)
        {
            var param = new Muffler_modify_param();
            ThDuctPortsDrawService.Get_muffler_dyn_block_properity(id, out Point3d insert_p, out string visibility, out double len,
                                                                   out double height, out double width, out double text_height, out double rotate_angle);
            param.handle = id.Handle;
            param.name = muffler_name;
            param.muffler_layer = id.GetBlockLayer();
            param.insert_p = insert_p.ToPoint2D();
            param.len = len;
            param.width = width;
            param.muffler_visibility = visibility;
            param.height = height;
            param.text_height = text_height;
            param.rotate_angle = rotate_angle;
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
            if (values.Count() != 5)
                return param;
            int inc = 0;
            param.handle = group_handle;
            param.start_handle = ThMEPHVACService.Covert_obj_to_handle(values.ElementAt(inc++).Value);
            param.type = (string)values.ElementAt(inc++).Value;
            if (param.type != "Duct" && param.type != "Vertical_bypass")
                return param;
            param.air_volume = Double.Parse((string)values.ElementAt(inc++).Value);
            param.elevation = Double.Parse((string)values.ElementAt(inc++).Value);
            param.duct_size = (string)values.ElementAt(inc++).Value;
            using (var db = AcadDatabase.Active())
            {
                var id = db.Database.GetObjectId(false, group_handle, 0);
                var portIndex2PositionDic = ThDuctPortsReadComponent.GetPortsOfGroup(id);
                if (portIndex2PositionDic.Count == 0)
                    return param;
                param.sp = portIndex2PositionDic["0"].Item1.ToPoint2D();
                param.ep = portIndex2PositionDic["1"].Item1.ToPoint2D();
            }
            return param;
        }
        public static VT_elbow_modify_param Get_vt_elbow_param(TypedValueList list, Handle group_handle)
        {
            var param = new VT_elbow_modify_param();
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
                return param;
            param.handle = group_handle;
            var type = (string)values.ElementAt(1).Value;
            if (type != "Vertical_elbow")
                return param;
            using (var db = AcadDatabase.Active())
            {
                var id = db.Database.GetObjectId(false, group_handle, 0);
                var portIndex2PositionDic = ThDuctPortsReadComponent.GetBypassPortsOfGroup(id);
                if (portIndex2PositionDic.Count == 0)
                    return param;
                param.detect_p = portIndex2PositionDic["0"].ToPoint2D();
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
                param.start_id = ThMEPHVACService.Covert_obj_to_handle(values.ElementAt(inc++).Value);
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
        public static string Get_entity_type(ObjectId[] obj_ids)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var id in obj_ids)
                {
                    var groups = id.GetGroups();
                    if (groups == null)
                        return String.Empty;
                    foreach (var g in groups)
                    {
                        var type = Get_entity_type(g);
                        if (type != "")
                            return type;
                    }
                }
                return String.Empty;
            }
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