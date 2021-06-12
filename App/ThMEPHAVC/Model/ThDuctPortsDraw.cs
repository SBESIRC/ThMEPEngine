using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThMEPHVAC.Duct;
using DotNetARX;
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
    public class ThDuctPortsDraw
    {
        private int port_num;
        private double air_volumn;
        private double elevation;
        private double port_width;
        private double port_height;
        private string port_range;
        private string scenario;
        private string geo_layer;
        private string flg_layer;
        private string port_layer;
        private string center_layer;
        private string duct_size_layer;
        private string dimension_layer;
        private string port_mark_layer;
        private string scale;
        private string port_name;
        private string block_name;
        private string port_mark_name;
        private string duct_size_style;
        private string ui_duct_size;
        private List<Point2d> align_points;

        private bool have_main;
        public ThDuctPortsDraw(ThDuctPortsParam in_param,
                               List<Point2d> align_points_)
        {
            scenario = in_param.scenario;
            block_name = "风口-AI研究中心";
            port_mark_name = "风口标注";
            duct_size_style = "HT-STYLE3";
            Set_layer();
            Import_Layer_Block();
            elevation = in_param.elevation;
            string[] s = in_param.port_size.Split('x');
            port_width = Double.Parse(s[0]);
            port_height = Double.Parse(s[1]);
            scale = in_param.scale;
            port_name = in_param.port_name;
            port_range = in_param.port_range;
            align_points = align_points_;
            port_num = in_param.port_num;
            air_volumn = in_param.air_volumn;
            ui_duct_size = in_param.in_duct_size;
        }
        public void Draw(ThDuctPortsAnalysis anay_res, ThDuctPortsConstructor endlines)
        {
            have_main = anay_res.main_ducts.Count != 0;
            Draw_endlines(endlines, anay_res.ui_duct_width);
            Draw_mainlines(anay_res, anay_res.ui_duct_width);
            Draw_special_shape(anay_res.special_shapes_info);
            Draw_port_mark(endlines);
        }

        private void Draw_port_mark(ThDuctPortsConstructor endlines)
        {
            var last_seg = endlines.endline_segs[endlines.endline_segs.Count - 1];
            var ports = last_seg[last_seg.Count - 1].ports_position;
            Point3d p = ports[0] + new Vector3d(500, 2000, 0);
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_mark_layer, port_mark_name, p, new Scale3d(100, 100 ,1), 0,
                          new Dictionary<string, string> { { "风口名称", port_name }, 
                                                           { "尺寸", ui_duct_size }, 
                                                           { "数量", port_num.ToString() }, 
                                                           { "风量", air_volumn.ToString() } });
            }
            Line l = new Line(ports[0], p);
            var line_set = new DBObjectCollection() { l };
            Draw_lines(line_set, Matrix3d.Identity, port_mark_layer);
        }
        private void Set_layer()
        {
            switch (scenario)
            {
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                    geo_layer = "H-DUCT-DUAL";
                    flg_layer = "H-DAPP-DAPP";
                    port_layer = "H-DAPP-DGRIL";
                    center_layer = "H-DUCT-DUAL-MID";
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
                    duct_size_layer = "H-DIMS-DUCT";
                    dimension_layer = "H-DIMS-DUCT";
                    port_mark_layer = "H-DIMS-DUCT";
                    break;
            }
        }
        private void Import_Layer_Block()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(geo_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(flg_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(port_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(center_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(duct_size_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(dimension_layer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(port_mark_layer));
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(port_mark_name), false);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(block_name), false);
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
            Draw_lines(cross.geo, mat, geo_layer);
            Draw_lines(cross.flg, mat, flg_layer);
            Draw_lines(cross.center_line, mat, center_layer);
        }

        private Cross_Info Get_cross_info(Special_graph_Info info)
        {
            Seperate_cross_vec(info, out int outter_vec_idx, out int collinear_idx, out int inner_vec_idx, out Vector3d in_vec);
            double i_width = info.every_port_width[0];
            double inner_width = info.every_port_width[inner_vec_idx];
            double outter_width = info.every_port_width[outter_vec_idx];
            double collinear_width = info.every_port_width[collinear_idx];
            double rotate_angle = in_vec.GetAngleTo(-Vector3d.YAxis);
            bool is_flip = false;
            if (Math.Abs(inner_width - outter_width) > 1e-3)
            {
                int idx = (inner_width > outter_width) ? inner_vec_idx : outter_vec_idx;
                Line l = info.lines[idx];
                Vector3d big_vec = (l.EndPoint - l.StartPoint).GetNormal();
                if (Is_outter(in_vec, big_vec))
                    is_flip = true;
            }
            return new Cross_Info(is_flip, i_width, outter_width, collinear_width, inner_width, rotate_angle, info.lines[0].StartPoint);
        }

        private void Seperate_cross_vec(Special_graph_Info info, out int outter_vec_idx, out int collinear_idx, out int inner_vec_idx, out Vector3d in_vec)
        {
            Line i_line = info.lines[0];
            outter_vec_idx = collinear_idx = inner_vec_idx = 0;
            in_vec = (i_line.EndPoint - i_line.StartPoint).GetNormal();
            for (int i = 0; i < info.lines.Count; ++i)
            {
                Line l = info.lines[i];
                Vector3d dir_vec = (l.EndPoint - l.StartPoint).GetNormal();
                if (Is_vertical(in_vec, dir_vec))
                {
                    if (Is_outter(in_vec, dir_vec))
                        outter_vec_idx = i;
                    else
                        inner_vec_idx = i;
                }
                else
                {
                    collinear_idx = i;
                }
            }
        }
        private void Draw_tee(Special_graph_Info info)
        {
            var tee_info = Get_tee_info(info, out Tee_Type type);
            var tee = (type == Tee_Type.BRANCH_VERTICAL_WITH_OTTER) ?
                       ThDuctPortsFactory.Create_r_tee_outlines(tee_info.i_width, tee_info.o_width1, tee_info.o_width2) :
                       ThDuctPortsFactory.Create_v_tee_outlines(tee_info.i_width, tee_info.o_width1, tee_info.o_width2);
            var mat = Get_tee_trans_mat(tee_info);
            Draw_lines(tee.geo, mat, geo_layer);
            Draw_lines(tee.flg, mat, flg_layer);
            Draw_lines(tee.center_line, mat, center_layer);
        }

        private Tee_Info Get_tee_info(Special_graph_Info info, out Tee_Type type)
        {
            bool is_flip;
            double rotate_angle;
            Seperate_tee_vec(info, out Vector3d in_vec, out Vector3d branch_vec, out Vector3d other_vec, out int branch_idx, out int other_idx);
            type = Is_vertical(branch_vec, other_vec) ? Tee_Type.BRANCH_VERTICAL_WITH_OTTER : Tee_Type.BRANCH_COLLINEAR_WITH_OTTER;
            if (Is_outter(in_vec, branch_vec))
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
            Vector3d o1_vec = (o1_line.EndPoint - o1_line.StartPoint).GetNormal();
            Vector3d o2_vec = (o2_line.EndPoint - o2_line.StartPoint).GetNormal();
            in_vec = (i_line.EndPoint - i_line.StartPoint).GetNormal();
            if (Is_vertical(o1_vec, o2_vec))
            {
                if (Is_vertical(in_vec, o1_vec))
                    Set_tee_vec(o1_vec, o2_vec, 1, 2, out branch_vec, out other_vec, out branch_idx, out other_idx);
                else
                    Set_tee_vec(o2_vec, o1_vec, 2, 1, out branch_vec, out other_vec, out branch_idx, out other_idx);
            }
            else
            {
                if (Is_outter(in_vec, o1_vec))
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
            Draw_lines(elbow.geo, mat, geo_layer);
            Draw_lines(elbow.flg, mat, flg_layer);
            Draw_lines(elbow.center_line, mat, center_layer);
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
            Vector3d in_vec = (in_line.EndPoint - in_line.StartPoint).GetNormal();
            Vector3d out_vec = (out_line.EndPoint - out_line.StartPoint).GetNormal();
            angle = Math.PI - in_vec.GetAngleTo(out_vec);
            width = in_width < out_width ? in_width : out_width;
            if (in_vec.CrossProduct(out_vec).Z < 0)
            {
                is_flip = true;
                rotate_angle = Get_elbow_rotate_angle(in_vec, out_vec, Vector3d.XAxis, angle, ref is_flip);
            }
            else
            {
                is_flip = false;
                rotate_angle = Get_elbow_rotate_angle(in_vec, out_vec, -Vector3d.XAxis, angle, ref is_flip);
            }
            return new Elbow_Info(is_flip, angle, width, rotate_angle, in_line.StartPoint);
        }
        private static double Get_elbow_rotate_angle(Vector3d in_vec, Vector3d out_vec, Vector3d judger_vec, double open_angle, ref bool is_flip)
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
        private void Draw_endlines(ThDuctPortsConstructor endlins, double main_height)
        {
            string pre_duct_text_info = String.Empty;
            Draw_special_shape(endlins.endline_elbow);
            for (int i = 0; i < endlins.endline_segs.Count; ++i)
            {
                var infos = endlins.endline_segs[i];
                Draw_port_duct(infos, main_height, align_points[i], ref pre_duct_text_info);
            }
        }
        private void Draw_port_duct(List<Duct_ports_Info> infos, double main_height, Point2d wall_point, ref string pre_duct_text_info)
        {
            var seg_outlines = Get_endline_duct_info(infos, main_height, ref pre_duct_text_info, out List<DBText> duct_size_info);
            var reducing = Get_endline_duct_reducing(seg_outlines.geo);

            Draw_lines(seg_outlines.geo, Matrix3d.Identity, geo_layer);
            Draw_lines(seg_outlines.flg, Matrix3d.Identity, geo_layer);
            Draw_lines(seg_outlines.center_line, Matrix3d.Identity, center_layer);
            Draw_lines(reducing.geo, Matrix3d.Identity, geo_layer);
            Draw_lines(reducing.flg, Matrix3d.Identity, flg_layer);
            Draw_lines(reducing.center_line, Matrix3d.Identity, center_layer);
            Draw_ports(infos);
            Draw_duct_size_info(duct_size_info);
            Draw_dimension(infos, wall_point);
        }

        private void Draw_dimension(List<Duct_ports_Info> infos, Point2d wall_point)
        {
            Vector3d vec = Get_edge_direction(infos[0].l);
            if (!Is_vertical(vec, Vector3d.XAxis) && !Is_vertical(vec, Vector3d.YAxis))
                return;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Layers.ElementOrDefault(dimension_layer).ObjectId;
                Insert_wall_point(infos, wall_point);
                for (int i = 0; i < infos.Count; ++i)
                {
                    var info = infos[i];
                    Vector3d dir_vec = Get_edge_direction(info.l);
                    Vector3d vertical_vec;
                    if (Math.Abs(dir_vec.X) < 1e-3)
                        vertical_vec = (dir_vec.Y > 0) ? Get_right_vertical_vec(dir_vec) : Get_left_vertical_vec(dir_vec);
                    else if (dir_vec.X > 0)
                        vertical_vec = Get_right_vertical_vec(dir_vec);
                    else
                        vertical_vec = Get_left_vertical_vec(dir_vec);

                    for (int j = 0; j < info.ports_position.Count - 1; ++j)
                    {
                        var dim = Create_align_dim(info.ports_position[j], info.ports_position[j + 1], vertical_vec, layerId);
                        acadDatabase.ModelSpace.Add(dim);
                        dim.SetDatabaseDefaults();
                    }
                    Draw_gap_dimension(infos, i, vertical_vec, layerId, acadDatabase);
                }
            }
        }

        private void Insert_wall_point(List<Duct_ports_Info> infos, Point2d wall_point)
        {
            bool had_insert = false;
            Point3d wall_p = new Point3d(wall_point.X, wall_point.Y, 0);
            foreach (var info in infos)
            {
                if (Is_between_points(wall_p, info.l.StartPoint, info.l.EndPoint))
                {
                    had_insert = true;
                    Search_nearest_point(info, wall_p, out int min_idx);
                    Insert_wall_point(info, wall_p, min_idx);
                    break;
                }
            }
            if (!had_insert)
                Insert_out_wall_point(infos, wall_p);
        }

        private void Insert_out_wall_point(List<Duct_ports_Info> infos, Point3d wall_p)
        {
            if (infos.Count > 0)
            {
                var first_info = infos[0];
                var first_pos = first_info.ports_position[0];
                var last_info = infos[infos.Count - 1];
                if (last_info.ports_position.Count > 0)
                {
                    var last_pos = last_info.ports_position[last_info.ports_position.Count - 1];
                    if (wall_p.DistanceTo(last_pos) < wall_p.DistanceTo(first_pos))
                        last_info.ports_position.Insert(last_info.ports_position.Count, wall_p);
                    else
                        first_info.ports_position.Insert(0, wall_p);
                }
                else
                    first_info.ports_position.Insert(0, wall_p);
            }
        }

        private void Insert_wall_point(Duct_ports_Info info, Point3d wall_p, int min_idx)
        {
            if (info.ports_position.Count > 0)
            {
                Point3d min_p = info.ports_position[min_idx];
                Vector3d dir1 = (wall_p - min_p).GetNormal();
                Vector3d dir2 = Get_edge_direction(info.l);
                if (Math.Abs(dir1.DotProduct(dir2) - 1) < 1e-3)
                    info.ports_position.Insert(min_idx, wall_p);
                else
                    info.ports_position.Insert(min_idx + 1, wall_p);
            }
        }
        private void Search_nearest_point(Duct_ports_Info info, Point3d wall_p, out int min_idx)
        {
            min_idx = 0;
            double min_dis = Double.MaxValue;
            for (int i = 0; i < info.ports_position.Count; ++i)
            {
                double cur_dis = wall_p.DistanceTo(info.ports_position[i]);
                if (cur_dis < min_dis)
                {
                    min_dis = cur_dis;
                    min_idx = i;
                }
            }
        }
        private bool Is_between_points(Point3d p, Point3d p1, Point3d p2)
        {
            //判断直线上的三个点 其中某点是否在其他两个之间
            Vector3d v1 = (p - p1).GetNormal();
            Vector3d v2 = (p - p2).GetNormal();
            return Math.Abs(v1.GetAngleTo(v2)) > 1e-3;
        }
        private void Draw_gap_dimension(List<Duct_ports_Info> infos, int idx, Vector3d vertical_vec, ObjectId layerId, AcadDatabase acadDatabase)
        {
            if (idx < infos.Count - 1)
            {
                var info = infos[idx];
                var next_info = infos[idx + 1];
                if (next_info.ports_position.Count > 0)
                {
                    Point3d nearest_p = Search_nearest_point(info, next_info, out Point3d next_p);
                    var dim = Create_align_dim(nearest_p, next_p, vertical_vec, layerId);
                    acadDatabase.ModelSpace.Add(dim);
                    dim.SetDatabaseDefaults();
                }
            }
        }
        private Point3d Search_nearest_point(Duct_ports_Info info, Duct_ports_Info next_info, out Point3d next_p)
        {
            next_p = next_info.ports_position[next_info.ports_position.Count - 1];
            Point3d p1 = info.ports_position[0];
            Point3d p2 = info.ports_position[info.ports_position.Count - 1];
            return (next_p.DistanceTo(p1) < next_p.DistanceTo(p2)) ? p1 : p2;
        }
        private AlignedDimension Create_align_dim(Point3d p1, Point3d p2, Vector3d vertical_vec, ObjectId layerId)
        {
            string style = "TH-DIM150";
            if (scale == "1:100")
                style = "TH-DIM100";
            else if (scale == "1:50")
                style = "TH-DIM50";
            using(var adb = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, adb.Database);
                return new AlignedDimension
                {
                    XLine1Point = p1,
                    XLine2Point = p2,
                    DimensionText = "",
                    DimLinePoint = Get_mid_point(p1, p2) + vertical_vec * 1200,
                    ColorIndex = 256,
                    DimensionStyle = id,
                    LayerId = layerId
                };
            }
        }
        private void Draw_duct_size_info(List<DBText> duct_size_info)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var info in duct_size_info)
                {
                    acadDatabase.ModelSpace.Add(info);
                    info.SetDatabaseDefaults();
                    info.Layer = duct_size_layer;
                }
            }
        }

        private Line_Info Get_endline_duct_info(List<Duct_ports_Info> infos,
                                                double main_height,
                                                ref string pre_duct_text_info,
                                                out List<DBText> duct_size_info)
        {
            var geo = new DBObjectCollection();
            var flg = new DBObjectCollection();
            var center_line = new DBObjectCollection();
            duct_size_info = new List<DBText>();
            for (int i = 0; i < infos.Count; ++i)
            {
                var info = infos[i];
                Get_line_pos_info(info.l, out double angle, out Point3d center_point);
                Get_duct_geo_flg_centerline(info, geo, flg, center_line, angle, center_point);
                DBText text = Create_duct_info(info.duct_size, main_height, !have_main && (i == 0));
                Matrix3d mat = Get_side_text_info_trans_mat(angle, info.width, center_point, text, info.l);
                Seperate_duct_size_elevation(text, mat, info.l, out DBText duct_size_text, out DBText elevation_size);
                if (pre_duct_text_info != duct_size_text.TextString)
                {
                    duct_size_info.Add(duct_size_text);
                    duct_size_info.Add(elevation_size);
                    pre_duct_text_info = duct_size_text.TextString;
                }
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
            double seperate_dis = 500;
            if (scale == "1:100")
                seperate_dis = 300;
            else if (scale == "1:50")
                seperate_dis = 100;
            double duct_text_size_len = duct_size_text.Bounds.Value.MaxPoint.X - duct_size_text.Bounds.Value.MinPoint.X + seperate_dis;
            Vector3d dir_vec = (cur_line.EndPoint - cur_line.StartPoint).GetNormal();
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

        private void Get_duct_geo_flg_centerline(Duct_ports_Info info,
                                                 DBObjectCollection geo,
                                                 DBObjectCollection flg,
                                                 DBObjectCollection center_line,
                                                 double angle,
                                                 Point3d center_point)
        {
            var lines = ThDuctPortsFactory.Create_duct(info.l.Length, info.width);
            Matrix3d mat = Matrix3d.Displacement(center_point.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
            Line l1 = lines[0] as Line;
            l1.TransformBy(mat);
            geo.Add(l1);
            Line l2 = lines[1] as Line;
            l2.TransformBy(mat);
            geo.Add(l2);
            flg.Add(new Line(l1.StartPoint, l2.StartPoint));
            flg.Add(new Line(l1.EndPoint, l2.EndPoint));
            center_line.Add(info.l);
        }
        private Matrix3d Get_side_text_info_trans_mat(double rotate_angle,
                                                      double duct_width,
                                                      Point3d center_point,
                                                      DBText text,
                                                      Line cur_line)
        {
            Vector3d dir_vec = (cur_line.EndPoint - cur_line.StartPoint).GetNormal();
            Vector3d vertical_vec = Get_vertical_vec(dir_vec);
            Vector3d leave_duct_mat = vertical_vec * (duct_width * 0.5 + 300);
            Matrix3d main_mat = Get_main_text_info_trans_mat(rotate_angle, center_point, text);
            main_mat = Matrix3d.Displacement(-vertical_vec * text.Height * 0.5) * main_mat;//Correct to pipe center
            bool is_side = port_range.Contains("侧");
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
            for (int i = 0; i <= seg_outlines.Count - 4; i += 2)
            {
                var l1 = seg_outlines[i] as Line;
                var l2 = seg_outlines[i + 1] as Line;
                var l3 = seg_outlines[i + 2] as Line;
                var l4 = seg_outlines[i + 3] as Line;
                geo.Add(new Line(l1.EndPoint, l3.StartPoint));
                geo.Add(new Line(l2.EndPoint, l4.StartPoint));
                Vector3d dir_vec = (l1.EndPoint - l2.EndPoint).GetNormal();
                flg.Add(new Line(l1.EndPoint + dir_vec * extend, l2.EndPoint - dir_vec * extend));
                dir_vec = (l4.EndPoint - l3.EndPoint).GetNormal();
                flg.Add(new Line(l4.StartPoint + dir_vec * extend, l3.StartPoint - dir_vec * extend));
                center_line.Add(new Line(Get_mid_point(l1.EndPoint, l2.EndPoint), Get_mid_point(l4.StartPoint, l3.StartPoint)));
            }
            return new Line_Info(geo, flg, center_line);
        }
        private void Draw_mainlines(ThDuctPortsAnalysis anay_res, double main_height)
        {
            string pre_duct_size_text = String.Empty;
            foreach (var info in anay_res.main_ducts)
            {
                Line l = Get_shrink_line(info);
                var mainlines = Get_main_duct(info, out double duct_width);
                Get_line_pos_info(l, out double angle, out Point3d center_point);
                Matrix3d mat = Matrix3d.Displacement(center_point.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                Draw_lines(mainlines.geo, mat, geo_layer);
                Draw_lines(mainlines.flg, mat, geo_layer);
                Draw_lines(mainlines.center_line, Matrix3d.Identity, center_layer);
                Draw_mainline_text_info(main_height, angle, center_point, info, ref pre_duct_size_text);
            }
        }
        private void Draw_mainline_text_info(double main_height,
                                             double angle,
                                             Point3d center_point,
                                             ThDuctEdge<ThDuctVertex> info,
                                             ref string pre_duct_size_text)
        {
            Line l = new Line(info.Source.Position, info.Target.Position);
            DBText text = Create_duct_info(ui_duct_size, main_height, true);
            Matrix3d mat = Get_main_text_info_trans_mat(angle, center_point, text);
            Vector3d dir_vec = Get_edge_direction(l);
            Vector3d vertical_vec = -Get_vertical_vec(dir_vec);
            mat = Matrix3d.Displacement(vertical_vec * text.Height * 0.5) * mat;
            Seperate_duct_size_elevation(text, mat, l, out DBText duct_size_text, out DBText elevation_size);
            if (pre_duct_size_text != duct_size_text.TextString)
            {
                List<DBText> duct_size_info = new List<DBText> { duct_size_text, elevation_size };
                Draw_duct_size_info(duct_size_info);
                pre_duct_size_text = duct_size_text.TextString;
            }
        }
        private Line_Info Get_main_duct(ThDuctEdge<ThDuctVertex> info, out double duct_width)
        {
            Line l = Get_shrink_line(info);
            String []s = ui_duct_size.Split('x');
            duct_width = Double.Parse(s[0]);
            var outlines = ThDuctPortsFactory.Create_duct(l.Length, duct_width);
            var center_line = new DBObjectCollection { l };
            Line outline1 = outlines[0] as Line;
            Line outline2 = outlines[1] as Line;
            var flg = new DBObjectCollection{new Line(outline1.StartPoint, outline2.StartPoint),
                                             new Line(outline1.EndPoint, outline2.EndPoint)};
            return new Line_Info(outlines, flg, center_line);
        }
        private void Get_line_pos_info(Line l, out double angle, out Point3d center_point)
        {
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            Vector2d edge_vec = new Vector2d(end_p.X - srt_p.X, end_p.Y - srt_p.Y);
            angle = edge_vec.Angle;
            center_point = new Point3d(0.5 * (srt_p.X + end_p.X), 0.5 * (srt_p.Y + end_p.Y), 0);
        }
        private void Draw_ports(List<Duct_ports_Info> infos)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var info in infos)
                {
                    Vector3d dir_vec = Get_edge_direction(info.l);
                    double angle = Get_port_rotate_angle(dir_vec);
                    foreach (var pos in info.ports_position)
                    {
                        if (port_range.Contains("下"))
                        {
                            Point3d p = Get_down_port_insert_pos(dir_vec, pos);
                            var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_layer, block_name, p, new Scale3d(), angle);
                            Set_port_dyn_block_properity(obj);
                        }
                        else
                        {
                            Get_side_port_insert_pos(dir_vec, pos, info.width, out Point3d pL, out Point3d pR);
                            var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_layer, block_name, pL, new Scale3d(), angle + Math.PI * 0.5);
                            Set_port_dyn_block_properity(obj);
                            obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_layer, block_name, pR, new Scale3d(), angle - Math.PI * 0.5);
                            Set_port_dyn_block_properity(obj);
                        }
                    }
                }
            }
        }
        private Point3d Get_down_port_insert_pos(Vector3d dir_vec, Point3d pos)
        {
            Vector3d vertical_left = Get_left_vertical_vec(dir_vec);
            Vector3d dis_vec = dir_vec * port_height * 0.5 + vertical_left * (500 - port_width * 0.5);
            return pos + dis_vec;
        }
        private void Get_side_port_insert_pos(Vector3d dir_vec, Point3d pos, double duct_width, out Point3d pL, out Point3d pR)
        {
            Vector3d vertical_left = Get_left_vertical_vec(dir_vec);
            pL = pos - dir_vec * (500 - port_width * 0.5) + vertical_left * (duct_width * 0.5 + 100);
            Vector3d vertical_right = Get_right_vertical_vec(dir_vec);
            pR = pos + dir_vec * (500 - port_width * 0.5) + vertical_right * (duct_width * 0.5 + 100);
        }
        private double Get_port_rotate_angle(Vector3d dir_vec)
        {
            Vector3d judger = -Vector3d.YAxis;
            double angle = dir_vec.GetAngleTo(judger);
            if (judger.CrossProduct(dir_vec).Z < 0)
                angle = -angle;
            return angle;
        }
        private void Set_port_dyn_block_properity(ObjectId obj)
        {
            var data = new ThBlockReferenceData(obj);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER, port_width);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT, port_height);
            if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE))
                data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE, port_range);
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
        private void Draw_lines(DBObjectCollection lines, Matrix3d trans_mat, string str_layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (Curve obj in lines)
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                    obj.Layer = str_layer;
                    obj.TransformBy(trans_mat);
                }
            }
        }
        private Vector3d Get_left_vertical_vec(Vector3d dir_vec)
        {
            return new Vector3d(-dir_vec.Y, dir_vec.X, 0);
        }
        private Vector3d Get_right_vertical_vec(Vector3d dir_vec)
        {
            return new Vector3d(dir_vec.Y, -dir_vec.X, 0);
        }
        private bool Is_vertical(Vector3d v1, Vector3d v2)
        {
            return Math.Abs(v1.DotProduct(v2)) < 1e-1 ? true : false;
        }
        private bool Is_outter(Vector3d v1, Vector3d v2)
        {
            return v1.CrossProduct(v2).Z > 0 ? true : false;
        }
        private Point3d Get_mid_point(Point3d p1, Point3d p2)
        {
            return new Point3d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, 0);
        }
        private static Vector3d Get_edge_direction(Line l)
        {
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            return (end_p - srt_p).GetNormal();
        }
        private Vector3d Get_vertical_vec(Vector3d dir_vec)
        {
            Vector3d vertical_vec;
            if (Math.Abs(dir_vec.X) < 1e-3)
                vertical_vec = (dir_vec.Y > 0) ? Get_left_vertical_vec(dir_vec) : Get_right_vertical_vec(dir_vec);
            else if (dir_vec.X > 0)
                vertical_vec = Get_left_vertical_vec(dir_vec);
            else
                vertical_vec = Get_right_vertical_vec(dir_vec);
            return vertical_vec;
        }
        private DBText Create_duct_info(string duct_size, double main_height, bool is_first)
        {
            // 不处理main在树间的情况
            string[] s = duct_size.Split('x');
            if (s.Length != 2)
                return new DBText();
            double duct_height = Double.Parse(s[1]);
            double num = is_first ? elevation : (elevation * 1000 + duct_height - main_height) / 1000;
            string text_info;
            if (num > 0)
                text_info = $"{duct_size} (h+" + num.ToString("0.00") + "m)";
            else
                text_info = $"{duct_size} (h" + num.ToString("0.00") + "m)";
            double h = 450;
            if (scale == "1:100")
                h = 300;
            else if (scale == "1:50")
                h = 150;
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
    }
}
