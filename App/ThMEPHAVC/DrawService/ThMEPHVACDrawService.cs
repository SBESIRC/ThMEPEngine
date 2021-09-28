using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.DrawService
{
    public class ThMEPHVACDrawService
    {
        private string scale;
        private ObjectId start_id;
        private Point3d srt_flag_pos;
        private ThDuctPortsDrawService service;
        public ThMEPHVACDrawService(string scenario,
                                    string scale,
                                    Point3d srtFlagPosition,
                                    Vector3d srtDuctDirection)
        {
            this.scale = scale;
            this.srt_flag_pos = srtFlagPosition;
            service = new ThDuctPortsDrawService(scenario, scale);
            var angle = srtDuctDirection.GetAngleTo(-Vector3d.YAxis) - Math.PI / 3;
            start_id = service.Insert_start_flag(srt_flag_pos, angle);
        }
        public void Draw_duct(Duct_modify_param param, Matrix3d mat)
        {
            double w = ThMEPHVACService.Get_width(param.duct_size);

            var duct = ThDuctPortsFactory.Create_duct(param.sp, param.ep, w);
            service.Draw_duct(duct, mat, out ObjectIdList gids, out ObjectIdList fids, out ObjectIdList cids,
                                         out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            var duct_param = ThMEPHVACService.Create_duct_modify_param(
                duct.center_line, param.duct_size, param.elevation.ToString(), param.air_volume, start_id.Handle);
            ThDuctPortsRecoder.Create_duct_group(gids, fids, cids, ports_ids, ext_ports_ids, duct_param);
            service.text_service.Draw_duct_text(duct_param, scale);
        }
        public void Draw_reducing(Line centerLine, double bigWidth, double smallWidth, bool isAxis, Matrix3d mat)
        {
            var reducing = ThDuctPortsReDrawFactory.Create_reducing(centerLine, bigWidth, smallWidth, isAxis);
            var param = ThMEPHVACService.Create_reducing_modify_param(reducing, start_id.Handle);
            service.Draw_shape(reducing, mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                              out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
        }
        public void Draw_special_shape(List<Special_graph_Info> special_shapes_info)
        {
            var org_dis_mat = Matrix3d.Displacement(srt_flag_pos.GetAsVector());
            service.Draw_special_shape(special_shapes_info, org_dis_mat);
        }
    }
}