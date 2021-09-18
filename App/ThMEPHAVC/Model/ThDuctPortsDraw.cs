using System;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDraw
    {
        private bool have_main;
        private double main_height;
        private double port_width;
        private double port_height;
        private readonly List<Point2d> duct_dir_align_points;
        private readonly List<Point2d> duct_ver_align_points;
        private ThDuctPortsDrawService service;
        private ThMEPHVACParam in_param;
        private ObjectId start_id;
        private Point3d start_point;
        private Vector3d org_dis_vec;
        private Matrix3d org_dis_mat;
        public ThDuctPortsDraw(Point3d start_point_,
                               ThMEPHVACParam in_param_,
                               List<Point2d> duct_dir_align_points_,
                               List<Point2d> duct_ver_align_points_)
        {
            Param_init(in_param_, start_point_);
            duct_dir_align_points = duct_dir_align_points_;
            duct_ver_align_points = duct_ver_align_points_;
        }
        private void Param_init(ThMEPHVACParam in_param_, Point3d start_point_)
        {
            in_param = in_param_;
            ThMEPHVACService.Seperate_size_info(in_param.port_size, out double width, out double height);
            port_width = width;
            port_height = height;
            main_height = ThMEPHVACService.Get_height(in_param.in_duct_size);
            in_param.main_height = main_height;
            service = new ThDuctPortsDrawService(in_param.scenario, in_param.scale);
            start_point = start_point_;
            org_dis_vec = start_point.GetAsVector();
            org_dis_mat = Matrix3d.Displacement(org_dis_vec);
        }
        public void Draw(ThDuctPortsAnalysis anay_res, ThDuctPortsConstructor endlines)
        {
            have_main = anay_res.main_ducts.Count != 0;
            var angle = anay_res.start_dir_vec.GetAngleTo(-Vector3d.YAxis) - Math.PI / 3;
            start_id = service.Insert_start_flag(start_point, angle);
            Draw_endlines(endlines);
            Draw_mainlines(anay_res);
            service.Draw_special_shape(anay_res.special_shapes_info, org_dis_mat);
            Draw_port_mark(endlines);
            service.air_valve_service.Insert_valve(anay_res.merged_endlines.Count, start_point, endlines);
            ThDuctPortsRecoder.Attach_start_param(start_id, in_param);
        }
        private void Draw_port_mark(ThDuctPortsConstructor endlines)
        {
            if (endlines.endline_segs.Count == 0)
                return;
            var last_seg = endlines.endline_segs[endlines.endline_segs.Count - 1];
            if (last_seg.segs.Count == 0)
                return;
            var ports = last_seg.segs[last_seg.segs.Count - 1].ports_info;
            if (ports.Count == 0)
                return;
            Point3d p = Get_mark_base_point(ports) + new Vector3d(1500, 2000, 0) + org_dis_vec;
            ThDuctPortsDrawPortMark.Insert_mark(in_param, port_width, port_height, service.port_mark_name, service.port_mark_layer, p);
            ThDuctPortsDrawPortMark.Insert_leader(Get_mark_base_point(ports) + org_dis_vec, p, service.port_mark_layer);
        }
        private Point3d Get_mark_base_point(List<Port_Info> ports)
        {
            if (ports.Count > 0)
            {
                if (ports[ports.Count - 1].air_volume > 1)
                    return ports[ports.Count - 1].position;
                else
                    return ports[ports.Count - 2].position;
            }
            return Point3d.Origin;
        }
        private void Draw_endlines(ThDuctPortsConstructor endlines)
        {
            service.Draw_special_shape(endlines.endline_elbow, org_dis_mat);
            for (int i = 0; i < endlines.endline_segs.Count; ++i)
            {
                string pre_duct_text_info = String.Empty;
                var infos = endlines.endline_segs[i];
                var ver_wall_point = (duct_dir_align_points.Count > i) ? duct_ver_align_points[i] : Point2d.Origin;
                var dir_wall_point = (duct_dir_align_points.Count > i) ? duct_dir_align_points[i] : Point2d.Origin;
                Draw_port_duct(infos.segs, ref pre_duct_text_info);
                service.dim_service.Draw_dimension(infos.segs, dir_wall_point, ver_wall_point, start_point);
            }
        }
        private void Draw_port_duct(List<Duct_ports_Info> infos, ref string duct_text_info)
        {
            double pre_air_volume = 0;
            string pre_duct_size = string.Empty;
            var pre_seg = new Line_Info();
            var geo_set = new DBObjectCollection();
            for (int i = 0; i < infos.Count; ++i)
            {
                var info = infos[i];
                var cur_seg = service.text_service.Get_endline_duct_info(have_main, main_height, in_param, 
                    info, org_dis_mat, ref duct_text_info, out List<DBText> duct_size_info);
                service.port_service.Draw_ports(info, in_param, org_dis_vec, port_width, port_height);
                service.text_service.Draw_duct_size_info(duct_size_info);
                Collect_duct_geo(geo_set, cur_seg);
                Record_pre_seg_info(cur_seg, duct_text_info, info, ref pre_seg, ref pre_duct_size, ref pre_air_volume);
                Record_duct(cur_seg, pre_seg, pre_duct_size, pre_air_volume);
                if (i == 0)
                    continue;//第一段duct不画reducing，之后的都是reducing + duct
                Record_reducing(geo_set);
                Remove_pre_duct_geo(geo_set);
            }
        }
        private void Record_duct(Line_Info cur_seg, Line_Info pre_seg, string pre_duct_size, double pre_air_volume)
        {
            var duct_param = ThMEPHVACService.Create_duct_modify_param(cur_seg.center_line, pre_duct_size, pre_air_volume, in_param, start_id.Handle);
            if (duct_param.sp.GetDistanceTo(duct_param.ep) > 1)
            {
                service.Draw_duct(pre_seg, org_dis_mat, out ObjectIdList seg_geo_ids, out ObjectIdList seg_flg_ids, 
                                  out ObjectIdList seg_center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                ThDuctPortsRecoder.Create_duct_group(seg_geo_ids, seg_flg_ids, seg_center_ids, ports_ids, ext_ports_ids, duct_param);
            }
        }
        private void Record_reducing(DBObjectCollection geo_set)
        {
            var reducing = ThDuctPortsFactory.Create_reducing(geo_set);
            service.Draw_shape(reducing, org_dis_mat, out ObjectIdList red_geo_ids, out ObjectIdList red_flg_ids, 
                out ObjectIdList red_center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            var reducing_param = ThMEPHVACService.Create_reducing_modify_param(reducing, start_id.Handle);
            ThDuctPortsRecoder.Create_group(red_geo_ids, red_flg_ids, red_center_ids, ports_ids, ext_ports_ids, reducing_param);
        }
        private void Record_pre_seg_info(Line_Info cur_seg,
                                         string duct_text_info,
                                         Duct_ports_Info info,
                                         ref Line_Info pre_seg,
                                         ref string pre_duct_text_info,
                                         ref double pre_air_volume)
        {
            pre_seg = cur_seg;
            pre_duct_text_info = duct_text_info;
            if (info.ports_info.Count > 0)
                pre_air_volume = info.ports_info[0].air_volume;
        }
        private void Remove_pre_duct_geo(DBObjectCollection geo_set)
        {
            geo_set.RemoveAt(0);
            geo_set.RemoveAt(0);
        }
        private void Collect_duct_geo(DBObjectCollection geo_set, Line_Info seg_outlines)
        {
            foreach (var g in seg_outlines.geo)
                geo_set.Add(g as Line);
        }
        private void Draw_mainlines(ThDuctPortsAnalysis anay_res)
        {
            string pre_duct_size_text = String.Empty;
            foreach (var info in anay_res.main_ducts)
            {
                var l = ThMEPHVACService.Get_shrink_line(info);
                var mainlines = Get_main_duct(info, out double duct_width);
                ThMEPHVACService.Get_line_pos_info(l, out double angle, out Point3d center_point);
                var mat = Matrix3d.Displacement(center_point.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat = org_dis_mat * mat;
                ThDuctPortsDrawService.Draw_lines(mainlines.geo, mat, service.geo_layer, out ObjectIdList geo_ids);
                ThDuctPortsDrawService.Draw_lines(mainlines.flg, mat, service.geo_layer, out ObjectIdList flg_ids);
                ThDuctPortsDrawService.Draw_lines(mainlines.center_line, org_dis_mat, service.center_layer, out ObjectIdList center_ids);
                // port根据中心线变化
                ThDuctPortsDrawService.Draw_ports(mainlines.ports, mainlines.ports_ext, org_dis_mat,
                                                  out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var param = ThMEPHVACService.Create_duct_modify_param(mainlines.center_line, in_param.in_duct_size, info.AirVolume, in_param, start_id.Handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
                var dir_vec = (info.Target.Position - info.Source.Position).GetNormal();
                service.text_service.Draw_mainline_text_info(angle, main_height,center_point, dir_vec, org_dis_mat, in_param, ref pre_duct_size_text);
            }
        }
        private Line_Info Get_main_duct(ThDuctEdge<ThDuctVertex> info, out double duct_width)
        {
            var l = ThMEPHVACService.Get_shrink_line(info);
            duct_width = ThMEPHVACService.Get_width(in_param.in_duct_size);
            var outlines = ThDuctPortsFactory.Create_duct(l.Length, duct_width);
            var center_line = new DBObjectCollection { l };
            var outline1 = outlines[0] as Line;
            var outline2 = outlines[1] as Line;
            var flg = new DBObjectCollection{new Line(outline1.StartPoint, outline2.StartPoint),
                                             new Line(outline1.EndPoint, outline2.EndPoint)};
            ThMEPHVACService.Get_duct_ports(l, out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(outlines, flg, center_line, ports, ports_ext);
        }
    }
}