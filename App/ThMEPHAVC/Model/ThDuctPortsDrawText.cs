﻿using System;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.CAD;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawText
    {
        private bool is_first;
        private string duct_size_layer;
        private string duct_size_style;
        public ThDuctPortsDrawText(string duct_size_layer)
        {
            is_first = true;
            duct_size_style = "TH-STYLE3";
            this.duct_size_layer = duct_size_layer;
        }
        public void Get_fjf_duct_info(string scale,
                                      double room_elevation, 
                                      double other_elevation,
                                      List<TextAlignLine> text_alignment,
                                      out List<DBText> duct_size_info)
        {
            duct_size_info = new List<DBText>();
            foreach (var t in text_alignment)
            {
                var w = ThMEPHVACService.Get_width(t.duct_size);
                var text = Create_fjf_duct_info(t.is_room, room_elevation, other_elevation, scale, t.duct_size);
                ThMEPHVACService.Get_line_pos_info(t.l, out double angle, out Point3d center_point);
                var mat = Get_side_text_info_trans_mat(angle, w, center_point, text, t.l);
                var dir_vec = ThMEPHVACService.Get_edge_direction(t.l);
                Seperate_duct_size_elevation(scale, text, mat, dir_vec, out DBText duct_size_text, out DBText elevation_size);
                duct_size_info.Add(duct_size_text);
                duct_size_info.Add(elevation_size);
            }
        }
        public Line_Info Get_endline_duct_info(bool have_main,
                                               double main_height,
                                               ThMEPHVACParam in_param,
                                               Duct_ports_Info info,
                                               Matrix3d org_dis_mat,
                                               ref string pre_duct_text_info,
                                               out List<DBText> duct_size_info)
        {
            duct_size_info = new List<DBText>();
            ThMEPHVACService.Get_line_pos_info(info.l, out double angle, out Point3d center_point);
            ThDuctPortsFactory.Get_duct_geo_flg_center_line(info.l, info.width, angle, center_point, 
                out DBObjectCollection geo, out DBObjectCollection flg, out DBObjectCollection center_line);
            var text = Create_duct_info(!have_main && is_first, in_param.elevation, main_height, in_param.scale, info.duct_size);
            is_first = false;
            var mat = Get_side_text_info_trans_mat(angle, info.width, center_point, text, info.l, in_param);
            mat = org_dis_mat * mat;
            var dir_vec = ThMEPHVACService.Get_edge_direction(info.l);
            Seperate_duct_size_elevation(in_param.scale, text, mat, dir_vec, out DBText duct_size_text, out DBText elevation_size);
            if (pre_duct_text_info != duct_size_text.TextString && info.l.Length > 10)
            {
                duct_size_info.Add(duct_size_text);
                duct_size_info.Add(elevation_size);
                pre_duct_text_info = duct_size_text.TextString;
            }
            ThMEPHVACService.Get_duct_ports(info.l, out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(geo, flg, center_line, ports, ports_ext);
        }
        private Matrix3d Get_side_text_info_trans_mat(double rotate_angle,
                                                      double duct_width,
                                                      Point3d center_point,
                                                      DBText text,
                                                      Line cur_line)
        {
            var dir_vec = ThMEPHVACService.Get_edge_direction(cur_line);
            var vertical_vec = ThMEPHVACService.Get_vertical_vec(dir_vec);
            var leave_duct_mat = vertical_vec * (duct_width * 0.5 + 500);
            var main_mat = Get_main_text_info_trans_mat(rotate_angle, center_point, text);
            main_mat = Matrix3d.Displacement(-vertical_vec * text.Height * 0.5) * main_mat;//Correct to pipe center
            return Matrix3d.Displacement(leave_duct_mat) * main_mat;
        }
        private Matrix3d Get_side_text_info_trans_mat(double rotate_angle,
                                                      double duct_width,
                                                      Point3d center_point,
                                                      DBText text,
                                                      Line cur_line,
                                                      ThMEPHVACParam in_param)
        {
            var dir_vec = ThMEPHVACService.Get_edge_direction(cur_line);
            var vertical_vec = ThMEPHVACService.Get_vertical_vec(dir_vec);
            var leave_duct_mat = vertical_vec * (duct_width * 0.5 + 500);
            var main_mat = Get_main_text_info_trans_mat(rotate_angle, center_point, text);
            main_mat = Matrix3d.Displacement(-vertical_vec * text.Height * 0.5) * main_mat;//Correct to pipe center
            bool is_side = in_param.port_range.Contains("侧");
            return is_side ? main_mat : Matrix3d.Displacement(leave_duct_mat) * main_mat;
        }
        public void Draw_mainline_text_info(double angle,
                                            double main_height,
                                            Point3d center_point,
                                            Vector3d dir_vec,
                                            Matrix3d org_dis_mat,
                                            ThMEPHVACParam in_param,
                                            ref string pre_duct_size_text)
        {
            var text = Create_duct_info(true, in_param.elevation, main_height, in_param.scale, in_param.in_duct_size);
            var mat = Get_main_text_info_trans_mat(angle, center_point, text);
            var vertical_vec = -ThMEPHVACService.Get_vertical_vec(dir_vec);
            mat = org_dis_mat * Matrix3d.Displacement(vertical_vec * text.Height * 0.5) * mat;
            Seperate_duct_size_elevation(in_param.scale, text, mat, dir_vec, out DBText duct_size_text, out DBText elevation_size);
            if (pre_duct_size_text != duct_size_text.TextString)//过滤掉一些标注
            {
                Draw_text(duct_size_text);
                Draw_text(elevation_size);
                pre_duct_size_text = duct_size_text.TextString;
            }
            //Draw_text(duct_size_text);
            //Draw_text(elevation_size);
            //pre_duct_size_text = duct_size_text.TextString;
        }
        private Matrix3d Get_main_text_info_trans_mat(double rotate_angle, Point3d center_point, DBText text)
        {
            while (rotate_angle > 0.5 * Math.PI && (rotate_angle - 0.5 * Math.PI) > 1e-3)
                rotate_angle -= Math.PI;
            double text_len = (text.Bounds == null) ? 0 : (text.Bounds.Value.MaxPoint.X - text.Bounds.Value.MinPoint.X);
            return Matrix3d.Displacement(center_point.GetAsVector()) *
                   Matrix3d.Rotation(rotate_angle, Vector3d.ZAxis, Point3d.Origin) * Matrix3d.Displacement(new Vector3d(-0.5 * text_len, 0, 0));
        }
        public void Draw_duct_size_info(List<DBText> duct_size_info)
        {
            foreach (var info in duct_size_info)
                Draw_text(info);
        }
        private void Draw_text(DBText text)
        {
            using (var db = AcadDatabase.Active())
            {
                db.ModelSpace.Add(text);
                text.SetDatabaseDefaults();
                text.Layer = duct_size_layer;
                text.ColorIndex = (int)ColorIndex.BYLAYER;
                text.Linetype = "ByLayer";
            }
        }
        private DBText Create_fjf_duct_info(bool is_room, double room_elevation, double other_elevation, string scale, string duct_size)
        {
            // 不处理main在树间的情况
            string text_info = !is_room ? $"{duct_size} (h+" + other_elevation.ToString("0.00") + "m)" :
                                          $"{duct_size} (h+" + room_elevation.ToString("0.00") + "m)";
            double h = ThMEPHVACService.Get_text_height(scale);
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
                    Position = Point3d.Origin,
                    HorizontalMode = TextHorizontalMode.TextLeft
                };
            }
        }
        public DBText Create_duct_info( bool is_first,
                                        double elevation,
                                        double main_height,
                                        string scale,
                                        string duct_size)
        {
            // 不处理main在树间的情况
            double duct_height = ThMEPHVACService.Get_height(duct_size);
            double num = is_first ? elevation : (elevation * 1000 + main_height - duct_height) / 1000;
            string text_info = (num > 0) ? $"{duct_size} (h+" + num.ToString("0.00") + "m)":
                                           $"{duct_size} (h" + num.ToString("0.00") + "m)";
            double h = ThMEPHVACService.Get_text_height(scale);
            using (var adb = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetTextStyleId(duct_size_style);
                return new DBText()
                {
                    Height = h, Oblique = 0, Rotation = 0, WidthFactor = 0.7, TextStyleId = id,
                    TextString = text_info, Position = new Point3d(0, 0, 0), 
                    HorizontalMode = TextHorizontalMode.TextLeft
                };
            }
        }
        public static void Seperate_duct_size_elevation(string scale,
                                                        DBText text, 
                                                        Matrix3d mat, 
                                                        Vector3d dir_vec, 
                                                        out DBText duct_size_text, 
                                                        out DBText elevation_size)
        {
            string[] str = text.TextString.Split(' ');
            duct_size_text = text.Clone() as DBText;
            elevation_size = text.Clone() as DBText;
            if (str.Length != 2)
                return;
            duct_size_text.TextString = str[0];
            elevation_size.TextString = str[1];
            double seperate_dis = ThMEPHVACService.Get_text_sep_dis(scale);
            duct_size_text.TransformBy(mat);
            if (Math.Abs(dir_vec.CrossProduct(-Vector3d.YAxis).Z) < 1e-3)
            {
                if (dir_vec.Y > 0)
                    elevation_size.TransformBy(Matrix3d.Displacement(dir_vec * seperate_dis) * mat);
                else
                    elevation_size.TransformBy(Matrix3d.Displacement(-dir_vec * seperate_dis) * mat);
            }
            else if (dir_vec.CrossProduct(-Vector3d.YAxis).Z > 0)
                elevation_size.TransformBy(Matrix3d.Displacement(-dir_vec * seperate_dis) * mat);
            else
                elevation_size.TransformBy(Matrix3d.Displacement(dir_vec * seperate_dis) * mat);
        }
        public void Re_draw_text(Text_modify_param cur_t, string modify_size, ThMEPHVACParam in_param)
        {
            double h = ThMEPHVACService.Get_text_height(in_param.scale);
            var id = Dreambuild.AutoCAD.DbHelper.GetTextStyleId(duct_size_style);
            var text = new DBText()
            {
                Height = h,
                Oblique = 0,
                Rotation = cur_t.rotate_angle,
                WidthFactor = 0.7,
                TextStyleId = id,
                TextString = Re_construct_text(cur_t.text_string, modify_size, in_param),
                Position = cur_t.pos,
                HorizontalMode = TextHorizontalMode.TextLeft
            };
            Draw_text(text);
        }
        private string Re_construct_text(string s, string modify_size, ThMEPHVACParam in_param)
        {
            if (s.Contains("x"))
                return modify_size;
            else
            {
                if (s == "(h+X.XXm)")
                    return s;
                double duct_height = ThMEPHVACService.Get_height(modify_size);
                if (Math.Abs(in_param.main_height) < 1e-3)//风机房内
                    return s;
                double num = (in_param.elevation * 1000 + in_param.main_height - duct_height) / 1000;
                return (num > 0) ? $"(h+" + num.ToString("0.00") + "m)" :
                                   $"(h" + num.ToString("0.00") + "m)";
            }
        }
    }
}
