﻿using System;
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
    public class Elbow_Info
    {
        public bool is_flip;
        public double open_angle;
        public double duct_width;
        public double rotate_angle;
        public Point3d center_point;
        public Elbow_Info(bool is_flip_, double open_angle_, double duct_width_, double rotate_angle_, Point3d center_point_)
        {
            is_flip = is_flip_;
            open_angle = open_angle_;
            duct_width = duct_width_;
            rotate_angle = rotate_angle_;
            center_point = center_point_;
        }
    }
    public class Tee_Info
    {
        public bool is_flip;
        public double i_width;
        public double o_width1;
        public double o_width2;
        public double rotate_angle;
        public Point3d center_point;
        public Tee_Info(bool is_flip_, double i_width_, double o_width1_, double o_width2_, double rotate_angle_, Point3d center_point_)
        {
            is_flip = is_flip_;
            i_width = i_width_;
            o_width1 = o_width1_;
            o_width2 = o_width2_;
            rotate_angle = rotate_angle_;
            center_point = center_point_;
        }
    }
    public class Cross_Info
    {
        public bool is_flip;
        public double i_width;
        public double o_width1;
        public double o_width2;
        public double o_width3;
        public double rotate_angle;
        public Point3d center_point;
        public Cross_Info(bool is_flip_, double i_width_, double o_width1_, double o_width2_, double o_width3_, double rotate_angle_, Point3d center_point_)
        {
            is_flip = is_flip_;
            i_width = i_width_;
            o_width1 = o_width1_;
            o_width2 = o_width2_;
            o_width3 = o_width3_;
            rotate_angle = rotate_angle_;
            center_point = center_point_;
        }
    }
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
        public ThDuctPortsDrawDim dim_service;
        public ThDuctPortsDrawService(string scenario, string scale)
        {
            valve_name = "风阀";
            start_name = "AI-风管起点";
            port_mark_name = "风口标注";
            block_name = "风口-AI研究中心";
            Set_layer(scenario);
            Import_Layer_Block();
            Pre_proc_layer();
            dim_service = new ThDuctPortsDrawDim(dimension_layer, scale);
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
                               out ObjectIdList center_ids)
        {
            Draw_lines(info.geo, mat, geo_layer, out geo_ids);
            Draw_lines(info.flg, mat, flg_layer, out flg_ids);
            Draw_lines(info.center_line, mat, center_layer, out center_ids);
        }
        public static void Draw_lines(DBObjectCollection lines,
                                      Matrix3d trans_mat,
                                      string str_layer,
                                      out ObjectIdList ids)
        {
            using (AcadDatabase db = AcadDatabase.Active())
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
        public void Attach_start_param(ObjectId id, DuctPortsParam param, List<Entity_param> entity_ids)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var value_list = new TypedValueList
                {
                    { (int)DxfCode.ExtendedDataAsciiString, id.ToString()},
                    { (int)DxfCode.ExtendedDataAsciiString, param.scale},
                    { (int)DxfCode.ExtendedDataAsciiString, param.scenario},
                };
                foreach (var info in entity_ids)
                    value_list.Add((int)DxfCode.ExtendedDataAsciiString, info.ToString());
                id.AddXData("Start", value_list);
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
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY, valve_visibility);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA, width);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT, height);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE, text_angle);
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
    }
}