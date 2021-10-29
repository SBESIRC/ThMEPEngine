using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Algorithm;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsInterpreter
    {
        public static void Get_basic_param(ObjectId[] obj_list, out ThMEPHVACParam param, out Point2d start_point)
        {
            var list = ThHvacAnalysisComponent.GetValueList(obj_list, ThHvacCommon.RegAppName_Duct_Param);
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
            {
                param = new ThMEPHVACParam();
                start_point = Point2d.Origin;
                return;
            }
            int inc = 0;
            if (values.Count() != 13)
                throw new NotImplementedException("Tringle parameter error");
            param = new ThMEPHVACParam
            {
                is_redraw = (string)values.ElementAt(inc++).Value == "True",
                port_num = Int32.Parse((string)values.ElementAt(inc++).Value),
                air_speed = Double.Parse((string)values.ElementAt(inc++).Value),
                air_volume = Double.Parse((string)values.ElementAt(inc++).Value),
                high_air_volume = Double.Parse((string)values.ElementAt(inc++).Value),
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
        public static void Get_vt_elbow(out List<VTElbowModifyParam> vt_elbows)
        {
            vt_elbows = new List<VTElbowModifyParam>();
            var vt_elbows_ids = ThDuctPortsReadComponent.Read_group_ids_by_type("Vertical_elbow");
            foreach (var id in vt_elbows_ids)
            {
                var param = Get_vt_elbow_by_id(id);
                if (param.handle != ObjectId.Null.Handle)
                    vt_elbows.Add(param);
            }
        }
        public static VTElbowModifyParam Get_vt_elbow_by_id(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var duct_list = ThHvacAnalysisComponent.GetValueList(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_vt_elbow_param(duct_list, id.Handle);
        }
        public static void Get_ducts_dic(out Dictionary<Polyline, DuctModifyParam> dic)
        {
            dic = new Dictionary<Polyline, DuctModifyParam>();
            var ductIds = ThDuctPortsReadComponent.Read_group_ids_by_type("Duct");
            var tor = new Tolerance(1.5, 1.5);
            foreach (var id in ductIds)
            {
                var param = ThHvacAnalysisComponent.GetDuctParamById(id);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep, tor))
                    continue;
                var poly = Create_duct_extends(param);
                dic.Add(poly, param);                    
            }
        }
        public static void Get_shapes_dic(out Dictionary<Polyline, EntityModifyParam> dic)
        {
            dic = new Dictionary<Polyline, EntityModifyParam>();
            var id2geo_dic = ThDuctPortsReadComponent.Read_group_id2geo_dic();
            foreach (var id in id2geo_dic.Keys)
            {
                var param = ThHvacAnalysisComponent.GetConnectorParamById(id);
                if (param.handle != ObjectId.Null.Handle)
                    dic.Add(id2geo_dic[id], param);
            }
        }
        private static Polyline Create_duct_extends(DuctModifyParam param)
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
        public static void Get_texts_dic(out Dictionary<Polyline, TextModifyParam> dic)
        {
            dic = new Dictionary<Polyline, TextModifyParam>();
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
                ThDuctPortsDrawService.Get_hose_dyn_block_properity(id, out double len, out double width);
                var poly = ThMEPHAVCBounds.getHoseBounds(blk, len, width);
                list.Add(poly);
            }
        }
        public static void Get_fan_dic(out Dictionary<Polyline, FanModifyParam> dic)
        {
            dic = new Dictionary<Polyline, FanModifyParam>();
            var fanIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("轴流风机");
            fanIds.AddRange(ThDuctPortsReadComponent.Read_blk_ids_by_name("离心风机"));
            foreach (var id in fanIds)
            {
                var fan = new ThDbModelFan(id);
                if (fan.air_volume < 0)
                    continue;
                var ext_len = Math.Max(fan.FanInlet.Width, fan.FanOutlet.Width);
                var l = new Line(fan.FanInletBasePoint, fan.FanOutletBasePoint);
                var poly = ThMEPHVACService.Get_line_extend(l, ext_len);
                dic.Add(poly, Get_fan_param(id));
            }
        }
        public static void Get_valves_dic(out Dictionary<Polyline, ValveModifyParam> dic)
        {
            dic = new Dictionary<Polyline, ValveModifyParam>();
            Get_valves_dic_by_name(dic, "风阀");
            Get_valves_dic_by_name(dic, "防火阀");
        }
        public static void Get_valves_dic_by_name(Dictionary<Polyline, ValveModifyParam> dic, string valve_name)
        {
            var valveIds = ThDuctPortsReadComponent.Read_blk_ids_by_name(valve_name);
            foreach (var id in valveIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var param = Get_valve_param(id, valve_name);
                var poly = ThMEPHAVCBounds.getValveBounds(blk, param);
                dic.Add(poly, param);
            }
        }
        public static void Get_holes_dic(out Dictionary<Polyline, HoleModifyParam> dic)
        {
            dic = new Dictionary<Polyline, HoleModifyParam>();
            var holeIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("洞口");
            foreach (var id in holeIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_hole_param(id, "洞口"));
            }
        }
        public static void Get_muffler_dic(out Dictionary<Polyline, MufflerModifyParam> dic)
        {
            dic = new Dictionary<Polyline, MufflerModifyParam>();
            var holeIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("阻抗复合式消声器");
            foreach (var id in holeIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_muffler_param(id, "阻抗复合式消声器"));
            }
        }
        public static void Get_ports_dic(out Dictionary<Polyline, PortModifyParam> dic)
        {
            dic = new Dictionary<Polyline, PortModifyParam>();
            var holeIds = ThDuctPortsReadComponent.Read_blk_ids_by_name("风口-AI研究中心");
            foreach (var id in holeIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var poly = new Polyline();
                poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                dic.Add(poly, Get_port_param(id));
            }
        }
        private static TextModifyParam Get_text_param(DBText text)
        {
            return new TextModifyParam(text.Handle,
                                         text.Bounds.Value.CenterPoint().ToPoint2D(),
                                         text.TextString,
                                         text.Rotation,
                                         text.Position);
        }
        private static FanModifyParam Get_fan_param(ObjectId id)
        {
            var param = new FanModifyParam(id.GetBlockName());
            return param;
        }
        private static ValveModifyParam Get_valve_param(ObjectId id, string valve_name)
        {
            var param = new ValveModifyParam();
            ThDuctPortsDrawService.Get_valve_dyn_block_properity(id, out Point3d insert_p, out double width,
                    out double height, out double text_angle, out double rotate_angle, out string valve_visibility);
            var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(rotate_angle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
            param.handle = id.Handle;
            param.valve_name = valve_name;
            param.valve_layer = id.GetBlockLayer();
            param.valve_visibility = valve_visibility;
            param.insert_p = insert_p.ToPoint2D();
            param.rotate_angle = rotate_angle;
            param.width = width;
            param.height = height;
            param.text_angle = text_angle;
            return param;
        }
        private static HoleModifyParam Get_hole_param(ObjectId id, string hole_name)
        {
            var param = new HoleModifyParam();
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
        private static MufflerModifyParam Get_muffler_param(ObjectId id, string muffler_name)
        {
            var param = new MufflerModifyParam();
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
        public static PortModifyParam Get_port_param(ObjectId id)
        {
            var param = new PortModifyParam();
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
        public static VTElbowModifyParam Get_vt_elbow_param(TypedValueList list, Handle group_handle)
        {
            var param = new VTElbowModifyParam();
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
                    list = ThHvacAnalysisComponent.GetValueList(g_ids, ThHvacCommon.RegAppName_Duct_Info);
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