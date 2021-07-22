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
        public string valve_layer;
        public string start_layer;
        public string center_layer;
        public string duct_size_layer;
        public string dimension_layer;
        public string port_mark_layer;
        public string valve_name;
        public string block_name;
        public string start_name;
        public string port_mark_name;
        private string valve_visibility;
        public ThDuctPortsDrawDim dim_service;
        public ThDuctPortsDrawValve valve_service;
        public ThDuctPortsDrawText text_service;
        public ThDuctPortsDrawService(string scenario, string scale)
        {
            valve_name = "风阀";
            start_name = "AI-风管起点";
            port_mark_name = "风口标注";
            block_name = "风口-AI研究中心";
            valve_visibility = "多叶调节风阀";
            Set_layer(scenario);
            Import_Layer_Block();
            Pre_proc_layer();
            dim_service = new ThDuctPortsDrawDim(dimension_layer, scale);
            valve_service = new ThDuctPortsDrawValve(valve_visibility, valve_name, valve_layer);
            text_service = new ThDuctPortsDrawText(duct_size_layer);
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
                    valve_layer = "H-DAPP-DDAMP";
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
                    valve_layer = "H-DAPP-FDAMP";
                    duct_size_layer = "H-DIMS-FIRE";
                    dimension_layer = "H-DIMS-FIRE";
                    port_mark_layer = "H-DIMS-FIRE";
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
                    valve_layer = "H-DAPP-DAMP";
                    duct_size_layer = "H-DIMS-DUCT";
                    dimension_layer = "H-DIMS-DUCT";
                    port_mark_layer = "H-DIMS-DUCT";
                    break;
            }
            start_layer = "AI-风管起点";
        }
        private void Import_Layer_Block()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(geo_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(flg_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(port_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(valve_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(start_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(center_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(duct_size_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(dimension_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(port_mark_layer));
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(port_mark_name), false);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(block_name), false);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(valve_name), false);
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
                Get_cur_layer(db, valve_layer);
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
        public static void Draw_ports(List<Point3d> ports, 
                                      List<Point3d> ports_ext,
                                      Matrix3d mat,
                                      out ObjectIdList ports_ids,
                                      out ObjectIdList ext_ports_ids)
        {
            var ports_ids2 = new ObjectIdList();
            var ext_ports_ids2 = new ObjectIdList();
            ports.ForEach(p => ports_ids2.Add(Dreambuild.AutoCAD.Draw.Point(p.TransformBy(mat))));
            ports_ext.ForEach(p => ext_ports_ids2.Add(Dreambuild.AutoCAD.Draw.Point(p.TransformBy(mat))));

            ports_ids = ports_ids2;
            ext_ports_ids = ext_ports_ids2;
        }
        public static void Draw_lines(DBObjectCollection lines,
                                      Matrix3d trans_mat,
                                      string str_layer,
                                      out ObjectIdList ids)
        {
            using (var db = AcadDatabase.Active())
            {
                ids = new ObjectIdList();
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
        public ObjectId Insert_start_flag(Point3d p)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                return acadDb.ModelSpace.ObjectId.InsertBlockReference(start_layer, start_name, p, new Scale3d(), 0);
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
        public static void Set_valve_dyn_block_properity(ObjectId obj, double width, double height, double text_angle, string valve_visibility)
        {
            var data = new ThBlockReferenceData(obj);
            var properity = data.CustomProperties;
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY, valve_visibility);
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA, width);
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT, height);
            if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE))
                properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE, text_angle);
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
                              (string)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY) :
                              String.Empty;
            width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA) ?
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA) : 0;
            height = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT) ? 
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT) : 0;
            text_angle = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE) ? 
                    (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE) : 0;
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
                    g_id.RemoveXData("Info");
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
                    g_id.RemoveXData("Info");
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
    }
}