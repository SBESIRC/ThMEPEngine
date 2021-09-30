using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using NFox.Cad;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsModifyDuct
    {
        private Tolerance tor;
        private Handle start_handle;
        private Point2d start_point;
        private Vector3d org_dis_vec;
        private Matrix3d org_dis_mat;
        private ThDuctPortsDrawService service;
        private DBObjectCollection hoses_bounds;                     // 软接外包框
        private Dictionary<Polyline, Fan_modify_param> fans_dic;     // 风机外包框到文字参数的映射
        private Dictionary<Polyline, Port_modify_param> ports_dic;   // 风口外包框到文字参数的映射
        private Dictionary<Polyline, Text_modify_param> texts_dic;   // 文字外包框到文字参数的映射
        private Dictionary<Polyline, Duct_modify_param> ducts_dic;   // 管段外包框到管段参数的映射
        private Dictionary<Polyline, Entity_modify_param> shapes_dic;// 连接件外包框到连接件参数的映射
        private ThCADCoreNTSSpatialIndex hoses_index;
        private ThCADCoreNTSSpatialIndex fans_index;
        private ThCADCoreNTSSpatialIndex ports_index;
        private ThCADCoreNTSSpatialIndex texts_index;
        private ThCADCoreNTSSpatialIndex ducts_index;
        private ThCADCoreNTSSpatialIndex shapes_index;
        private Duct_modify_param cur_line;
        private ThMEPHVACParam in_param;
        private Polyline cur_duct_geo;
        private ThModifyDuctConnComponent duct_conn_service;
        private bool is_visit_duct_conn_duct;// 只处理一次管道连管道的情况
        public ThDuctPortsModifyDuct(ObjectId[] ids, string modify_size, Duct_modify_param param)
        {
            using (var db = AcadDatabase.Active())
            {
                Read_X_data(param.start_handle, db);
                if (ducts_dic.Count == 0 || ids.Length != 9)
                    return;
                Init(param.start_handle);
                Get_select_line_info(ids[4], out Point2d f_detect, out Point2d l_detect);
                var org_f_detect = f_detect;
                var org_l_detect = l_detect;
                Search_cur_duct(f_detect, l_detect);
                if (cur_line == null)
                    return;
                Modify_entity(modify_size, ref f_detect, out bool f_is_dirct_duct);
                cur_line.sp = f_detect;
                Modify_entity(modify_size, ref l_detect, out bool l_is_dirct_duct);
                var direct_flag = l_is_dirct_duct || f_is_dirct_duct;
                if (!direct_flag)
                    Draw_modify_duct(f_detect, l_detect, modify_size, param.air_volume);
                Update_text(f_detect, l_detect, direct_flag, modify_size);
                duct_conn_service.Update_cur_duct_valve_hole(org_f_detect, f_detect, org_l_detect, l_detect, cur_line.duct_size, modify_size);
            }
        }
        private void Read_X_data(Handle start_handle_, AcadDatabase db)
        {
            var start_id = new ObjectId[] { db.Database.GetObjectId(false, start_handle_, 0) };
            ThDuctPortsInterpreter.Get_basic_param(start_id, out in_param, out start_point);
            ThDuctPortsInterpreter.Get_ports_dic(out ports_dic);
            ThDuctPortsInterpreter.Get_ducts_dic(out ducts_dic);
            ThDuctPortsInterpreter.Get_shapes_dic(out shapes_dic);
            ThDuctPortsInterpreter.Get_texts_dic(out texts_dic);
            ThDuctPortsInterpreter.Get_fan_dic(out fans_dic);
            ThDuctPortsInterpreter.Get_hose_bounds(out hoses_bounds);
        }
        private void Init(Handle start_handle_)
        {
            start_handle = start_handle_;
            is_visit_duct_conn_duct = false;
            tor = new Tolerance(1e-3, 1e-3);
            var start_3d_p = new Point3d(start_point.X, start_point.Y, 0);
            org_dis_vec = start_3d_p.GetAsVector();
            org_dis_mat = Matrix3d.Displacement(org_dis_vec);
            service = new ThDuctPortsDrawService(in_param.scenario, in_param.scale);
            var mat = Matrix2d.Displacement(-start_point.GetAsVector());
            var m = Matrix3d.Displacement(-org_dis_vec);
            Move_duct_to_org(mat, m);
            ducts_index = new ThCADCoreNTSSpatialIndex(ducts_dic.Keys.ToCollection());
            Move_shape_to_org(mat, m);
            shapes_index = new ThCADCoreNTSSpatialIndex(shapes_dic.Keys.ToCollection());
            foreach (Polyline b in texts_dic.Keys.ToCollection())
                b.TransformBy(m);
            texts_index = new ThCADCoreNTSSpatialIndex(texts_dic.Keys.ToCollection());
            Move_port_to_org(mat, m);
            ports_index = new ThCADCoreNTSSpatialIndex(ports_dic.Keys.ToCollection());
            Move_fan_to_org(m);
            fans_index = new ThCADCoreNTSSpatialIndex(fans_dic.Keys.ToCollection());
            Move_hose_to_org(m);
            hoses_index = new ThCADCoreNTSSpatialIndex(hoses_bounds);
            duct_conn_service = new ThModifyDuctConnComponent(start_3d_p);
        }
        private void Move_hose_to_org(Matrix3d m3)
        {
            foreach (Polyline b in hoses_bounds)
                b.TransformBy(m3);
        }
        private void Move_fan_to_org(Matrix3d m3)
        {
            foreach (Polyline b in fans_dic.Keys.ToCollection())
                b.TransformBy(m3);
        }
        private void Move_port_to_org(Matrix2d m2, Matrix3d m3)
        {
            foreach (Polyline b in ports_dic.Keys.ToCollection())
                b.TransformBy(m3);
            foreach (var p in ports_dic.Values)
                p.pos = p.pos.TransformBy(m2);
        }
        private void Move_duct_to_org(Matrix2d m2, Matrix3d m3)
        {
            foreach (var pair in ducts_dic)
            {
                pair.Key.TransformBy(m3);
                pair.Value.sp = pair.Value.sp.TransformBy(m2);
                pair.Value.ep = pair.Value.ep.TransformBy(m2);
            }
        }
        private void Move_shape_to_org(Matrix2d m2, Matrix3d m3)
        {
            foreach (var pair in shapes_dic)
            {
                pair.Key.TransformBy(m3);
                for (int i = 0; i < pair.Value.pos.Count; ++i)
                {
                    pair.Value.pos[i] = pair.Value.pos[i].TransformBy(m2);
                    pair.Value.pos_ext[i] = pair.Value.pos_ext[i].TransformBy(m2);
                }
            }
        }
        private void Search_cur_duct(Point2d f_detect, Point2d l_detect)
        {
            var dir_vec = (l_detect - f_detect).GetNormal();
            var sp2 = f_detect - dir_vec;
            var ep2 = l_detect + dir_vec;
            var sp = new Point3d(sp2.X, sp2.Y, 0);
            var ep = new Point3d(ep2.X, ep2.Y, 0);
            var l = new Line(sp, ep);
            var pl = ThMEPHVACService.Get_line_extend(l, 1);
            var res = ducts_index.SelectCrossingPolygon(pl);
            foreach (Polyline poly in res)
            {
                if (ducts_dic.ContainsKey(poly))
                {
                    cur_duct_geo = poly;
                    cur_line = ducts_dic[poly];
                    if (cur_line.sp.IsEqualTo(f_detect, tor) && cur_line.ep.IsEqualTo(l_detect, tor))
                        return;
                }
            }
            throw new NotImplementedException("Current duct doesn't belong to ducts index");
        }
        private void Draw_modify_duct(Point2d f_detect, Point2d l_detect, string modify_size, double air_volume)
        {
            double cur_duct_width = ThMEPHVACService.Get_width(modify_size);
            var duct = ThDuctPortsFactory.Create_duct(f_detect, l_detect, cur_duct_width);
            var duct_param = ThMEPHVACService.Create_duct_modify_param(duct.center_line, modify_size, air_volume, in_param, start_handle);
            service.Draw_duct(duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                  out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
            Update_port(modify_size);
        }
        private void Update_port(string modify_size)
        {
            if (ports_dic.Count > 0)
            {
                if (in_param.port_range.Contains("侧"))
                {
                    var cross_port = Search_neig_port();
                    Do_update_port(cross_port, modify_size);
                }
            }
        }
        private void Do_update_port(List<Port_modify_param> cross_port, string modify_size)
        {
            var line = ThMEPHVACService.Covert_duct_to_line(cur_line);
            var dir_vec = ThMEPHVACService.Get_edge_direction(line);
            var l_vec = ThMEPHVACService.Get_left_vertical_vec(dir_vec);
            var r_vec = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
            var org_width = ThMEPHVACService.Get_width(cur_line.duct_size);
            var cur_width = ThMEPHVACService.Get_width(modify_size);
            var extend_len = (cur_width - org_width) * 0.5;
            var sp = new Point3d(start_point.X, start_point.Y, 0);
            var mat = Matrix3d.Displacement(sp.GetAsVector());
            foreach (var port in cross_port)
            {
                var p = new Point3d(port.pos.X, port.pos.Y, 0);
                var vec = (p - line.StartPoint).GetNormal();
                ThDuctPortsDrawService.Clear_graph(port.handle);
                double z = dir_vec.CrossProduct(vec).Z;
                if (z < 0)
                {
                    var new_pos = p + r_vec * extend_len;
                    new_pos = new_pos.TransformBy(mat);
                    service.port_service.Insert_port(new_pos, port.rotate_angle, port.port_width, port.port_height, port.port_range);
                }
                else if (z > 0)
                {
                    var new_pos = p + l_vec * extend_len;
                    new_pos = new_pos.TransformBy(mat);
                    service.port_service.Insert_port(new_pos, port.rotate_angle, port.port_width, port.port_height, port.port_range);
                }
                else
                    throw new NotImplementedException("z equals 0");
            }
        }
        private List<Port_modify_param> Search_neig_port()
        {
            var width = ThMEPHVACService.Get_width(cur_line.duct_size);
            var line = ThMEPHVACService.Covert_duct_to_line(cur_line);
            var bounds = ThMEPHVACService.Get_line_extend(line, width);
            var res = ports_index.SelectCrossingPolygon(bounds);
            var port_param = new List<Port_modify_param>();
            foreach (Polyline pl in res)
            {
                if (ports_dic.ContainsKey(pl))
                    port_param.Add(ports_dic[pl]);
            }
            return port_param;
        }
        private void Modify_entity(string modify_duct_width, ref Point2d detect_p, out bool is_direct_duct)
        {
            using (var db = AcadDatabase.Active())
            {
                is_direct_duct = false;
                Search_connected_shape(detect_p, out int port_idx, out Entity_modify_param shape);
                if (shape.handle == ObjectId.Null.Handle)
                {
                    Do_proc_duct_conn_duct(modify_duct_width, detect_p, out Point2d other_p);
                    Search_connected_shape(other_p, out port_idx, out shape);
                    if (shape.type == "")
                        return;
                }
                if (shape.type == "Reducing")
                    Do_proc_reducing(port_idx, modify_duct_width, shape, out is_direct_duct, ref detect_p);
                else if (shape.type == "Elbow")
                    Do_proc_elbow(port_idx, modify_duct_width, shape, ref detect_p);
                else if (shape.type == "Tee")
                    Do_proc_tee(port_idx, modify_duct_width, shape, ref detect_p);
                else if (shape.type == "Cross")
                    Do_proc_cross(port_idx, modify_duct_width, shape, ref detect_p);
                else
                    throw new NotImplementedException();
            }
        }
        private void Do_proc_duct_conn_duct(string modify_duct_size, Point2d detect_p, out Point2d other_p)
        {
            other_p = Point2d.Origin;
            //管子连接管子
            if (is_visit_duct_conn_duct)
                return;
            is_visit_duct_conn_duct = true;
            var res = ducts_index.SelectCrossingPolygon(cur_duct_geo);
            res.Remove(cur_duct_geo);
            if (res.Count == 1)
            {
                var pl = res[0] as Polyline;
                var duct = ducts_dic[pl];
                duct_conn_service.Update_cur_duct_valve_hole(duct.sp, duct.sp, duct.ep, duct.ep, cur_line.duct_size, modify_duct_size);
                other_p = detect_p.IsEqualTo(duct.sp, tor) ? duct.ep : duct.sp;
                var width = ThMEPHVACService.Get_width(modify_duct_size);
                var new_duct = ThDuctPortsFactory.Create_duct(duct.sp, duct.ep, width);
                Update_text(duct.sp, duct.ep, false, modify_duct_size);// 更新机房内管段的duct_size(如果不需要机房内标注置true)
                ThDuctPortsDrawService.Clear_graph(duct.handle);
                service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids,
                          out ObjectIdList center_ids, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, modify_duct_size, duct.air_volume, in_param, start_handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
            }
        }
        private void Do_proc_reducing(int port_idx,
                                      string modify_size,
                                      Entity_modify_param red,
                                      out bool is_direct_duct,
                                      ref Point2d detect_p)
        {
            is_direct_duct = false;
            var p = red.pos[(port_idx + 1) % 2];
            var conn_duct = Search_2_port_neig_duct(p);
            var elbow = Search_reducing_with_elbow(p, "Elbow");
            if (conn_duct.handle == ObjectId.Null.Handle && elbow.handle == ObjectId.Null.Handle)
            {
                // 变径另一端连接风机
                bool is_axis = Is_conn_axis_fan(p, red.pos[port_idx]);
                var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(red, port_idx, modify_size, is_axis);
                Update_reducing(reducing_geo);
                ThDuctPortsDrawService.Clear_graph(red.handle);
                return;
            }
            if (conn_duct.handle != ObjectId.Null.Handle)
                Draw_reducing_connect_duct(conn_duct, modify_size, out is_direct_duct);
            if (!is_direct_duct)
            {
                if (conn_duct.handle == ObjectId.Null.Handle)
                {
                    var other_elbow_port = elbow.pos[0].IsEqualTo(p) ? elbow.pos[1] : elbow.pos[0];
                    conn_duct = Search_2_port_neig_duct(other_elbow_port);
                }
                if (modify_size == conn_duct.duct_size)
                    detect_p = p;
                else
                {
                    var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(red, port_idx, modify_size, false);
                    Update_reducing(reducing_geo);
                }
            }
            ThDuctPortsDrawService.Clear_graph(red.handle);
        }
        private bool Is_conn_axis_fan(Point2d p, Point2d org_p)// p->与风机相接的变径的点 org_p->与风机相接的变径的另一边
        {
            var p3 = new Point3d(p.X, p.Y, 0);
            var org_p3 = new Point3d(org_p.X, org_p.Y, 0);
            var dir_vec = (p3 - org_p3).GetNormal();
            var detect_pl = ThMEPHVACService.Create_detect_poly(p3);
            var res = hoses_index.SelectCrossingPolygon(detect_pl);
            var detect_len = res.Count == 0 ? 10 : 200;
            p3 += (dir_vec * detect_len);
            detect_pl = ThMEPHVACService.Create_detect_poly(p3, detect_len);
            res = fans_index.SelectCrossingPolygon(detect_pl);
            if (res.Count == 1)
            {
                var pl = res[0] as Polyline;
                var param = fans_dic[pl];
                return param.fan_name == "轴流风机";
            }
            else
                throw new NotImplementedException("Reducing cross multi fan");
        }
        private void Do_proc_elbow(int port_idx, string modify_duct_width, Entity_modify_param elbow, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            if (elbow.port_widths[0] > modify_width)
                Proc_elbow_shrink(port_idx, modify_duct_width, elbow, ref detect_p);
            else
            {
                var p = elbow.pos[(port_idx + 1) % 2];
                var e = Search_reducing_with_elbow(p, "Reducing");
                if (e.type != "Reducing")
                    Proc_elbow_conn_duct(modify_duct_width, elbow, ref detect_p);
                else
                    Proc_elbow_extend(elbow, e, modify_duct_width, ref detect_p);
            }
        }
        private void Proc_elbow_extend(Entity_modify_param elbow,
                                       Entity_modify_param reducing,
                                       string modify_duct_width,
                                       ref Point2d detect_p)
        {
            var p = reducing.pos[0].GetDistanceTo(detect_p) > reducing.pos[1].GetDistanceTo(detect_p) ?
                    reducing.pos[0] : reducing.pos[1];
            var conn_duct = Search_2_port_neig_duct(p);
            if (conn_duct.handle == ObjectId.Null.Handle)
                return;
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            if (modify_duct_width == conn_duct.duct_size)
            {
                Create_elbow_by_elbow(false, modify_duct_width, elbow, ref detect_p, out double _, out double _, out Point2d elbow_other_port);
                var dis1 = conn_duct.sp.GetDistanceTo(elbow_other_port);
                var dis2 = conn_duct.ep.GetDistanceTo(elbow_other_port);
                var new_duct = dis1 > dis2 ? ThDuctPortsFactory.Create_duct(conn_duct.sp, elbow_other_port, modify_width) :
                                             ThDuctPortsFactory.Create_duct(elbow_other_port, conn_duct.ep, modify_width);
                service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                         out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, conn_duct.duct_size, conn_duct.air_volume, in_param, start_handle);
                ThDuctPortsDrawService.Clear_graph(conn_duct.handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
                ThDuctPortsDrawService.Clear_graph(reducing.handle);
            }
            else
            {
                Create_elbow_by_elbow(false, modify_duct_width, elbow, ref detect_p, out _, out double _, out Point2d elbow_other_port);
                Create_reducing_by_reducing(reducing, elbow_other_port, p, conn_duct.duct_size, modify_width);
            }
        }
        private void Proc_elbow_shrink(int port_idx, string modify_duct_width, Entity_modify_param elbow, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            Create_elbow_by_elbow(true, modify_duct_width, elbow, ref detect_p,
                                  out double shrink_change_len, out double open_angle, out Point2d _);
            var p = elbow.pos[(port_idx + 1) % 2];
            var conn_duct = Search_2_port_neig_duct(p);
            if (conn_duct.handle != ObjectId.Null.Handle)
                Draw_elbow_connect_duct(shrink_change_len, conn_duct, modify_duct_width, p);
            Modify_reducing_with_elbow(p, modify_width, open_angle, elbow.port_widths[0]);
        }
        private void Create_elbow_by_elbow(bool is_shrink,
                                           string modify_duct_width,
                                           Entity_modify_param elbow,
                                           ref Point2d detect_p,
                                           out double shrink_change_len,
                                           out double open_angle,
                                           out Point2d elbow_other_port)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            var dir_vec = (cur_line.ep - cur_line.sp).GetNormal();
            var dis1 = cur_line.sp.GetDistanceTo(detect_p);
            var dis2 = cur_line.ep.GetDistanceTo(detect_p);
            // 需要改变elbow的宽度
            open_angle = ThDuctPortsShapeService.Get_elbow_open_angle(elbow);
            var elbow_geo = ThDuctPortsFactory.Create_elbow(open_angle, modify_width);
            shrink_change_len = Get_elbow_shrink_change(elbow.port_widths[0], modify_width, open_angle);
            var mat = ThDuctPortsShapeService.Create_elbow_trans_mat(elbow);
            var new_elbow = ThMEPHVACService.Create_special_modify_param("Elbow", mat, start_handle, elbow_geo.flg, elbow_geo.center_line);
            Update_new_shape(elbow_geo, mat, new_elbow);
            var dis_vec = dir_vec * shrink_change_len;
            if (is_shrink)
                detect_p = dis1 < dis2 ? cur_line.sp - dis_vec : cur_line.ep + dis_vec;
            else
                detect_p = dis1 < dis2 ? cur_line.sp + dis_vec : cur_line.ep - dis_vec;
            elbow_other_port = detect_p.IsEqualTo(new_elbow.pos[0]) ? new_elbow.pos[1] : new_elbow.pos[0];
            ThDuctPortsDrawService.Clear_graph(elbow.handle);
        }
        private void Create_reducing_by_reducing(Entity_modify_param reducing,
                                                 Point2d elbow_other_port,
                                                 Point2d reducing_other_port,
                                                 string conn_duct_size,
                                                 double modify_width)
        {
            var conn_width = ThMEPHVACService.Get_width(conn_duct_size);
            var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(reducing_other_port, elbow_other_port, conn_width, modify_width);
            Update_reducing(reducing_geo);
            ThDuctPortsDrawService.Clear_graph(reducing.handle);
        }
        private void Proc_elbow_conn_duct(string modify_duct_width, Entity_modify_param elbow, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            var dir_vec = (cur_line.ep - cur_line.sp).GetNormal();
            var dis_vec = dir_vec * 1000;
            var p = detect_p.IsEqualTo(cur_line.sp, tor) ? cur_line.sp + dis_vec : cur_line.ep - dis_vec;
            // 添加变径
            var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(p, detect_p, modify_width, elbow.port_widths[0]);
            Update_reducing(reducing_geo);
            var conn_duct = Search_2_port_neig_duct(detect_p);
            if (conn_duct.handle != ObjectId.Null.Handle)
            {
                if (!conn_duct.sp.IsEqualTo(cur_line.sp) || !conn_duct.ep.IsEqualTo(cur_line.ep))
                {
                    var other_p = detect_p.IsEqualTo(conn_duct.sp, tor) ? conn_duct.ep : conn_duct.sp;
                    Create_duct_by_duct(conn_duct, other_p, p, modify_width, modify_duct_width);
                }
            }
            detect_p = p;
        }
        private void Do_proc_tee(int port_idx, string modify_duct_size, Entity_modify_param tee, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_size);
            var mat = ThDuctPortsShapeService.Create_tee_trans_mat(tee);
            var tee_geo = ThDuctPortsReDrawFactory.Create_tee(tee, port_idx, modify_width);
            Search_tee_neig_duct(port_idx, tee.pos, out List<Duct_modify_param> connect_duct);
            var new_tee = ThMEPHVACService.Create_special_modify_param("Tee", mat, start_handle, tee_geo.flg, tee_geo.center_line);
            Update_new_shape(tee_geo, mat, new_tee);
            Update_shape_conn_duct(port_idx, new_tee, connect_duct);
            Draw_shape_connect_duct(connect_duct);
            var l = tee_geo.center_line[port_idx] as Line;
            detect_p = l.EndPoint.TransformBy(mat).ToPoint2D();
            ThDuctPortsDrawService.Clear_graph(tee.handle);
        }
        private void Do_proc_cross(int port_idx, string modify_duct_size, Entity_modify_param cross, ref Point2d detect_p)
        {
            var mat = ThDuctPortsShapeService.Create_cross_trans_mat(cross, modify_duct_size, port_idx);
            var cross_geo = ThDuctPortsReDrawFactory.Create_cross(cross, modify_duct_size, port_idx);
            Search_tee_neig_duct(port_idx, cross.pos, out List<Duct_modify_param> connect_duct);
            var new_cross = ThMEPHVACService.Create_special_modify_param("Cross", mat, start_handle, cross_geo.flg, cross_geo.center_line);
            Update_new_shape(cross_geo, mat, new_cross);
            Update_shape_conn_duct(port_idx, new_cross, connect_duct);
            Draw_shape_connect_duct(connect_duct);
            var l = cross_geo.center_line[port_idx] as Line;
            detect_p = l.EndPoint.TransformBy(mat).ToPoint2D();
            ThDuctPortsDrawService.Clear_graph(cross.handle);
        }
        private void Update_shape_conn_duct(int port_idx,
                                            Entity_modify_param new_shape,
                                            List<Duct_modify_param> connect_duct)
        {
            int inc = 0;
            for (int i = 0; i < new_shape.pos.Count; ++i)
            {
                if (i == port_idx)
                    continue;
                var dis1 = connect_duct[inc].sp.GetDistanceTo(new_shape.pos[i]);
                var dis2 = connect_duct[inc].ep.GetDistanceTo(new_shape.pos[i]);
                if (dis1 < dis2)
                    connect_duct[inc++].sp = new_shape.pos[i];
                else
                    connect_duct[inc++].ep = new_shape.pos[i];
            }
        }
        private void Modify_reducing_with_elbow(Point2d p,
                                                double modify_width,
                                                double open_angle,
                                                double elbow_width)
        {
            var reducing = Search_reducing_with_elbow(p, "Reducing");
            if (reducing.handle != ObjectId.Null.Handle)
            {
                var shrink_change_len = Get_elbow_shrink_change(elbow_width, modify_width, open_angle);
                var sp = reducing.pos[0];
                var ep = reducing.pos[1];
                var dir_vec = (ep - sp).GetNormal();
                var dis1 = p.GetDistanceTo(sp);
                var dis2 = p.GetDistanceTo(ep);
                var new_sp = dis1 > dis2 ? sp : sp - dir_vec * shrink_change_len;
                var new_ep = dis1 < dis2 ? ep : ep + dir_vec * shrink_change_len;
                var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(new_sp, new_ep, reducing.port_widths[0], modify_width);
                Update_reducing(reducing_geo);
                ThDuctPortsDrawService.Clear_graph(reducing.handle);
            }
        }
        private Entity_modify_param Search_reducing_with_elbow(Point2d p, string type)
        {
            // 查找变径与弯头连接的连接件
            var p3 = new Point3d(p.X, p.Y, 0);
            var detect_pl = ThMEPHVACService.Create_detect_poly(p3);
            var res = shapes_index.SelectCrossingPolygon(detect_pl);
            if (res.Count != 1 && res.Count != 2)
                return new Entity_modify_param();
            foreach (Polyline pl in res)
            {
                if (shapes_dic.ContainsKey(pl))
                {
                    var param = shapes_dic[pl];
                    if (param.type == type)
                        return param;
                }
            }
            return new Entity_modify_param();
        }
        private double Get_elbow_shrink_change(double org_width, double modify_width, double open_angle)
        {
            var org_shrink = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, org_width, 0, 0.7);
            var cur_shrink = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, modify_width, 0, 0.7);
            return Math.Abs(org_shrink - cur_shrink);
        }
        private void Update_reducing(Line_Info reducing_geo)
        {
            var param = ThMEPHVACService.Create_reducing_modify_param(reducing_geo, start_handle);
            service.Draw_shape(reducing_geo, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                          out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
        }
        private void Update_new_shape(Line_Info geo, Matrix3d mat, Entity_modify_param new_param)
        {
            service.Draw_shape(geo, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                       out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, new_param);
        }
        private Duct_modify_param Search_2_port_neig_duct(Point2d p)
        {
            var p3 = new Point3d(p.X, p.Y, 0);
            var detect_pl = ThMEPHVACService.Create_detect_poly(p3);
            var res = ducts_index.SelectCrossingPolygon(detect_pl);
            if (res.Count != 1)
                return new Duct_modify_param();
            var duct_bounds = res[0] as Polyline;
            return ducts_dic.ContainsKey(duct_bounds) ? ducts_dic[duct_bounds] : new Duct_modify_param();
        }
        private void Search_tee_neig_duct(int cur_port_idx, List<Point2d> tee_port_pos, out List<Duct_modify_param> connect_duct)
        {
            connect_duct = new List<Duct_modify_param>();
            for (int i = 0; i < tee_port_pos.Count; ++i)
            {
                if (i == cur_port_idx)
                    continue;
                var p = tee_port_pos[i];
                var duct = Search_2_port_neig_duct(p);
                if (duct.handle != ObjectId.Null.Handle)
                {
                    connect_duct.Add(duct);
                    ThDuctPortsDrawService.Clear_graph(duct.handle);
                }
            }
        }
        private void Draw_reducing_connect_duct(Duct_modify_param connect_duct, string modify_size, out bool is_direct_duct)
        {
            is_direct_duct = false;
            double width = ThMEPHVACService.Get_width(modify_size);
            if (modify_size == connect_duct.duct_size)
            {
                is_direct_duct = true;
                ThMEPHVACService.Get_longest_dis(cur_line.sp, cur_line.ep, connect_duct.sp, connect_duct.ep, out Point2d p1, out Point2d p2);
                Create_duct_by_duct(connect_duct, p1, p2, width, modify_size);
            }
        }
        private void Create_duct_by_duct(Duct_modify_param connect_line,
                                         Point2d sp,
                                         Point2d ep,
                                         double width,
                                         string modify_size)
        {
            var new_duct = ThDuctPortsFactory.Create_duct(sp, ep, width);
            service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                      out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            var air_volume = Math.Max(connect_line.air_volume, cur_line.air_volume);
            var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, modify_size, air_volume, in_param, start_handle);
            ThDuctPortsDrawService.Clear_graph(connect_line.handle);
            ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
            Update_port(modify_size);
        }
        private void Draw_elbow_connect_duct(double shrink_change_len,
                                             Duct_modify_param conn_duct,
                                             string modify_duct_width,
                                             Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            var connect_width = ThMEPHVACService.Get_width(conn_duct.duct_size);
            var dir_vec = (conn_duct.ep - conn_duct.sp).GetNormal();
            var dis1 = conn_duct.sp.GetDistanceTo(detect_p);
            var dis2 = conn_duct.ep.GetDistanceTo(detect_p);
            var dis_vec = dir_vec * shrink_change_len;
            var new_sp = dis1 > dis2 ? conn_duct.sp : conn_duct.sp + dis_vec;
            var new_ep = dis1 < dis2 ? conn_duct.ep : conn_duct.ep - dis_vec;
            var new_duct = ThDuctPortsFactory.Create_duct(new_sp, new_ep, connect_width);
            service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                     out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, conn_duct.duct_size, conn_duct.air_volume, in_param, start_handle);
            ThDuctPortsDrawService.Clear_graph(conn_duct.handle);
            ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
            Insert_reducing(connect_width, modify_width, new_sp, new_ep, detect_p, dis_vec);
        }
        private void Insert_reducing(double connect_width,
                                     double modify_width,
                                     Point2d new_sp,
                                     Point2d new_ep,
                                     Point2d detect_p,
                                     Vector2d dis_vec)
        {
            if (connect_width > modify_width)
            {
                var dis1 = detect_p.GetDistanceTo(new_sp);
                var dis2 = detect_p.GetDistanceTo(new_ep);
                // dis1 < dis2 风量小的管段比风量大的管段粗(变径反向)
                var reducing_geo = (dis1 < dis2) ?
                    ThDuctPortsReDrawFactory.Create_reducing(detect_p - dis_vec, new_sp, modify_width, connect_width) :
                    ThDuctPortsReDrawFactory.Create_reducing(new_ep, detect_p + dis_vec, connect_width, modify_width);
                Update_reducing(reducing_geo);
            }
        }
        private void Draw_shape_connect_duct(List<Duct_modify_param> connect_duct)
        {
            foreach (var conn_duct in connect_duct)
            {
                var sp = conn_duct.sp;
                var conn_width = ThMEPHVACService.Get_width(conn_duct.duct_size);
                var new_duct = (sp.GetDistanceTo(conn_duct.sp) > sp.GetDistanceTo(conn_duct.ep)) ?
                                ThDuctPortsFactory.Create_duct(sp, conn_duct.sp, conn_width) :
                                ThDuctPortsFactory.Create_duct(sp, conn_duct.ep, conn_width);
                service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                          out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, conn_duct.duct_size, conn_duct.air_volume, in_param, start_handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
                var pl = ThMEPHVACService.Get_line_extend(conn_duct.sp, conn_duct.ep, conn_width);
                duct_conn_service.Update_valve(pl, sp, conn_width);// 多叶调节风阀都是在forward的位置
            }
        }
        private void Update_text(Point2d f_detect,
                                 Point2d l_detect,
                                 bool direct_flag,
                                 string modify_size)
        {
            var f_p = new Point3d(f_detect.X, f_detect.Y, 0);
            var l_p = new Point3d(l_detect.X, l_detect.Y, 0);
            var l = new Line(f_p, l_p);
            double width = ThMEPHVACService.Get_width(cur_line.duct_size) + 1200;//字高300
            var pl = ThMEPHVACService.Get_line_extend(l, width);
            var res = texts_index.SelectCrossingPolygon(pl);
            var duct_dir_vec = (l_detect - f_detect).GetNormal();
            foreach (Polyline text_pl in res)
            {
                if (texts_dic.ContainsKey(text_pl))
                {
                    var cur_text = texts_dic[text_pl];
                    var text_dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(cur_text.rotate_angle);
                    if (ThMEPHVACService.Is_collinear(duct_dir_vec, text_dir_vec))
                    {
                        ThDuctPortsDrawService.Clear_graph(cur_text.handle);
                        if (!direct_flag)
                            service.text_service.Re_draw_text(cur_text, modify_size, in_param);
                    }
                }
            }
        }
        private void Get_select_line_info(ObjectId id, out Point2d detect1, out Point2d detect2)
        {
            Line l;
            using (var db = AcadDatabase.Active())
            {
                l = db.Element<Entity>(id) as Line;
            }
            var sp = l.StartPoint.ToPoint2D();
            var ep = l.EndPoint.ToPoint2D();
            var mat = Matrix2d.Displacement(-start_point.GetAsVector());
            detect1 = sp.TransformBy(mat);
            detect2 = ep.TransformBy(mat);
            ThDuctPortsDrawService.Remove_group_by_comp(id);
        }
        private void Search_connected_shape(Point2d detect_p, out int port_idx, out Entity_modify_param shape)
        {
            port_idx = -1;
            var p = new Point3d(detect_p.X, detect_p.Y, 0);
            var detect_pl = ThMEPHVACService.Create_detect_poly(p);
            var res = shapes_index.SelectCrossingPolygon(detect_pl);
            shape = new Entity_modify_param();
            if (res.Count == 1)
            {
                var pl = res[0] as Polyline;
                shape = shapes_dic[pl];
                for (int j = 0; j < shape.pos.Count; ++j)
                {
                    if (detect_p.IsEqualTo(shape.pos[j], tor))
                    {
                        port_idx = j;
                        break;
                    }
                }
            }
        }
    }
}