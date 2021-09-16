using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawService
    {
        public string geo_layer;
        public string flg_layer;
        public string port_layer;
        public string air_valve_layer;
        public string electrycity_valve_layer;
        public string start_layer;
        public string center_layer;
        public string duct_size_layer;
        public string dimension_layer;
        public string port_mark_layer;
        public string hole_layer;
        public string air_valve_name;
        public string electrycity_valve_name;
        public string port_name;
        public string start_name;
        public string port_mark_name;
        private string air_valve_visibility;
        public string electrycity_valve_visibility;
        public ThDuctPortsDrawDim dim_service;
        public ThDuctPortsDrawValve air_valve_service;
        public ThDuctPortsDrawText text_service;
        public ThDuctPortsDrawPort port_service;
        public ThDuctPortsDrawService(string scenario, string scale)
        {
            air_valve_name = "风阀";
            start_name = "AI-风管起点";
            port_mark_name = "风口标注";
            port_name = "风口-AI研究中心";
            air_valve_visibility = "多叶调节风阀";
            electrycity_valve_visibility = "电动多叶调节风阀";
            Set_layer(scenario);
            Import_Layer_Block();
            Pre_proc_layer();
            dim_service = new ThDuctPortsDrawDim(dimension_layer, scale);
            air_valve_service = new ThDuctPortsDrawValve(air_valve_visibility, air_valve_name, air_valve_layer);
            text_service = new ThDuctPortsDrawText(duct_size_layer);
            port_service = new ThDuctPortsDrawPort(port_layer, port_name);
        }
        private void Set_layer(string scenario)
        {
            switch (scenario)
            {
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                    geo_layer = "H-DUCT-DUAL";
                    flg_layer = "H-DAPP-DAPP";
                    port_layer = "H-DAPP-DGRIL";
                    center_layer = "H-DUCT-DUAL-MID";
                    air_valve_layer = "H-DAPP-DDAMP";
                    duct_size_layer = "H-DIMS-DUAL";
                    dimension_layer = "H-DIMS-DUAL";
                    port_mark_layer = "H-DIMS-DUAL";
                    break;
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    geo_layer = "H-DUCT-FIRE";
                    flg_layer = "H-DAPP-FAPP";
                    port_layer = "H-DAPP-FGRIL";
                    center_layer = "H-DUCT-FIRE-MID";
                    air_valve_layer = "H-DAPP-FDAMP";
                    duct_size_layer = "H-DIMS-FIRE";
                    dimension_layer = "H-DIMS-FIRE";
                    port_mark_layer = "H-DIMS-FIRE";
                    hole_layer = "H-HOLE";
                    break;
                case "平时送风":
                case "平时排风":
                case "事故排风":
                case "事故补风":
                case "平时送风兼事故补风":
                case "平时排风兼事故排风":
                case "厨房排油烟补风":
                case "厨房排油烟":
                    geo_layer = "H-DUCT-VENT";
                    flg_layer = "H-DAPP-AAPP";
                    port_layer = "H-DAPP-GRIL";
                    center_layer = "H-DUCT-VENT-MID";
                    air_valve_layer = "H-DAPP-DAMP";
                    duct_size_layer = "H-DIMS-DUCT";
                    dimension_layer = "H-DIMS-DUCT";
                    port_mark_layer = "H-DIMS-DUCT";
                    break;
                default:throw new NotImplementedException("No such scenior!");
            }
            start_layer = "AI-风管起点";
            electrycity_valve_layer = "H-DAPP-FDAMP";
        }
        private void Import_Layer_Block()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(geo_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(flg_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(port_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(air_valve_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(start_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(center_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(duct_size_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(dimension_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(port_mark_layer));
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(port_mark_name), false);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(port_name), false);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(air_valve_name), false);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(start_name), false);
                currentDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault("TH-DIM150"));
                currentDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault("TH-DIM100"));
                currentDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault("TH-DIM50"));
            }
        }
        private void Pre_proc_layer()
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                Get_cur_layer(db, geo_layer);
                Get_cur_layer(db, flg_layer);
                Get_cur_layer(db, port_layer);
                Get_cur_layer(db, air_valve_layer);
                Get_cur_layer(db, center_layer);
                Get_cur_layer(db, duct_size_layer);
                Get_cur_layer(db, dimension_layer);
                Get_cur_layer(db, port_mark_layer);
            }
        }
        private void Get_cur_layer(AcadDatabase db, string layer_name)
        {
            db.Database.UnFrozenLayer(layer_name);
            db.Database.UnLockLayer(layer_name);
            db.Database.UnOffLayer(layer_name);
        }
        public void Draw_duct(Line_Info info,
                              Matrix3d mat,
                              out ObjectIdList geo_ids,
                              out ObjectIdList flg_ids,
                              out ObjectIdList center_ids,
                              out ObjectIdList ports_ids,
                              out ObjectIdList ext_ports_ids)
        {
            Draw_duct(info, mat, out geo_ids, out flg_ids, out center_ids);
            Draw_ports(info.ports, info.ports_ext, mat, out ports_ids, out ext_ports_ids);
        }
        public void Draw_duct(Line_Info info,
                              Matrix3d mat,
                              out ObjectIdList geo_ids,
                              out ObjectIdList flg_ids,
                              out ObjectIdList center_ids)
        {
            Draw_lines(info.geo, mat, geo_layer, out geo_ids);
            Draw_lines(info.flg, mat, geo_layer, out flg_ids);
            Draw_lines(info.center_line, mat, center_layer, out center_ids);
        }
        public void Draw_dash_duct(Line_Info info,
                                  Matrix3d mat,
                                  out ObjectIdList geo_ids,
                                  out ObjectIdList flg_ids,
                                  out ObjectIdList center_ids,
                                  out ObjectIdList ports_ids,
                                  out ObjectIdList ext_ports_ids)
        {
            Draw_dash_lines(info.geo, mat, geo_layer, out geo_ids);
            Draw_dash_lines(info.flg, mat, geo_layer, out flg_ids);
            Draw_dash_lines(info.center_line, mat, center_layer, out center_ids);
            Draw_ports(info.ports, info.ports_ext, mat, out ports_ids, out ext_ports_ids);
        }
        public void Draw_shape(Line_Info info,
                               Matrix3d mat,
                               out ObjectIdList geo_ids,
                               out ObjectIdList flg_ids,
                               out ObjectIdList center_ids,
                               out ObjectIdList ports_ids,
                               out ObjectIdList ext_ports_ids)
        {
            Draw_lines(info.geo, mat, geo_layer, out geo_ids);
            Draw_lines(info.flg, mat, flg_layer, out flg_ids);
            Draw_lines(info.center_line, mat, center_layer, out center_ids);
            Draw_ports(info.ports, info.ports_ext, mat, out ports_ids, out ext_ports_ids);
        }
        public void Draw_dash_shape(Line_Info info,
                                   Matrix3d mat,
                                   out ObjectIdList geo_ids,
                                   out ObjectIdList flg_ids,
                                   out ObjectIdList center_ids,
                                   out ObjectIdList ports_ids,
                                   out ObjectIdList ext_ports_ids)
        {
            Draw_dash_lines(info.geo, mat, geo_layer, out geo_ids);
            Draw_dash_lines(info.flg, mat, flg_layer, out flg_ids);
            Draw_dash_lines(info.center_line, mat, center_layer, out center_ids);
            Draw_ports(info.ports, info.ports_ext, mat, out ports_ids, out ext_ports_ids);
        }
        public static void Draw_ports(List<Point3d> ports, 
                                      List<Point3d> ports_ext,
                                      Matrix3d mat,
                                      out ObjectIdList ports_ids,
                                      out ObjectIdList ext_ports_ids)
        {
            var ports_ids2 = new ObjectIdList();
            var ext_ports_ids2 = new ObjectIdList();
            if (ports != null && ports_ext != null)
            {
                ports.ForEach(p => ports_ids2.Add(Dreambuild.AutoCAD.Draw.Point(p.TransformBy(mat))));
                ports_ext.ForEach(p => ext_ports_ids2.Add(Dreambuild.AutoCAD.Draw.Point(p.TransformBy(mat))));
            }
            ports_ids = ports_ids2;
            ext_ports_ids = ext_ports_ids2;
        }
        public static void Draw_dash_lines(DBObjectCollection lines,
                                           Matrix3d trans_mat,
                                           string str_layer,
                                           out ObjectIdList ids)
        {
            using (var db = AcadDatabase.Active())
            {
                ids = new ObjectIdList();
                if (lines != null)
                {
                    foreach (Curve obj in lines)
                    {
                        var shadow = obj.Clone() as Curve;
                        ids.Add(db.ModelSpace.Add(shadow));
                        shadow.SetDatabaseDefaults();
                        shadow.Layer = str_layer;
                        shadow.ColorIndex = (int)ColorIndex.BYLAYER;
                        shadow.Linetype = ThHvacCommon.DASH_LINETYPE;
                        shadow.TransformBy(trans_mat);
                    }
                }
            }
        }
        public static void Draw_lines(DBObjectCollection lines,
                                      Matrix3d trans_mat,
                                      string str_layer,
                                      out ObjectIdList ids)
        {
            using (var db = AcadDatabase.Active())
            {
                ids = new ObjectIdList();
                if(lines != null)
                {
                    foreach (Curve obj in lines)
                    {
                        var shadow = obj.Clone() as Curve;
                        ids.Add(db.ModelSpace.Add(shadow));
                        shadow.SetDatabaseDefaults();
                        shadow.Layer = str_layer;
                        shadow.ColorIndex = (int)ColorIndex.BYLAYER;
                        shadow.Linetype = "ByLayer";
                        shadow.TransformBy(trans_mat);
                    }
                }
            }
        }
        public ObjectId Insert_start_flag(Point3d p, double angle)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                return acadDb.ModelSpace.ObjectId.InsertBlockReference(start_layer, start_name, p, new Scale3d(), angle);
            }
        }
        public static void Remove_ids(ObjectId[] objectIds)
        {
            foreach (var id in objectIds)
                id.Erase();
        }
        public static string Get_cur_layer(ObjectIdCollection colle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return colle.Cast<ObjectId>().Select(id => acadDatabase.Element<Entity>(id)).Select(e => e.Layer).FirstOrDefault();
            }
        }
        public static void Set_valve_dyn_block_properity(ObjectId obj, double width, double text_height, double text_angle, string valve_visibility)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY, valve_visibility);
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA, width);
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT, text_height);
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE, text_angle);
        }
        public static void Set_hole_dyn_block_properity(ObjectId obj, double width, double len)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            if (properity.Contains("长度"))
                properity.SetValue("长度", len);
            if (properity.Contains("宽度或直径"))
                properity.SetValue("宽度或直径", width);
        }
        public static void Set_muffler_dyn_block_properity(ObjectId obj, Muffler_modify_param muffler)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            if (properity.Contains("可见性"))
                properity.SetValue("可见性", muffler.muffler_visibility);
            if (properity.Contains("长度"))
                properity.SetValue("长度", muffler.len);
            if (properity.Contains("宽度"))
                properity.SetValue("宽度", muffler.width);
            if (properity.Contains("高度"))
                properity.SetValue("高度", muffler.height);
            if (properity.Contains("字高"))
                properity.SetValue("字高", muffler.text_height);
        }
        public static void Get_valve_dyn_block_properity(ObjectId obj,
                                                         out Point3d pos,
                                                         out double width, 
                                                         out double height,
                                                         out double text_angle,
                                                         out double rotate_angle,
                                                         out string valve_visibility)
        {
            var data = new ThBlockReferenceData(obj);
            
            var properity = data.CustomProperties;
            valve_visibility = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY) ?
                              (string)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY) : String.Empty;
            width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA) ?
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA) : 0;
            height = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT) ? 
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT) : 0;
            text_angle = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE) ? 
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE) : 0;
            rotate_angle = data.Rotation;
            pos = data.Position;
        }
        public static void Get_hole_dyn_block_properity(ObjectId obj,
                                                        out Point3d pos,
                                                        out double len,
                                                        out double width,
                                                        out double rotate_angle)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            len = properity.Contains("长度") ? (double)properity.GetValue("长度") : 0;
            width = properity.Contains("宽度或直径") ? (double)properity.GetValue("宽度或直径") : 0;
            rotate_angle = data.Rotation;
            pos = data.Position;
        }
        public static void Get_muffler_dyn_block_properity(ObjectId obj,
                                                           out Point3d pos,
                                                           out string visibility,
                                                           out double len,
                                                           out double height,
                                                           out double width,
                                                           out double text_height,
                                                           out double rotate_angle)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            visibility = properity.Contains("可见性") ? (string)properity.GetValue("可见性") : String.Empty;
            len = properity.Contains("长度") ? (double)properity.GetValue("长度") : 0;
            width = properity.Contains("宽度") ? (double)properity.GetValue("宽度") : 0;
            height = properity.Contains("高度") ? (double)properity.GetValue("高度") : 0;
            text_height = properity.Contains("字高") ? (double)properity.GetValue("字高") : 0;
            rotate_angle = data.Rotation;
            pos = data.Position;
        }
        public static void Get_port_dyn_block_properity(ObjectId obj,
                                                        out Point3d pos,
                                                        out string port_range,
                                                        out double port_height,
                                                        out double port_width,
                                                        out double rotate_angle)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            port_width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER) ?
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER) : 0;
            port_height = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT) ?
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT) : 0;
            port_range = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE) ?
                    (string)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE) : String.Empty;
            rotate_angle = data.Rotation;
            pos = data.Position;
        }
        public static void Set_port_dyn_block_properity(ObjectId obj, double port_width, double port_height, string port_range)
        {
            var data = new ThBlockReferenceData(obj);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER, port_width);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT, port_height);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE, port_range);
        }
        public static void Get_fan_dyn_block_properity( ThBlockReferenceData fan_data,
                                                        bool is_axis,
                                                        out double fan_in_width,
                                                        out double fan_out_width)
        {
            using (var db = AcadDatabase.Active())
            {
                var properity = fan_data.CustomProperties;
                if (is_axis)
                {
                    fan_in_width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) : 0;
                    fan_out_width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) ?
                            (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) : 0;
                }
                else
                {
                    fan_in_width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL) : 0;
                    fan_out_width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL) ?
                            (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL) : 0;
                }
            }
        }
        public static void Move_to_origin(Point3d align_p, DBObjectCollection line_set)
        {
            var dis_mat = Matrix3d.Displacement(-align_p.GetAsVector());
            foreach (Curve l in line_set)
                l.TransformBy(dis_mat);
        }
        public static void Remove_group_by_comp(ObjectId id)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = id.GetGroups();
                foreach (var g_id in ids)
                {
                    g_id.RemoveXData(ThHvacCommon.RegAppName_Duct_Info);
                    Remove_group(g_id);
                }
            }  
        }
        public static void Clear_graph(Handle handle)
        {
            if (handle.Value != 0)
            {
                using (var db = AcadDatabase.Active())
                {
                    var g_id = db.Database.GetObjectId(false, handle, 0);
                    //g_id.RemoveXData(ThHvacCommon.RegAppName_Info);
                    Remove_group(g_id);
                }
            }
        }
        private static void Remove_group(ObjectId g_id)
        {
            var component_ids = Dreambuild.AutoCAD.DbHelper.GetEntityIdsInGroup(g_id);
            foreach (ObjectId i in component_ids)
                i.Erase();
            g_id.Erase();
        }
        public void Draw_special_shape(List<Special_graph_Info> special_shapes_info, Matrix3d org_dis_mat)
        {
            foreach (var info in special_shapes_info)
            {
                switch (info.lines.Count)
                {
                    case 2: Draw_elbow(info, org_dis_mat); break;
                    case 3: Draw_tee(info, org_dis_mat); break;
                    case 4: Draw_cross(info, org_dis_mat); break;
                    default: throw new NotImplementedException();
                }
            }
        }
        private void Draw_cross(Special_graph_Info info, Matrix3d org_dis_mat)
        {
            var cross_info = Get_cross_info(info);
            var cross = ThDuctPortsFactory.Create_cross(cross_info.i_width, cross_info.o_width1, cross_info.o_width2, cross_info.o_width3);
            var mat = ThMEPHVACService.Get_trans_mat(cross_info.trans);
            Draw_shape(cross, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            Collect_special_shape_ids(cross, geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, "Cross", mat);
        }
        private Cross_Info Get_cross_info(Special_graph_Info info)
        {
            Seperate_cross_vec(info, out int outter_vec_idx, out int collinear_idx, out int inner_vec_idx,
                               out Vector3d in_vec, out Vector3d big_vec);
            double i_width = info.every_port_width[0];
            double inner_width = info.every_port_width[inner_vec_idx];
            double outter_width = info.every_port_width[outter_vec_idx];
            double collinear_width = info.every_port_width[collinear_idx];

            double rotate_angle = ThDuctPortsShapeService.Get_cross_trans_info(inner_width, outter_width, in_vec, big_vec, out bool is_flip);
            var trans = new Trans_info(is_flip, rotate_angle, info.lines[0].StartPoint.ToPoint2D());
            return new Cross_Info(i_width, outter_width, collinear_width, inner_width, trans);
        }
        private void Seperate_cross_vec(Special_graph_Info info,
                                        out int outter_vec_idx,
                                        out int collinear_idx,
                                        out int inner_vec_idx,
                                        out Vector3d in_vec,
                                        out Vector3d big_vec)
        {
            var i_line = info.lines[0];
            outter_vec_idx = collinear_idx = inner_vec_idx = 0;
            in_vec = ThMEPHVACService.Get_edge_direction(i_line);
            for (int i = 0; i < info.lines.Count; ++i)
            {
                var dir_vec = ThMEPHVACService.Get_edge_direction(info.lines[i]);
                if (ThMEPHVACService.Is_vertical(in_vec, dir_vec))
                {
                    if (ThMEPHVACService.Is_outter(in_vec, dir_vec))
                        outter_vec_idx = i;
                    else
                        inner_vec_idx = i;
                }
                else
                    collinear_idx = i;
            }
            double inner_width = info.every_port_width[inner_vec_idx];
            double outter_width = info.every_port_width[outter_vec_idx];
            int idx = (inner_width > outter_width) ? inner_vec_idx : outter_vec_idx;
            var l = info.lines[idx];
            big_vec = ThMEPHVACService.Get_edge_direction(l);
        }
        private void Draw_tee(Special_graph_Info info, Matrix3d org_dis_mat)
        {
            var tee_info = Get_tee_info(info, out Tee_Type type);
            var tee = ThDuctPortsFactory.Create_tee(tee_info.main_width, tee_info.branch, tee_info.other, type);
            var mat = ThMEPHVACService.Get_trans_mat(tee_info.trans);
            Draw_shape(tee, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids,
                          out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            Collect_special_shape_ids(tee, geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, "Tee", mat);
        }
        private Tee_Info Get_tee_info(Special_graph_Info info, out Tee_Type type)
        {
            Seperate_tee_vec(info.lines, out Vector3d in_vec, out Vector3d branch_vec, out Vector3d other_vec,
                                         out int branch_idx, out int other_idx);
            type = ThMEPHVACService.Is_collinear(branch_vec, other_vec) ? Tee_Type.BRANCH_COLLINEAR_WITH_OTTER : Tee_Type.BRANCH_VERTICAL_WITH_OTTER;
            double rotate_angle = ThDuctPortsShapeService.Get_tee_trans_info(in_vec, branch_vec, out bool is_flip);
            double main_width = info.every_port_width[0];
            double branch = info.every_port_width[branch_idx];
            double other = info.every_port_width[other_idx];
            var trans = new Trans_info(is_flip, rotate_angle, info.lines[0].StartPoint.ToPoint2D());
            return new Tee_Info(main_width, branch, other, trans);
        }
        public void Seperate_tee_vec(List<Line> lines,
                                     out Vector3d in_vec, out Vector3d branch_vec, out Vector3d other_vec,
                                     out int branch_idx, out int other_idx)
        {
            var i_line = lines[0];
            var o1_line = lines[1];
            var o2_line = lines[2];
            var o1_vec = ThMEPHVACService.Get_edge_direction(o1_line);
            var o2_vec = ThMEPHVACService.Get_edge_direction(o2_line);
            in_vec = ThMEPHVACService.Get_edge_direction(i_line);
            Do_seperate_tee(in_vec, o1_vec, o2_vec, out branch_vec, out other_vec, out branch_idx, out other_idx);
        }
        private void Do_seperate_tee(Vector3d in_vec, Vector3d o1_vec, Vector3d o2_vec,
                                     out Vector3d branch_vec, out Vector3d other_vec,
                                     out int branch_idx, out int other_idx)
        {
            if (ThMEPHVACService.Is_vertical(o1_vec, o2_vec))
            {
                if (ThMEPHVACService.Is_vertical(in_vec, o1_vec))
                    Set_tee_vec(o1_vec, o2_vec, 1, 2, out branch_vec, out other_vec, out branch_idx, out other_idx);
                else
                    Set_tee_vec(o2_vec, o1_vec, 2, 1, out branch_vec, out other_vec, out branch_idx, out other_idx);
            }
            else
            {
                if (ThMEPHVACService.Is_outter(in_vec, o1_vec))
                    Set_tee_vec(o1_vec, o2_vec, 1, 2, out branch_vec, out other_vec, out branch_idx, out other_idx);
                else
                    Set_tee_vec(o2_vec, o1_vec, 2, 1, out branch_vec, out other_vec, out branch_idx, out other_idx);
            }
        }
        private void Set_tee_vec(Vector3d vec1, Vector3d vec2, int idx1, int idx2,
                                 out Vector3d branch_vec, out Vector3d other_vec, out int branch_idx, out int other_idx)
        {
            branch_vec = vec1;
            other_vec = vec2;
            branch_idx = idx1;
            other_idx = idx2;
        }
        private void Draw_elbow(Special_graph_Info info, Matrix3d org_dis_mat)
        {
            var elbow_info = Get_elbow_info(info);
            var elbow = ThDuctPortsFactory.Create_elbow(elbow_info.open_angle, elbow_info.duct_width);
            var mat = ThMEPHVACService.Get_trans_mat(elbow_info.trans);
            Draw_shape(elbow, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids,
                                                 out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            Collect_special_shape_ids(elbow, geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, "Elbow", mat);
        }
        private Elbow_Info Get_elbow_info(Special_graph_Info info)
        {
            var in_line = info.lines[0];
            var out_line = info.lines[1];
            double in_width = info.every_port_width[0];
            double out_width = info.every_port_width[1];
            return Record_elbow_info(in_line, out_line, in_width, out_width);
        }
        private Elbow_Info Record_elbow_info(Line in_line, Line out_line, double in_width, double out_width)
        {
            double rotate_angle;
            var in_vec = ThMEPHVACService.Get_edge_direction(in_line);
            var out_vec = ThMEPHVACService.Get_edge_direction(out_line);
            double open_angle = Math.PI - in_vec.GetAngleTo(out_vec);
            double width = in_width < out_width ? in_width : out_width;
            rotate_angle = ThDuctPortsShapeService.Get_elbow_trans_info(open_angle, in_vec, out_vec, out bool is_flip);
            var trans = new Trans_info(is_flip, rotate_angle, in_line.StartPoint.ToPoint2D());
            return new Elbow_Info(open_angle, width, trans);
        }
        private void Collect_special_shape_ids(Line_Info info,
                                               ObjectIdList geo_ids,
                                               ObjectIdList flg_ids,
                                               ObjectIdList center_ids,
                                               ObjectIdList ports_ids,
                                               ObjectIdList ext_ports_ids,
                                               string type,
                                               Matrix3d mat)
        {
            var param = ThMEPHVACService.Create_special_modify_param(type, mat, ObjectId.Null.Handle, info.flg, info.center_line);
            ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
        }
    }
}