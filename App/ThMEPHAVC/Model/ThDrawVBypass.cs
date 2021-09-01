using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;

namespace ThMEPHVAC.Model
{
    public class ThDrawVBypass
    {
        private double air_volume;
        private string bypass_size;
        private string elevation;
        private Matrix3d dis_mat;
        private ObjectId start_id;
        private ThDuctPortsDrawService service;

        public ThDrawVBypass(double air_volume, 
                             string scale, 
                             string scenario, 
                             Point3d move_srt_p, 
                             ObjectId start_id,
                             string bypass_size,
                             string elevation)
        {
            this.start_id = start_id;
            this.air_volume = air_volume;
            this.bypass_size = bypass_size;
            this.elevation = elevation;
            dis_mat = Matrix3d.Displacement(move_srt_p.GetAsVector());
            service = new ThDuctPortsDrawService(scenario, scale);
        }
        public void Draw_4vertical_bypass(List<Line_Info> vt_elbow, Point3d in_vt_pos, Point3d out_vt_pos)
        {
            foreach (var vt in vt_elbow)
            {
                service.Draw_shape(vt, dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                ThDuctPortsRecoder.Create_vt_elbow_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids);
            }
            Draw_vt_duct(in_vt_pos, out_vt_pos, false);
        }
        public void Draw_5vertical_bypass(List<Line_Info> vt_elbow, Point3d in_vt_pos, Point3d out_vt_pos)
        {
            foreach (var vt in vt_elbow)
            {
                service.Draw_dash_shape(vt, dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids,
                                    out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                ThDuctPortsRecoder.Create_vt_elbow_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids);
            }
            Draw_vt_duct(in_vt_pos, out_vt_pos, true);
        }
        private void Draw_vt_duct(Point3d in_vt_pos, Point3d out_vt_pos, bool is_dash)
        {
            var dir_vec = (out_vt_pos - in_vt_pos).GetNormal();
            in_vt_pos = in_vt_pos.TransformBy(dis_mat);
            out_vt_pos = out_vt_pos.TransformBy(dis_mat);
            ThMEPHVACService.Seperate_size_info(bypass_size, out double width, out double height);
            var sp = in_vt_pos + (dir_vec * height * 0.5);
            var ep = out_vt_pos - (dir_vec * height * 0.5);
            var duct = ThDuctPortsFactory.Create_duct(sp.ToPoint2D(), ep.ToPoint2D(), width);
            if (!is_dash)
            {
                service.Draw_duct(duct, Matrix3d.Identity, out ObjectIdList gids, out ObjectIdList fids, out ObjectIdList cids,
                                                           out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(duct.center_line, bypass_size, elevation, air_volume, start_id.Handle);
                duct_param.type = "Vertical_bypass";
                ThDuctPortsRecoder.Create_duct_group(gids, fids, cids, ports_ids, ext_ports_ids, duct_param);
            }
            else
            {
                service.Draw_dash_duct(duct, Matrix3d.Identity, out ObjectIdList gids, out ObjectIdList fids, out ObjectIdList cids,
                                                                out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(duct.center_line, bypass_size, elevation, air_volume, start_id.Handle);
                duct_param.type = "Vertical_bypass";
                ThDuctPortsRecoder.Create_duct_group(gids, fids, cids, ports_ids, ext_ports_ids, duct_param);
            }
        }
    }
}
