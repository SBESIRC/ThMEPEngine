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
        private DuctPortsParam in_param;
        private ObjectId start_id;
        private bool is_first;
        private Point3d start_point;
        private Vector3d org_dis_vec;
        private Matrix3d org_dis_mat;
        public ThDuctPortsDraw(Point3d start_point_,
                               DuctPortsParam in_param_,
                               List<Point2d> duct_dir_align_points_,
                               List<Point2d> duct_ver_align_points_)
        {
            Param_init(in_param_, start_point_);
            duct_dir_align_points = duct_dir_align_points_;
            duct_ver_align_points = duct_ver_align_points_;
        }
        private void Param_init(DuctPortsParam in_param_, Point3d start_point_)
        {
            is_first = true;
            in_param = in_param_;
            ThDuctPortsService.Seperate_size_info(in_param.port_size, out double width, out double height);
            port_width = width;
            port_height = height;
            main_height = ThDuctPortsService.Get_height(in_param.in_duct_size);
            in_param.main_height = main_height;
            service = new ThDuctPortsDrawService(in_param.scenario, in_param.scale);
            start_point = start_point_;
            org_dis_vec = start_point.GetAsVector();
            org_dis_mat = Matrix3d.Displacement(org_dis_vec);
        }
        public void Draw(ThDuctPortsAnalysis anay_res, ThDuctPortsConstructor endlines)
        {
            have_main = anay_res.main_ducts.Count != 0;
            start_id = service.Insert_start_flag(start_point);
            Draw_endlines(endlines);
            Draw_mainlines(anay_res);
            Draw_special_shape(anay_res.special_shapes_info);
            Draw_port_mark(endlines);
            service.valve_service.Insert_valve(anay_res.merged_endlines.Count, start_point, endlines);
            ThDuctPortsRecoder.Attach_start_param(start_id, start_point.ToPoint2D(), in_param);
        }
        private void Draw_port_mark(ThDuctPortsConstructor endlines)
        {
            if (endlines.endline_segs.Count == 0)
                return;
            var last_seg = endlines.endline_segs[endlines.endline_segs.Count - 1];
            if (last_seg.segs.Count == 0)
                return;
            var ports = last_seg.segs[last_seg.segs.Count - 1].ports_info;
            if (ports.Count < 2)
                return;
            Point3d p = Get_mark_base_point(ports) + new Vector3d(1500, 2000, 0) + org_dis_vec;
            ThDuctPortsDrawPortMark.Insert_mark(in_param.port_num,
                                                in_param.air_volumn,
                                                port_width,
                                                port_height,
                                                in_param.scale,
                                                in_param.port_name,
                                                service.port_mark_name, service.port_mark_layer, p);
            ThDuctPortsDrawPortMark.Insert_leader(Get_mark_base_point(ports) + org_dis_vec, p, service.port_mark_layer);
        }
        private Point3d Get_mark_base_point(List<Port_Info> ports)
        {
            if (ports[ports.Count - 1].air_volume > 1)
                return ports[ports.Count - 1].position;
            else
                return ports[ports.Count - 2].position;
        }
        private void Draw_special_shape(List<Special_graph_Info> special_shapes_info)
        {
            foreach (var info in special_shapes_info)
            {
                switch (info.lines.Count)
                {
                    case 2: Draw_elbow(info); break;
                    case 3: Draw_tee(info); break;
                    case 4: Draw_cross(info); break;
                    default: throw new NotImplementedException();
                }
            }
        }
        private void Draw_cross(Special_graph_Info info)
        {
            var cross_info = Get_cross_info(info);
            var cross = ThDuctPortsFactory.Create_cross(cross_info.i_width, cross_info.o_width1, cross_info.o_width2, cross_info.o_width3);
            var mat = ThDuctPortsService.Get_trans_mat(cross_info.trans);
            service.Draw_shape(cross, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
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
            in_vec = ThDuctPortsService.Get_edge_direction(i_line);
            for (int i = 0; i < info.lines.Count; ++i)
            {
                var dir_vec = ThDuctPortsService.Get_edge_direction(info.lines[i]);
                if (ThDuctPortsService.Is_vertical(in_vec, dir_vec))
                {
                    if (ThDuctPortsService.Is_outter(in_vec, dir_vec))
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
            big_vec = ThDuctPortsService.Get_edge_direction(l);
        }
        private void Draw_tee(Special_graph_Info info)
        {
            var tee_info = Get_tee_info(info, out Tee_Type type);
            var tee = ThDuctPortsFactory.Create_tee(tee_info.main_width, tee_info.branch, tee_info.other, type);
            var mat = ThDuctPortsService.Get_trans_mat(tee_info.trans);
            service.Draw_shape(tee, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, 
                          out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            Collect_special_shape_ids(tee, geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, "Tee", mat);
        }
        private Tee_Info Get_tee_info(Special_graph_Info info, out Tee_Type type)
        {
            Seperate_tee_vec(info.lines, out Vector3d in_vec, out Vector3d branch_vec, out Vector3d other_vec, 
                                         out int branch_idx, out int other_idx);
            type = ThDuctPortsService.Is_collinear(branch_vec, other_vec) ? Tee_Type.BRANCH_COLLINEAR_WITH_OTTER : Tee_Type.BRANCH_VERTICAL_WITH_OTTER;
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
            var o1_vec = ThDuctPortsService.Get_edge_direction(o1_line);
            var o2_vec = ThDuctPortsService.Get_edge_direction(o2_line);
            in_vec = ThDuctPortsService.Get_edge_direction(i_line);
            Do_seperate_tee(in_vec, o1_vec, o2_vec, out branch_vec, out other_vec, out branch_idx, out other_idx);
        }
        private void Do_seperate_tee(Vector3d in_vec, Vector3d o1_vec, Vector3d o2_vec,
                                     out Vector3d branch_vec, out Vector3d other_vec,
                                     out int branch_idx, out int other_idx)
        {
            if (ThDuctPortsService.Is_vertical(o1_vec, o2_vec))
            {
                if (ThDuctPortsService.Is_vertical(in_vec, o1_vec))
                    Set_tee_vec(o1_vec, o2_vec, 1, 2, out branch_vec, out other_vec, out branch_idx, out other_idx);
                else
                    Set_tee_vec(o2_vec, o1_vec, 2, 1, out branch_vec, out other_vec, out branch_idx, out other_idx);
            }
            else
            {
                if (ThDuctPortsService.Is_outter(in_vec, o1_vec))
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
        private void Draw_elbow(Special_graph_Info info)
        {
            var elbow_info = Get_elbow_info(info);
            var elbow = ThDuctPortsFactory.Create_elbow(elbow_info.open_angle, elbow_info.duct_width);
            var mat = ThDuctPortsService.Get_trans_mat(elbow_info.trans);
            service.Draw_shape(elbow, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids,
                                                         out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            Collect_special_shape_ids(elbow, geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, "Elbow", mat);
        }
        private static Elbow_Info Get_elbow_info(Special_graph_Info info)
        {
            var in_line = info.lines[0];
            var out_line = info.lines[1];
            double in_width = info.every_port_width[0];
            double out_width = info.every_port_width[1];
            return Record_elbow_info(in_line, out_line, in_width, out_width);
        }
        private static Elbow_Info Record_elbow_info(Line in_line, Line out_line, double in_width, double out_width)
        {
            double rotate_angle;
            var in_vec = ThDuctPortsService.Get_edge_direction(in_line);
            var out_vec = ThDuctPortsService.Get_edge_direction(out_line);
            double open_angle = Math.PI - in_vec.GetAngleTo(out_vec);
            double width = in_width < out_width ? in_width : out_width;
            rotate_angle = ThDuctPortsShapeService.Get_elbow_trans_info(open_angle, in_vec, out_vec, out bool is_flip);
            var trans = new Trans_info(is_flip, rotate_angle, in_line.StartPoint.ToPoint2D());
            return new Elbow_Info(open_angle, width, trans);
        }
        private void Draw_endlines(ThDuctPortsConstructor endlines)
        {
            Draw_special_shape(endlines.endline_elbow);
            for (int i = 0; i < endlines.endline_segs.Count; ++i)
            {
                string pre_duct_text_info = String.Empty;
                var infos = endlines.endline_segs[i];
                var ver_wall_point = (duct_ver_align_points.Count > i) ? duct_ver_align_points[i] : Point2d.Origin;
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
                var cur_seg = service.text_service.Get_endline_duct_info(have_main, main_height, in_param, info, org_dis_mat, ref is_first, ref duct_text_info, out List<DBText> duct_size_info);
                Draw_ports(info);
                service.text_service.Draw_duct_size_info(duct_size_info);
                Collect_duct_geo(geo_set, cur_seg);
                Record_pre_seg_info(cur_seg, duct_text_info, info, ref pre_seg, ref pre_duct_size, ref pre_air_volume);
                Record_duct(cur_seg, pre_seg, pre_duct_size, pre_air_volume);
                if (i == 0)
                    continue;//第一段duct不画reducing，之后的都是reducing+duct
                Record_reducing(geo_set);
                Remove_pre_duct_geo(geo_set);
            }
        }
        private void Record_duct(Line_Info cur_seg, Line_Info pre_seg, string pre_duct_size, double pre_air_volume)
        {
            var duct_param = ThDuctPortsService.Create_duct_modify_param(cur_seg, pre_duct_size, pre_air_volume, start_id.Handle);
            if (duct_param.sp.GetDistanceTo(duct_param.ep) > 1)
            {
                service.Draw_shape(pre_seg, org_dis_mat, out ObjectIdList seg_geo_ids, out ObjectIdList seg_flg_ids, 
                                    out ObjectIdList seg_center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                ThDuctPortsRecoder.Create_duct_group(seg_geo_ids, seg_flg_ids, seg_center_ids, ports_ids, ext_ports_ids, duct_param);
            }
        }
        private void Record_reducing(DBObjectCollection geo_set)
        {
            var reducing = ThDuctPortsFactory.Create_reducing(geo_set);
            service.Draw_shape(reducing, org_dis_mat, out ObjectIdList red_geo_ids, out ObjectIdList red_flg_ids, 
                out ObjectIdList red_center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            var reducing_param = ThDuctPortsService.Create_reducing_modify_param(reducing, start_id.Handle);
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
                var l = ThDuctPortsService.Get_shrink_line(info);
                var mainlines = Get_main_duct(info, out double duct_width);
                ThDuctPortsService.Get_line_pos_info(l, out double angle, out Point3d center_point);
                var mat = Matrix3d.Displacement(center_point.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat = org_dis_mat * mat;
                ThDuctPortsDrawService.Draw_lines(mainlines.geo, mat, service.geo_layer, out ObjectIdList geo_ids);
                ThDuctPortsDrawService.Draw_lines(mainlines.flg, mat, service.flg_layer, out ObjectIdList flg_ids);
                ThDuctPortsDrawService.Draw_lines(mainlines.center_line, org_dis_mat, service.center_layer, out ObjectIdList center_ids);
                // port根据中心线变化
                ThDuctPortsDrawService.Draw_ports(mainlines.ports, mainlines.ports_ext, org_dis_mat,
                                                  out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var param = ThDuctPortsService.Create_duct_modify_param(mainlines, in_param.in_duct_size, info.AirVolume, start_id.Handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
                var dir_vec = (info.Target.Position - info.Source.Position).GetNormal();
                service.text_service.Draw_mainline_text_info(angle, main_height,center_point, dir_vec, org_dis_mat, in_param,ref pre_duct_size_text);
            }
        }
        private Line_Info Get_main_duct(ThDuctEdge<ThDuctVertex> info, out double duct_width)
        {
            var l = ThDuctPortsService.Get_shrink_line(info);
            duct_width = ThDuctPortsService.Get_width(in_param.in_duct_size);
            var outlines = ThDuctPortsFactory.Create_duct(l.Length, duct_width);
            var center_line = new DBObjectCollection { l };
            var outline1 = outlines[0] as Line;
            var outline2 = outlines[1] as Line;
            var flg = new DBObjectCollection{new Line(outline1.StartPoint, outline2.StartPoint),
                                             new Line(outline1.EndPoint, outline2.EndPoint)};
            ThDuctPortsService.Get_ports(l, out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(outlines, flg, center_line, ports, ports_ext);
        }
        private void Draw_ports(Duct_ports_Info info)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var dir_vec = ThDuctPortsService.Get_edge_direction(info.l);
                double angle = ThDuctPortsService.Get_port_rotate_angle(dir_vec);
                foreach (var pos in info.ports_info)
                {
                    if (in_param.port_range.Contains("下"))
                    {
                        var p = ThDuctPortsService.Get_down_port_insert_pos(dir_vec, pos.position, port_width, port_height);
                        p += org_dis_vec;
                        var obj = db.ModelSpace.ObjectId.InsertBlockReference(service.port_layer, service.block_name, p, new Scale3d(), angle);
                        ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, in_param.port_range);
                    }
                    else
                    {
                        ThDuctPortsService.Get_side_port_insert_pos(dir_vec, pos.position, info.width, port_width, out Point3d pL, out Point3d pR);
                        pL += org_dis_vec;
                        pR += org_dis_vec;
                        var obj = db.ModelSpace.ObjectId.InsertBlockReference(service.port_layer, service.block_name, pL, new Scale3d(), angle + Math.PI * 0.5);
                        ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, in_param.port_range);
                        obj = db.ModelSpace.ObjectId.InsertBlockReference(service.port_layer, service.block_name, pR, new Scale3d(), angle - Math.PI * 0.5);
                        ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, in_param.port_range);
                    }
                }
            }
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
            var param = ThDuctPortsService.Create_special_modify_param(info, start_id.Handle, type, mat);
            ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
        }
    }
}