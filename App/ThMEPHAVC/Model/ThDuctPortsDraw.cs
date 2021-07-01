using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using DotNetARX;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDraw
    {
        private bool have_main;
        private double main_height;
        private double port_width;
        private double port_height;
        private string duct_size_style;
        private string valve_visibility;
        private readonly List<Point2d> duct_dir_align_points;
        private readonly List<Point2d> duct_ver_align_points;
        private List<Entity_param> entity_ids;
        private ThDuctPortsDrawService service;
        private DuctPortsParam in_param;
        private ObjectId start_id;
        private bool is_first;
        public ThDuctPortsDraw(DuctPortsParam in_param_,
                               List<Point2d> duct_dir_align_points_,
                               List<Point2d> duct_ver_align_points_)
        {
            Param_init(in_param_);
            duct_dir_align_points = duct_dir_align_points_;
            duct_ver_align_points = duct_ver_align_points_;
        }
        private void Param_init(DuctPortsParam in_param_)
        {
            is_first = true;
            duct_size_style = "HT-STYLE3";
            valve_visibility = "多叶调节风阀";
            in_param = in_param_;
            ThDuctPortsService.Seperate_size_info(in_param.port_size, out double width, out double height);
            port_width = width;
            port_height = height;
            main_height = ThDuctPortsService.Get_height(in_param.in_duct_size);
            entity_ids = new List<Entity_param>();
            service = new ThDuctPortsDrawService(in_param.scenario, in_param.scale);
        }
        public void Draw(ThDuctPortsAnalysis anay_res, ThDuctPortsConstructor endlines)
        {
            have_main = anay_res.main_ducts.Count != 0;
            start_id = service.Insert_start_flag(anay_res.start_point);
            Draw_endlines(endlines);
            Draw_mainlines(anay_res);
            Draw_special_shape(anay_res.special_shapes_info);
            Draw_port_mark(endlines);
            Insert_valve(anay_res.merged_endlines.Count, endlines);
            service.Attach_start_param(start_id, in_param, entity_ids);
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
            Point3d p = Get_mark_base_point(ports) + new Vector3d(1500, 2000, 0);
            string port_size = port_width.ToString() + 'x' + port_height.ToString();
            double h = ThDuctPortsService.Get_text_height(in_param.scale);
            double scale_h = h * 2 / 3;
            double single_port_volume = in_param.air_volumn / in_param.port_num;
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(service.port_mark_layer, service.port_mark_name, p, new Scale3d(scale_h, scale_h, 1), 0,
                          new Dictionary<string, string> { { "风口名称", in_param.port_name },
                                                           { "尺寸", port_size },
                                                           { "数量", in_param.port_num.ToString() },
                                                           { "风量", single_port_volume.ToString("0.")} });
            }
            Insert_leader(Get_mark_base_point(ports), p);
        }
        private Point3d Get_mark_base_point(List<Port_Info> ports)
        {
            if (ports[ports.Count - 1].air_volume > 1)
                return ports[ports.Count - 1].position;
            else
                return ports[ports.Count - 2].position;
        }
        private void Insert_leader(Point3d srt_p, Point3d end_p)
        {
            Leader leader = new Leader { HasArrowHead = true };
            leader.AppendVertex(srt_p);
            leader.AppendVertex(end_p);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.ModelSpace.Add(leader);
                leader.SetDatabaseDefaults();
                leader.Layer = service.port_mark_layer;
                leader.ColorIndex = (int)ColorIndex.BYLAYER;
                leader.Linetype = "ByLayer";
            }
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
            var mat = Get_cross_trans_mat(cross_info);
            service.Draw_shape(cross, mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids);
            Collect_special_shape_ids(cross, geo_ids, flg_ids, center_ids, "Cross");
        }
        private Cross_Info Get_cross_info(Special_graph_Info info)
        {
            Seperate_cross_vec(info, out int outter_vec_idx, out int collinear_idx, out int inner_vec_idx, out Vector3d in_vec);
            double i_width = info.every_port_width[0];
            double inner_width = info.every_port_width[inner_vec_idx];
            double outter_width = info.every_port_width[outter_vec_idx];
            double collinear_width = info.every_port_width[collinear_idx];
            double rotate_angle = in_vec.GetAngleTo(-Vector3d.YAxis);
            double z = in_vec.CrossProduct(-Vector3d.YAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z < 0)
                rotate_angle += Math.PI;
            bool is_flip = false;
            if (Math.Abs(inner_width - outter_width) > 1e-3)
            {
                int idx = (inner_width > outter_width) ? inner_vec_idx : outter_vec_idx;
                Line l = info.lines[idx];
                Vector3d big_vec = ThDuctPortsService.Get_edge_direction(l);
                if (ThDuctPortsService.Is_outter(in_vec, big_vec))
                    is_flip = true;
            }
            return new Cross_Info(is_flip, i_width, outter_width, collinear_width, inner_width, rotate_angle, info.lines[0].StartPoint);
        }
        private void Seperate_cross_vec(Special_graph_Info info, out int outter_vec_idx, out int collinear_idx, out int inner_vec_idx, out Vector3d in_vec)
        {
            Line i_line = info.lines[0];
            outter_vec_idx = collinear_idx = inner_vec_idx = 0;
            in_vec = ThDuctPortsService.Get_edge_direction(i_line);
            for (int i = 0; i < info.lines.Count; ++i)
            {
                Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(info.lines[i]);
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
        }
        private void Draw_tee(Special_graph_Info info)
        {
            var tee_info = Get_tee_info(info, out Tee_Type type);
            var tee = (type == Tee_Type.BRANCH_VERTICAL_WITH_OTTER) ?
                       ThDuctPortsFactory.Create_r_tee_outlines(tee_info.i_width, tee_info.o_width1, tee_info.o_width2) :
                       ThDuctPortsFactory.Create_v_tee_outlines(tee_info.i_width, tee_info.o_width1, tee_info.o_width2);
            var mat = Get_tee_trans_mat(tee_info);
            service.Draw_shape(tee, mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids);
            Collect_special_shape_ids(tee, geo_ids, flg_ids, center_ids, "Tee");
        }
        private Tee_Info Get_tee_info(Special_graph_Info info, out Tee_Type type)
        {
            bool is_flip;
            double rotate_angle;
            Seperate_tee_vec(info, out Vector3d in_vec, out Vector3d branch_vec, out Vector3d other_vec, out int branch_idx, out int other_idx);
            type = ThDuctPortsService.Is_vertical(branch_vec, other_vec) ? Tee_Type.BRANCH_VERTICAL_WITH_OTTER : Tee_Type.BRANCH_COLLINEAR_WITH_OTTER;
            if (ThDuctPortsService.Is_outter(in_vec, branch_vec))
            {
                is_flip = false;
                rotate_angle = branch_vec.GetAngleTo(Vector3d.XAxis);
                if (branch_vec.CrossProduct(Vector3d.XAxis).Z < 0)
                    rotate_angle = -rotate_angle;
            }
            else
            {
                is_flip = true;
                rotate_angle = branch_vec.GetAngleTo(-Vector3d.XAxis);
                if (branch_vec.CrossProduct(-Vector3d.XAxis).Z < 0)
                    rotate_angle = -rotate_angle;
            }
            double i_width = info.every_port_width[0];
            double o_width1 = info.every_port_width[branch_idx];
            double o_width2 = info.every_port_width[other_idx];
            return new Tee_Info(is_flip, i_width, o_width1, o_width2, rotate_angle, info.lines[0].StartPoint);
        }
        private void Seperate_tee_vec(Special_graph_Info info,
                                      out Vector3d in_vec,
                                      out Vector3d branch_vec,
                                      out Vector3d other_vec,
                                      out int branch_idx,
                                      out int other_idx)
        {
            Line i_line = info.lines[0];
            Line o1_line = info.lines[1];
            Line o2_line = info.lines[2];
            Vector3d o1_vec = ThDuctPortsService.Get_edge_direction(o1_line);
            Vector3d o2_vec = ThDuctPortsService.Get_edge_direction(o2_line);
            in_vec = ThDuctPortsService.Get_edge_direction(i_line);
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
            var mat = Get_elbow_trans_mat(elbow_info);
            service.Draw_shape(elbow, mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids);
            Collect_special_shape_ids(elbow, geo_ids, flg_ids, center_ids, "Elbow");
        }
        private Matrix3d Get_cross_trans_mat(Cross_Info cross_info)
        {
            Matrix3d mat = Matrix3d.Displacement(cross_info.center_point.GetAsVector()) *
                           Matrix3d.Rotation(-cross_info.rotate_angle, Vector3d.ZAxis, Point3d.Origin);
            if (cross_info.is_flip)
                mat *= Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
            return mat;
        }
        private Matrix3d Get_tee_trans_mat(Tee_Info tee_info)
        {
            Matrix3d mat = Matrix3d.Displacement(tee_info.center_point.GetAsVector()) *
                           Matrix3d.Rotation(-tee_info.rotate_angle, Vector3d.ZAxis, Point3d.Origin);
            if (tee_info.is_flip)
                mat *= Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
            return mat;
        }
        private Matrix3d Get_elbow_trans_mat(Elbow_Info elbow_info)
        {
            Matrix3d mat = Matrix3d.Displacement(elbow_info.center_point.GetAsVector()) *
                           Matrix3d.Rotation(-elbow_info.rotate_angle, Vector3d.ZAxis, Point3d.Origin);
            if (elbow_info.is_flip)
                mat *= Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
            return mat;
        }
        private static Elbow_Info Get_elbow_info(Special_graph_Info info)
        {
            Line in_line = info.lines[0];
            Line out_line = info.lines[1];
            double in_width = info.every_port_width[0];
            double out_width = info.every_port_width[1];
            return Record_elbow_info(in_line, out_line, in_width, out_width);
        }
        private static Elbow_Info Record_elbow_info(Line in_line, Line out_line, double in_width, double out_width)
        {
            bool is_flip;
            double angle;
            double width;
            double rotate_angle;
            Vector3d in_vec = ThDuctPortsService.Get_edge_direction(in_line);
            Vector3d out_vec = ThDuctPortsService.Get_edge_direction(out_line);
            angle = Math.PI - in_vec.GetAngleTo(out_vec);
            width = in_width < out_width ? in_width : out_width;
            if (in_vec.CrossProduct(out_vec).Z < 0)
            {
                is_flip = true;
                rotate_angle = Get_elbow_rotate_angle(in_vec, Vector3d.XAxis, angle, ref is_flip);
            }
            else
            {
                is_flip = false;
                rotate_angle = Get_elbow_rotate_angle(in_vec, -Vector3d.XAxis, angle, ref is_flip);
            }
            return new Elbow_Info(is_flip, angle, width, rotate_angle, in_line.StartPoint);
        }
        private static double Get_elbow_rotate_angle(Vector3d in_vec, Vector3d judger_vec, double open_angle, ref bool is_flip)
        {
            if (Math.Abs(open_angle - Math.PI * 0.5) < 1e-3)
            {
                double rotate_angle = in_vec.GetAngleTo(judger_vec);
                return (in_vec.CrossProduct(judger_vec).Z < 0) ? -rotate_angle : rotate_angle;
            }
            else if (open_angle < Math.PI * 0.5)
            {
                double rotate_angle = in_vec.GetAngleTo(-Vector3d.YAxis);
                is_flip = !is_flip;
                return (in_vec.CrossProduct(-Vector3d.YAxis).Z < 0) ? -rotate_angle : rotate_angle;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        private void Draw_endlines(ThDuctPortsConstructor endlins)
        {
            Draw_special_shape(endlins.endline_elbow);
            for (int i = 0; i < endlins.endline_segs.Count; ++i)
            {
                string pre_duct_text_info = String.Empty;
                var infos = endlins.endline_segs[i];
                Point2d ver_wall_point = (duct_ver_align_points.Count > 0) ? duct_ver_align_points[i] : Point2d.Origin;
                Point2d dir_wall_point = (duct_dir_align_points.Count > 0) ? duct_dir_align_points[i] : Point2d.Origin;
                Draw_port_duct(infos.segs, ref pre_duct_text_info);
                service.dim_service.Draw_dimension(infos.segs, dir_wall_point, ver_wall_point);
            }
        }
        private void Draw_port_duct(List<Duct_ports_Info> infos, ref string duct_text_info)
        {
            double pre_air_volume = 0;
            string pre_duct_size = string.Empty;
            var pre_seg = new Line_Info();
            var geo_set = new DBObjectCollection();
            Duct_modify_param param;
            for (int i = 0; i < infos.Count; ++i)
            {
                var info = infos[i];
                var cur_seg = Get_endline_duct_info(info, ref duct_text_info, out List<DBText> duct_size_info);
                Draw_ports(info);
                Draw_duct_size_info(duct_size_info);
                Collect_duct_geo(geo_set, cur_seg);
                Record_pre_seg_info(cur_seg, duct_text_info, info, ref pre_seg, ref pre_duct_size, ref pre_air_volume);
                param = ThDuctPortsService.Create_duct_modify_param(cur_seg, pre_duct_size, pre_air_volume, start_id);
                service.Draw_shape(pre_seg, Matrix3d.Identity, out ObjectIdList seg_geo_ids, out ObjectIdList seg_flg_ids, out ObjectIdList seg_center_ids);
                ThDuctPortsRecoder.Create_duct_group(seg_geo_ids, seg_flg_ids, seg_center_ids, param);
                if (i == 0)
                    continue;
                var reducing = Get_endline_duct_reducing(geo_set);
                service.Draw_shape(reducing, Matrix3d.Identity, out ObjectIdList red_geo_ids, out ObjectIdList red_flg_ids, out ObjectIdList red_center_ids);
                param = ThDuctPortsService.Create_duct_modify_param(reducing, pre_duct_size, pre_air_volume, start_id);
                var id = ThDuctPortsRecoder.Create_reducing_group(red_geo_ids, red_flg_ids, red_center_ids, param);
                entity_ids.Add(new Entity_param (id, "Reducing"));
                Remove_pre_duct_geo(geo_set);
            }
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
        private void Draw_duct_size_info(List<DBText> duct_size_info)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var info in duct_size_info)
                {
                    acadDatabase.ModelSpace.Add(info);
                    info.SetDatabaseDefaults();
                    info.Layer = service.duct_size_layer;
                    info.ColorIndex = (int)ColorIndex.BYLAYER;
                    info.Linetype = "ByLayer";
                }
            }
        }
        private Line_Info Get_endline_duct_info(Duct_ports_Info info,
                                                ref string pre_duct_text_info,
                                                out List<DBText> duct_size_info)
        {
            duct_size_info = new List<DBText>();
            ThDuctPortsService.Get_line_pos_info(info.l, out double angle, out Point3d center_point);
            ThDuctPortsFactory.Get_duct_geo_flg_center_line(info.l, info.width, angle, center_point, out DBObjectCollection geo, out DBObjectCollection flg, out DBObjectCollection center_line);
            DBText text = Create_duct_info(info.duct_size, !have_main && is_first);
            is_first = false;
            Matrix3d mat = Get_side_text_info_trans_mat(angle, info.width, center_point, text, info.l);
            Seperate_duct_size_elevation(text, mat, info.l, out DBText duct_size_text, out DBText elevation_size);
            if (pre_duct_text_info != duct_size_text.TextString && info.l.Length > 10)
            {
                duct_size_info.Add(duct_size_text);
                duct_size_info.Add(elevation_size);
                pre_duct_text_info = duct_size_text.TextString;
            }
            return new Line_Info(geo, flg, center_line);
        }

        private void Seperate_duct_size_elevation(DBText text, Matrix3d mat, Line cur_line, out DBText duct_size_text, out DBText elevation_size)
        {
            string[] str = text.TextString.Split(' ');
            duct_size_text = text.Clone() as DBText;
            elevation_size = text.Clone() as DBText;
            if (str.Length != 2)
                return;
            duct_size_text.TextString = str[0];
            elevation_size.TextString = str[1];
            double seperate_dis = ThDuctPortsService.Get_text_sep_dis(in_param.scale);
            double duct_text_size_len = duct_size_text.Bounds.Value.MaxPoint.X - duct_size_text.Bounds.Value.MinPoint.X + seperate_dis;
            Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(cur_line);
            duct_size_text.TransformBy(mat);
            if (Math.Abs(dir_vec.CrossProduct(-Vector3d.YAxis).Z) < 1e-3)
            {
                if (dir_vec.Y > 0)
                    elevation_size.TransformBy(Matrix3d.Displacement(dir_vec * duct_text_size_len) * mat);
                else
                    elevation_size.TransformBy(Matrix3d.Displacement(-dir_vec * duct_text_size_len) * mat);
            }
            else if (dir_vec.CrossProduct(-Vector3d.YAxis).Z > 0)
                elevation_size.TransformBy(Matrix3d.Displacement(-dir_vec * duct_text_size_len) * mat);
            else
                elevation_size.TransformBy(Matrix3d.Displacement(dir_vec * duct_text_size_len) * mat);
        }
        private Matrix3d Get_side_text_info_trans_mat(double rotate_angle,
                                                      double duct_width,
                                                      Point3d center_point,
                                                      DBText text,
                                                      Line cur_line)
        {
            Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(cur_line);
            Vector3d vertical_vec = Get_text_vertical_vec(dir_vec);
            Vector3d leave_duct_mat = vertical_vec * (duct_width * 0.5 + 500);
            Matrix3d main_mat = Get_main_text_info_trans_mat(rotate_angle, center_point, text);
            main_mat = Matrix3d.Displacement(-vertical_vec * text.Height * 0.5) * main_mat;//Correct to pipe center
            bool is_side = in_param.port_range.Contains("侧");
            return is_side ? main_mat : Matrix3d.Displacement(leave_duct_mat) * main_mat;
        }
        private Matrix3d Get_main_text_info_trans_mat(double rotate_angle,
                                                      Point3d center_point,
                                                      DBText text)
        {
            while (rotate_angle > 0.5 * Math.PI && (rotate_angle - 0.5 * Math.PI) > 1e-3)
                rotate_angle -= Math.PI;
            double text_len = (text.Bounds == null) ? 0 : text.Bounds.Value.MaxPoint.X - text.Bounds.Value.MinPoint.X;
            return Matrix3d.Displacement(center_point.GetAsVector()) *
                   Matrix3d.Rotation(rotate_angle, Vector3d.ZAxis, Point3d.Origin) * Matrix3d.Displacement(new Vector3d(-0.5 * text_len, 0, 0));
        }
        private Line_Info Get_endline_duct_reducing(DBObjectCollection seg_outlines)
        {
            double extend = 50;
            var geo = new DBObjectCollection();
            var flg = new DBObjectCollection();
            var center_line = new DBObjectCollection();
            var l1 = seg_outlines[0] as Line;
            var l2 = seg_outlines[1] as Line;
            var l3 = seg_outlines[2] as Line;
            var l4 = seg_outlines[3] as Line;
            geo.Add(new Line(l1.EndPoint, l3.StartPoint));
            geo.Add(new Line(l2.EndPoint, l4.StartPoint));
            Vector3d dir_vec = (l1.EndPoint - l2.EndPoint).GetNormal();
            flg.Add(new Line(l1.EndPoint + dir_vec * extend, l2.EndPoint - dir_vec * extend));
            dir_vec = (l4.EndPoint - l3.EndPoint).GetNormal();
            flg.Add(new Line(l4.StartPoint + dir_vec * extend, l3.StartPoint - dir_vec * extend));
            center_line.Add(new Line(ThDuctPortsService.Get_mid_point(l1.EndPoint, l2.EndPoint),
                                     ThDuctPortsService.Get_mid_point(l4.StartPoint, l3.StartPoint)));
            return new Line_Info(geo, flg, center_line);
        }
        private void Draw_mainlines(ThDuctPortsAnalysis anay_res)
        {
            string pre_duct_size_text = String.Empty;
            foreach (var info in anay_res.main_ducts)
            {
                Line l = Get_shrink_line(info);
                var mainlines = Get_main_duct(info, out double duct_width, out string duct_size);
                ThDuctPortsService.Get_line_pos_info(l, out double angle, out Point3d center_point);
                Matrix3d mat = Matrix3d.Displacement(center_point.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                ThDuctPortsDrawService.Draw_lines(mainlines.geo, mat, service.geo_layer, out ObjectIdList geo_ids);
                ThDuctPortsDrawService.Draw_lines(mainlines.flg, mat, service.flg_layer, out ObjectIdList flg_ids);
                ThDuctPortsDrawService.Draw_lines(mainlines.center_line, Matrix3d.Identity, service.center_layer, out ObjectIdList center_ids);
                var param = ThDuctPortsService.Create_duct_modify_param(mainlines, duct_size, info.AirVolume, start_id);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, param);
                Draw_mainline_text_info(angle, center_point, info, ref pre_duct_size_text);
            }
        }
        private void Draw_mainline_text_info(double angle,
                                             Point3d center_point,
                                             ThDuctEdge<ThDuctVertex> info,
                                             ref string pre_duct_size_text)
        {
            Line l = new Line(info.Source.Position, info.Target.Position);
            DBText text = Create_duct_info(in_param.in_duct_size, true);
            Matrix3d mat = Get_main_text_info_trans_mat(angle, center_point, text);
            Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(l);
            Vector3d vertical_vec = -Get_text_vertical_vec(dir_vec);
            mat = Matrix3d.Displacement(vertical_vec * text.Height * 0.5) * mat;
            Seperate_duct_size_elevation(text, mat, l, out DBText duct_size_text, out DBText elevation_size);
            if (pre_duct_size_text != duct_size_text.TextString)
            {
                List<DBText> duct_size_info = new List<DBText> { duct_size_text, elevation_size };
                Draw_duct_size_info(duct_size_info);
                pre_duct_size_text = duct_size_text.TextString;
            }
        }
        private Line_Info Get_main_duct(ThDuctEdge<ThDuctVertex> info, out double duct_width, out string duct_size)
        {
            var l = Get_shrink_line(info);
            duct_width = ThDuctPortsService.Get_width(in_param.in_duct_size);
            var outlines = ThDuctPortsFactory.Create_duct(l.Length, duct_width);
            var center_line = new DBObjectCollection { l };
            var outline1 = outlines[0] as Line;
            var outline2 = outlines[1] as Line;
            var flg = new DBObjectCollection{new Line(outline1.StartPoint, outline2.StartPoint),
                                             new Line(outline1.EndPoint, outline2.EndPoint)};
            duct_size = in_param.in_duct_size;
            return new Line_Info(outlines, flg, center_line);
        }
        private void Draw_ports(Duct_ports_Info info)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(info.l);
                double angle = ThDuctPortsService.Get_port_rotate_angle(dir_vec);
                foreach (var pos in info.ports_info)
                {
                    if (in_param.port_range.Contains("下"))
                    {
                        Point3d p = ThDuctPortsService.Get_down_port_insert_pos(dir_vec, pos.position, port_width, port_height);
                        var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(service.port_layer, service.block_name, p, new Scale3d(), angle);
                        ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, in_param.port_range);
                    }
                    else
                    {
                        ThDuctPortsService.Get_side_port_insert_pos(dir_vec, pos.position, info.width, port_width, out Point3d pL, out Point3d pR);
                        var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(service.port_layer, service.block_name, pL, new Scale3d(), angle + Math.PI * 0.5);
                        ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, in_param.port_range);
                        obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(service.port_layer, service.block_name, pR, new Scale3d(), angle - Math.PI * 0.5);
                        ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, in_param.port_range);
                    }
                }
            }
        }
        private Line Get_shrink_line(ThDuctEdge<ThDuctVertex> edge)
        {
            Point3d src_point = edge.Source.Position;
            Point3d tar_point = edge.Target.Position;
            Vector3d dir_vec = (tar_point - src_point).GetNormal();
            Point3d new_src_point = src_point + dir_vec * edge.SourceShrink;
            Point3d new_tar_point = tar_point - dir_vec * edge.TargetShrink;
            return new Line(new_src_point, new_tar_point);
        }
        private Vector3d Get_text_vertical_vec(Vector3d dir_vec)
        {
            Vector3d vertical_vec;
            if (Math.Abs(dir_vec.X) < 1e-3)
            {
                vertical_vec = (dir_vec.Y > 0) ? ThDuctPortsService.Get_left_vertical_vec(dir_vec) :
                                                 ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            }
            else if (dir_vec.X > 0)
                vertical_vec = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            else
                vertical_vec = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            return vertical_vec;
        }

        private DBText Create_duct_info(string duct_size, bool is_first)
        {
            // 不处理main在树间的情况
            double duct_height = ThDuctPortsService.Get_height(duct_size);
            double num = is_first ? in_param.elevation : (in_param.elevation * 1000 + main_height - duct_height) / 1000;
            string text_info;
            if (num > 0)
                text_info = $"{duct_size} (h+" + num.ToString("0.00") + "m)";
            else
                text_info = $"{duct_size} (h" + num.ToString("0.00") + "m)";
            double h = ThDuctPortsService.Get_text_height(in_param.scale);
            using (var adb = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetTextStyleId(duct_size_style);
                return new DBText()
                {
                    Height = h,
                    Oblique = 0,
                    Rotation = 0,
                    WidthFactor = 0.7,
                    TextStyleId = id,
                    TextString = text_info,
                    Position = new Point3d(0, 0, 0),
                    HorizontalMode = TextHorizontalMode.TextLeft
                };
            }
        }
        private void Insert_valve(int count, ThDuctPortsConstructor endlines)
        {
            if (count == 1)
                return;
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var endline in endlines.endline_segs)
                {
                    if (endline.is_in)
                    {
                        double width = endline.segs[0].width;
                        Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(endline.segs[0].l);
                        Vector3d vertical_r = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
                        double angle = dir_vec.GetAngleTo(-Vector3d.YAxis);
                        double cross_z = dir_vec.CrossProduct(-Vector3d.YAxis).Z;
                        if (Math.Abs(cross_z) < 1e-3)
                            cross_z = 0;
                        if (cross_z > 0 && Math.Abs(cross_z) > 1e-3)
                            angle += Math.PI;
                        double text_angle = (angle > 0 || angle < Math.PI) ? angle - Math.PI : angle;
                        Point3d tar_p = endline.segs[0].start_point + vertical_r * width * 0.5;
                        var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(service.valve_layer, service.valve_name, tar_p, new Scale3d(), angle);
                        ThDuctPortsDrawService.Set_valve_dyn_block_properity(obj, width, 250, text_angle, valve_visibility);
                    }
                }
            }
        }
        private void Collect_special_shape_ids(Line_Info info,
                                               ObjectIdList geo_ids,
                                               ObjectIdList flg_ids,
                                               ObjectIdList center_ids,
                                               string shape_name)
        {
            var param = ThDuctPortsService.Create_special_modify_param(info, start_id);
            var id = ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, shape_name, param);
            entity_ids.Add(new Entity_param(id, shape_name));
        }
    }
}