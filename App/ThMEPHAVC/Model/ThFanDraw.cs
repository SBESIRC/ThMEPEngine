using System;
using System.Collections.Generic;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThFanDraw
    {
        public ObjectId start_id;
        public ThDbModelFan fan;
        public Duct_InParam param;
        public Tolerance point_tor;
        public DBObjectCollection bypass;
        private Matrix3d dis_mat;
        private ThDuctPortsDrawService service;
        public ThFanDraw(ThFanAnalysis anay_res)
        {
            Init(anay_res);
            service.Draw_special_shape(anay_res.special_shapes_info, dis_mat);
            Draw_duct(anay_res.center_lines);
            Draw_reducing(anay_res.reducings);
            Draw_duct_text(anay_res.text_alignment, anay_res.move_srt_p);
        }
        private void Init(ThFanAnalysis anay_res)
        {
            dis_mat = Matrix3d.Displacement(anay_res.move_srt_p.GetAsVector());
            fan = anay_res.fan;
            param = anay_res.param;
            bypass = anay_res.bypass;
            point_tor = anay_res.point_tor;
            service = new ThDuctPortsDrawService(fan.scenario, param.scale);
            start_id = service.Insert_start_flag(fan.FanInletBasePoint);
            var par = new ThMEPHVACParam() { scenario = fan.scenario, scale = param.scale };
            ThDuctPortsRecoder.Attach_start_param(start_id, par);
        }
        private void Draw_duct_text(List<TextAlignLine> text_alignment, Point3d srt_p)
        {
            var elevation = Double.Parse(param.elevation);
            var mat = Matrix3d.Displacement(srt_p.GetAsVector());
            for (int i = 0; i < 2; ++i)
            {
                var t = text_alignment[i];
                t.l.TransformBy(mat);
            }
            service.text_service.Get_fjf_duct_info(param.scale, elevation, text_alignment, out List<DBText> duct_size_info);
            service.text_service.Draw_duct_size_info(duct_size_info);
        }
        public void Draw_duct(List<Fan_duct_Info> center_lines)
        {
            foreach (var l in center_lines)
            {
                var dir_vec = (l.ep - l.sp).GetNormal();
                var sp = (l.sp + dir_vec * l.src_shrink).ToPoint2D();
                var ep = (l.ep - dir_vec * l.dst_shrink).ToPoint2D();
                var duct = ThDuctPortsFactory.Create_duct(sp, ep, ThMEPHVACService.Get_width(l.size));
                service.Draw_duct(duct, dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                           out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(duct.center_line, l.size, param.elevation, 
                                                                           fan.air_volume, start_id.Handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
            }
        }
        private void Draw_reducing(List<Line_Info> reducings)
        {
            foreach (var red in reducings)
            {
                var param = ThMEPHVACService.Create_reducing_modify_param(red, start_id.Handle);
                service.Draw_shape(red, dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                              out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
            }
        }
        
        public ObjectId Insert_electric_valve(Vector3d fan_cp_vec, double valvewidth, double angle)
        {
            var e = new ThValve()
            {
                Length = 200,
                Width = valvewidth,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = "H-DAPP-EDAMP",
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = e.ValveBlockName;
                var layerName = e.ValveBlockLayer;
                Active.Database.ImportLayer(layerName);
                Active.Database.ImportValve(blockName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(e.Width, e.WidthPropertyName);
                objId.SetValveModel(e.ValveVisibility);

                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Matrix3d mat = Matrix3d.Displacement(fan_cp_vec) *
                               Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat *= Matrix3d.Displacement(new Vector3d(-valvewidth / 2, 125, 0));

                blockRef.TransformBy(mat);
                return objId;
            }
        }
    }
}
