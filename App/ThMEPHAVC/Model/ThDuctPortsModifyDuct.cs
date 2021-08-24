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
        private List<Duct_modify_param> ducts;
        private List<Valve_modify_param> valves;
        private Dictionary<Polyline, Valve_modify_param> valves_dic;
        private ThCADCoreNTSSpatialIndex valves_index;
        private List<Entity_modify_param> shapes;
        private List<Text_modify_param> texts;
        private List<Port_modify_param> ports;
        private Dictionary<Polyline, Hole_modify_param> holes_dic;
        private ThCADCoreNTSSpatialIndex holes_index;
        private Duct_modify_param cur_line;
        private ThMEPHVACParam in_param;
        public ThDuctPortsModifyDuct(ObjectId[] ids, string modify_size, Duct_modify_param param)
        {
            using (var db = AcadDatabase.Active())
            {
                Read_X_data(param.start_handle, db);
                if (ducts.Count == 0 || ids.Length != 9)
                    return;
                Init(param.start_handle);
                Get_select_line_info(ids[4], out Point2d f_detect, out Point2d l_detect);
                var org_f_detect = f_detect;
                var org_l_detect = l_detect;
                Search_cur_line(f_detect, l_detect);
                if (cur_line == null)
                    return;
                Modify_entity(modify_size, ref f_detect, out bool f_is_dirct_duct);
                cur_line.sp = f_detect;
                Modify_entity(modify_size, ref l_detect, out bool l_is_dirct_duct);
                var direct_flag = l_is_dirct_duct || f_is_dirct_duct;
                if (!direct_flag)
                    Draw_modify_duct(f_detect, l_detect, modify_size, param.air_volume);
                Update_text(f_detect, l_detect, direct_flag, modify_size);
                Update_cur_duct_valve_hole(org_f_detect, f_detect, org_l_detect, l_detect, modify_size);
            }
        }
        private void Read_X_data(Handle start_handle_, AcadDatabase db)
        {
            var start_id = new ObjectId[] { db.Database.GetObjectId(false, start_handle_, 0) };
            ThDuctPortsInterpreter.Get_basic_param(start_id, out in_param, out start_point);
            ThDuctPortsInterpreter.Get_shapes(out shapes);
            ThDuctPortsInterpreter.Get_ducts(out ducts);
            ThDuctPortsInterpreter.Get_valves(out valves);
            ThDuctPortsInterpreter.Get_texts(out texts);
            ThDuctPortsInterpreter.Get_ports(out ports);
            ThDuctPortsInterpreter.Get_holes_dic(out holes_dic);
            ThDuctPortsInterpreter.Get_valves_dic(out valves_dic);
        }
        private void Init(Handle start_handle_)
        {
            start_handle = start_handle_;
            tor = new Tolerance(1e-3, 1e-3);
            var start_3d_p = new Point3d(start_point.X, start_point.Y, 0);
            org_dis_vec = start_3d_p.GetAsVector();
            org_dis_mat = Matrix3d.Displacement(org_dis_vec);
            service = new ThDuctPortsDrawService(in_param.scenario, in_param.scale);
            var mat = Matrix2d.Displacement(-start_point.GetAsVector());
            Move_duct_to_org(mat);
            Move_shape_to_org(mat);
            Move_valve_to_org(mat);
            Move_text_to_org(mat);
            Move_port_to_org(mat);
            var m = Matrix3d.Displacement(-org_dis_vec);
            foreach (Polyline b in valves_dic.Keys.ToCollection())
                b.TransformBy(m);
            valves_index = new ThCADCoreNTSSpatialIndex(valves_dic.Keys.ToCollection());
            foreach (Polyline b in holes_dic.Keys.ToCollection())
                b.TransformBy(m);
            holes_index = new ThCADCoreNTSSpatialIndex(holes_dic.Keys.ToCollection());
        }
        private void Move_duct_to_org(Matrix2d mat)
        {
            foreach (var duct in ducts)
            {
                duct.sp = duct.sp.TransformBy(mat);
                duct.ep = duct.ep.TransformBy(mat);
            }
        }
        private void Move_shape_to_org(Matrix2d mat)
        {
            foreach (var shape in shapes)
            {
                for (int i = 0; i < shape.pos.Count; ++i)
                {
                    shape.pos[i] = shape.pos[i].TransformBy(mat);
                    shape.pos_ext[i] = shape.pos_ext[i].TransformBy(mat);
                }
            }
        }
        private void Move_valve_to_org(Matrix2d mat)
        {
            foreach (var valve in valves)
            {
                valve.judge_p = valve.judge_p.TransformBy(mat);
                valve.insert_p = valve.insert_p.TransformBy(mat);
            }
        }
        private void Move_text_to_org(Matrix2d mat)
        {
            foreach (var valve in texts)
                valve.center_point = valve.center_point.TransformBy(mat);
        }
        private void Move_port_to_org(Matrix2d mat)
        {
            foreach (var port in ports)
                port.pos = port.pos.TransformBy(mat);
        }
        private void Search_cur_line(Point2d f_detect, Point2d l_detect)
        {
            for (int i = 0; i < ducts.Count; ++i)
            {
                var duct = ducts[i];
                if (ThMEPHVACService.Is_same_line(f_detect, l_detect, duct.sp, duct.ep, tor))
                {
                    cur_line = ducts[i];
                    break;
                }
            }
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
            if (ports.Count > 0)
            {
                if (in_param.port_range.Contains("侧"))
                {
                    var neig_port_idx = Search_neig_port();
                    Do_update_port(neig_port_idx, modify_size);
                }
            }
        }
        private void Do_update_port(List<int> neig_port_idx, string modify_size)
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
            for (int i = 0; i < neig_port_idx.Count; ++i)
            {
                var port = ports[neig_port_idx[i]];
                var p = new Point3d(port.pos.X, port.pos.Y, 0);
                var vec = p - line.StartPoint;
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
                    throw new NotImplementedException();
            }
        }
        private List<int> Search_neig_port()
        {
            var width = ThMEPHVACService.Get_width(cur_line.duct_size);
            var line = ThMEPHVACService.Covert_duct_to_line(cur_line);
            var neig_port = new List<int>();
            for (int i = 0; i < ports.Count; ++i)
            {
                var port = ports[i];
                var p = new Point3d(port.pos.X, port.pos.Y, 0);
                var dis = line.GetClosestPointTo(p, false).DistanceTo(p);
                if (Math.Abs(dis - 0.5 * width) < 1e-3)
                    neig_port.Add(i);
            }
            return neig_port;
        }
        private void Modify_entity(string modify_duct_width, ref Point2d detect_p, out bool is_direct_duct)
        {
            using (var db = AcadDatabase.Active())
            {
                is_direct_duct = false;
                Search_connected_shape(detect_p, out int shape_idx, out int port_idx, out Entity_modify_param shape);
                if (shape_idx == -1 || shape.handle == ObjectId.Null.Handle)
                    return;
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
        private void Do_proc_reducing(int port_idx,
                                      string modify_size,
                                      Entity_modify_param red,
                                      out bool is_direct_duct,
                                      ref Point2d detect_p)
        {
            is_direct_duct = false;
            var p = red.pos[(port_idx + 1) % 2];
            Search_2_port_neig_duct(p, out int connect_duct_idx);
            var elbow = Search_reducing_conn_elbow(p);
            if (connect_duct_idx == -1 && elbow.handle == ObjectId.Null.Handle)
                return;
            if (connect_duct_idx >= 0)
                Draw_reducing_connect_duct(connect_duct_idx, modify_size, out is_direct_duct);
            if (!is_direct_duct)
            {
                if (connect_duct_idx < 0)
                {
                    var other_elbow_port = elbow.pos[0].IsEqualTo(p) ? elbow.pos[1] : elbow.pos[0];
                    Search_2_port_neig_duct(other_elbow_port, out connect_duct_idx);
                }
                if (modify_size == ducts[connect_duct_idx].duct_size)
                {
                    detect_p = p;
                }
                else
                {
                    var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(red, port_idx, modify_size);
                    Update_reducing(reducing_geo);
                }
            }
            ThDuctPortsDrawService.Clear_graph(red.handle);
        }
        private void Do_proc_elbow(int port_idx, string modify_duct_width, Entity_modify_param elbow, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            if (elbow.port_widths[0] > modify_width)
                Proc_elbow_shrink(port_idx, modify_duct_width, elbow, ref detect_p);
            else
            {
                var e = Search_elbow_conn_reducing(port_idx, elbow);
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
            Search_2_port_neig_duct(p, out int connect_duct_idx);
            if (connect_duct_idx < 0)
                return;
            var duct = ducts[connect_duct_idx];
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            if (modify_duct_width == duct.duct_size)
            {
                Create_elbow_by_elbow(false, modify_duct_width, elbow, ref detect_p,  out double _, out double _, out Point2d elbow_other_port);
                var dis1 = duct.sp.GetDistanceTo(elbow_other_port);
                var dis2 = duct.ep.GetDistanceTo(elbow_other_port);
                var new_duct = dis1 > dis2 ? ThDuctPortsFactory.Create_duct(duct.sp, elbow_other_port, modify_width) :
                                             ThDuctPortsFactory.Create_duct(elbow_other_port, duct.ep, modify_width);
                service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                         out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, duct.duct_size, duct.air_volume, in_param, start_handle);
                ThDuctPortsDrawService.Clear_graph(duct.handle);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
                ThDuctPortsDrawService.Clear_graph(reducing.handle);
            }
            else
            {
                Create_elbow_by_elbow(false, modify_duct_width, elbow, ref detect_p, out _, out double _, out Point2d elbow_other_port);
                Create_reducing_by_reducing(reducing, elbow_other_port, p, duct.duct_size, modify_width);
            }
        }
        private void Proc_elbow_shrink(int port_idx, string modify_duct_width, Entity_modify_param elbow, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            Create_elbow_by_elbow(true, modify_duct_width, elbow, ref detect_p, 
                                  out double shrink_change_len, out double open_angle, out Point2d _);
            var p = elbow.pos[(port_idx + 1) % 2];
            Search_2_port_neig_duct(p, out int connect_duct_idx);            
            if (connect_duct_idx >= 0)
                Draw_elbow_connect_duct(shrink_change_len, connect_duct_idx, modify_duct_width, p);
            Modify_reducing_with_elbow(p, modify_width, open_angle, elbow);
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
            elbow_other_port = detect_p.IsEqualTo(new_elbow.pos[0]) ? new_elbow.pos[1] : new_elbow.pos[0]; ;
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
            Search_2_port_neig_duct(detect_p, out int conn_duct_idx);
            if (conn_duct_idx > 0)
            {
                var duct = ducts[conn_duct_idx];
                if (!duct.sp.IsEqualTo(cur_line.sp) || !duct.ep.IsEqualTo(cur_line.ep))
                {
                    var other_p = detect_p.IsEqualTo(duct.sp, tor) ? duct.ep : duct.sp;
                    Create_duct_by_duct(duct, conn_duct_idx, other_p, p, modify_width, modify_duct_width);
                }
            }
            detect_p = p;
        }
        private Entity_modify_param Search_elbow_conn_reducing(int port_idx, Entity_modify_param elbow)
        {
            var p = elbow.pos[(port_idx + 1) % 2];
            foreach (var e in shapes)
            {
                if (e.type == "Reducing")
                {
                    foreach (var pos in e.pos)
                    {
                        if (pos.IsEqualTo(p))
                            return e;
                    }
                }
            }
            return new Entity_modify_param();
        }
        private void Do_proc_tee(int port_idx, string modify_duct_size, Entity_modify_param tee, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_size);
            var mat = ThDuctPortsShapeService.Create_tee_trans_mat(tee);
            var tee_geo = ThDuctPortsReDrawFactory.Create_tee(tee, port_idx, modify_width);
            Search_tee_neig_duct(port_idx, tee.pos, out List<int> connect_duct_idx);
            Draw_shape_connect_duct(port_idx, tee_geo.center_line, mat, connect_duct_idx, tee.pos);
            var new_tee = ThMEPHVACService.Create_special_modify_param("Tee", mat, start_handle, tee_geo.flg, tee_geo.center_line);
            Update_new_shape(tee_geo, mat, new_tee);
            var l = tee_geo.center_line[port_idx] as Line;
            detect_p = l.EndPoint.TransformBy(mat).ToPoint2D();
            ThDuctPortsDrawService.Clear_graph(tee.handle);
        }
        private void Do_proc_cross(int port_idx, string modify_duct_size, Entity_modify_param cross, ref Point2d detect_p)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_size);
            var mat = ThDuctPortsShapeService.Create_cross_trans_mat(cross);
            var cross_geo = ThDuctPortsReDrawFactory.Create_cross(cross, port_idx, modify_width);
            Search_tee_neig_duct(port_idx, cross.pos, out List<int> connect_duct_idx);
            Draw_shape_connect_duct(port_idx, cross_geo.center_line, mat, connect_duct_idx, cross.pos);
            var new_param = ThMEPHVACService.Create_special_modify_param("Cross", mat, start_handle, cross_geo.flg, cross_geo.center_line);
            Update_new_shape(cross_geo, mat, new_param);
            var l = cross_geo.center_line[port_idx] as Line;
            detect_p = l.EndPoint.TransformBy(mat).ToPoint2D();
            ThDuctPortsDrawService.Clear_graph(cross.handle);
        }
        private void Modify_reducing_with_elbow(Point2d p, 
                                                double modify_width,
                                                double open_angle,
                                                Entity_modify_param elbow)
        {
            var e = Search_reducing_conn_elbow(p);
            if (e.handle != ObjectId.Null.Handle)
            {
                var shrink_change_len = Get_elbow_shrink_change(elbow.port_widths[0], modify_width, open_angle);
                var sp = e.pos[0];
                var ep = e.pos[1];
                var dir_vec = (ep - sp).GetNormal();
                var dis1 = p.GetDistanceTo(sp);
                var dis2 = p.GetDistanceTo(ep);
                var new_sp = dis1 > dis2 ? sp : sp - dir_vec * shrink_change_len;
                var new_ep = dis1 < dis2 ? ep : ep + dir_vec * shrink_change_len;
                var reducing_geo = ThDuctPortsReDrawFactory.Create_reducing(new_sp, new_ep, e.port_widths[0], modify_width);
                Update_reducing(reducing_geo);
                ThDuctPortsDrawService.Clear_graph(e.handle);
            }
        }
        private Entity_modify_param Search_reducing_conn_elbow(Point2d p)
        {
            foreach (var e in shapes)
            {
                foreach (var pos in e.pos)
                    if (p.IsEqualTo(pos, tor))
                        return e;
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
            shapes.Add(param);
        }
        private void Update_new_shape(Line_Info geo, Matrix3d mat, Entity_modify_param new_param)
        {
            service.Draw_shape(geo, org_dis_mat * mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                       out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, new_param);
            shapes.Add(new_param);
        }
        private void Search_2_port_neig_duct(Point2d p, out int connect_duct_idx)
        {
            connect_duct_idx = -1;
            for (int i = 0; i < ducts.Count; ++i)
            {
                var duct = ducts[i];
                if (p.IsEqualTo(duct.sp, tor) || p.IsEqualTo(duct.ep, tor))
                    connect_duct_idx = i;
            }
        }
        private void Search_tee_neig_duct(int cur_port_idx, List<Point2d> shape_pos, out List<int> connect_duct_idx)
        {
            connect_duct_idx = new List<int>();
            for (int i = 0; i < shape_pos.Count; ++i)
            {
                if (i == cur_port_idx)
                    continue;
                var p = shape_pos[i];
                for (int j = 0; j < ducts.Count; ++j)
                {
                    var duct = ducts[j];
                    if (p.IsEqualTo(duct.sp, tor) || p.IsEqualTo(duct.ep, tor))
                    {
                        connect_duct_idx.Add(j);
                        ThDuctPortsDrawService.Clear_graph(duct.handle);
                    }
                }
            }
        }
        private void Draw_reducing_connect_duct(int connect_duct_idx, string modify_size, out bool is_direct_duct)
        {
            is_direct_duct = false;
            var connect_line = ducts[connect_duct_idx];
            double width = ThMEPHVACService.Get_width(modify_size);
            if (modify_size == connect_line.duct_size)
            {
                is_direct_duct = true;
                ThMEPHVACService.Get_max(cur_line.sp, cur_line.ep, connect_line.sp, connect_line.ep, out Point2d p1, out Point2d p2);
                Create_duct_by_duct(connect_line, connect_duct_idx, p1, p2, width, modify_size);
            }
        }
        private void Create_duct_by_duct(Duct_modify_param connect_line, 
                                         int connect_duct_idx,
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
            ducts.RemoveAt(connect_duct_idx);
            ducts.Add(duct_param);
        }
        private void Draw_elbow_connect_duct(double shrink_change_len, 
                                             int connect_duct_idx, 
                                             string modify_duct_width, 
                                             Point2d detect_p)
        {
            var connect_line = ducts[connect_duct_idx];
            var modify_width = ThMEPHVACService.Get_width(modify_duct_width);
            var connect_width = ThMEPHVACService.Get_width(connect_line.duct_size);
            var dir_vec = (connect_line.ep - connect_line.sp).GetNormal();
            var dis1 = connect_line.sp.GetDistanceTo(detect_p);
            var dis2 = connect_line.ep.GetDistanceTo(detect_p);
            var dis_vec = dir_vec * shrink_change_len;
            var new_sp = dis1 > dis2 ? connect_line.sp : connect_line.sp + dis_vec;
            var new_ep = dis1 < dis2 ? connect_line.ep : connect_line.ep - dis_vec;
            var new_duct = ThDuctPortsFactory.Create_duct(new_sp, new_ep, connect_width);
            service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                     out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
            var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, connect_line.duct_size, connect_line.air_volume, in_param, start_handle);
            ThDuctPortsDrawService.Clear_graph(connect_line.handle);
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
                var reducing_geo = (dis1 < dis2) ?
                    ThDuctPortsReDrawFactory.Create_reducing(detect_p - dis_vec, new_sp, connect_width, modify_width) :
                    ThDuctPortsReDrawFactory.Create_reducing(new_ep, detect_p + dis_vec, connect_width, modify_width);
                Update_reducing(reducing_geo);
            }
        }
        private void Draw_shape_connect_duct(int cur_port_idx, 
                                             DBObjectCollection center, 
                                             Matrix3d mat, 
                                             List<int> connect_duct_idx,
                                             List<Point2d> org_detect_p)
        {
            int inc = 0;
            if (connect_duct_idx.Count + 1 != center.Count)
                return;
            for (int i = 0; i < center.Count; ++i)
            {
                var l = center[i].Clone() as Line;
                l.TransformBy(mat);
                if (i == cur_port_idx)
                    continue;
                var connect_line = ducts[connect_duct_idx[inc++]];
                var connect_width = ThMEPHVACService.Get_width(connect_line.duct_size);
                var sp = l.EndPoint.ToPoint2D();
                var new_duct = (sp.GetDistanceTo(connect_line.sp) > sp.GetDistanceTo(connect_line.ep)) ?
                                ThDuctPortsFactory.Create_duct(sp, connect_line.sp, connect_width) :
                                ThDuctPortsFactory.Create_duct(sp, connect_line.ep, connect_width);
                service.Draw_duct(new_duct, org_dis_mat, out ObjectIdList geo_ids, out ObjectIdList flg_ids, out ObjectIdList center_ids,
                                                          out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                var duct_param = ThMEPHVACService.Create_duct_modify_param(new_duct.center_line, connect_line.duct_size, connect_line.air_volume, in_param, start_handle);
                ducts.Add(duct_param);
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
                Update_air_valve(org_detect_p[i], sp, connect_width);
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
            bool is_down_port = Get_port_range();
            for (int i = 0; i < texts.Count; ++i)
            {
                var cur_t = texts[i];
                var p = new Point3d(cur_t.center_point.X, cur_t.center_point.Y, 0);
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                double width = ThMEPHVACService.Get_width(cur_line.duct_size);
                var bounds = ThMEPHVACService.Get_line_extend(l, width + 1000);
                var is_in_rect = ThMEPHVACService.Is_in_polyline(p, bounds);
                //down port字高500 side port距线的距离应该为0
                if (is_in_rect && ((is_down_port && dis < (width + 501)) || (!is_down_port && dis < 50)))
                {
                    ThDuctPortsDrawService.Clear_graph(cur_t.handle);
                    if (!direct_flag)
                        service.text_service.Re_draw_text(cur_t, modify_size, in_param);
                }
            }
        }
        private bool Get_port_range()
        {
            if (ports.Count > 0)
            {
                return (ports[0].port_range.Contains("下"));
            }
            return true;//风机房内无风口的风管标注在侧边，与downport相同
        }
        private void Update_cur_duct_valve_hole(Point2d org_f_detect, 
                                                Point2d f_detect, 
                                                Point2d org_l_detect, 
                                                Point2d l_detect,
                                                string modify_duct_width)
        {
            double new_width = ThMEPHVACService.Get_width(modify_duct_width);
            var pl = ThMEPHVACService.Get_line_extend(org_f_detect, org_l_detect, 1);
            Update_valve(pl, f_detect, new_width);// 多叶调节风阀都是在forward的位置
            Update_hole(pl, new_width);
        }
        private void Update_valve(Polyline detect_pl, Point2d new_air_valve_pos, double new_width)
        {
            var res = valves_index.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (valves_dic.ContainsKey(p))
                {
                    var param = valves_dic[p];
                    Do_update_valve(new_width, new_air_valve_pos, param);
                }
            }
        }
        private void Update_hole(Polyline detect_pl, double new_width)
        {
            var res = holes_index.SelectCrossingPolygon(detect_pl);
            foreach (Polyline p in res)
            {
                if (holes_dic.ContainsKey(p))
                {
                    var param = holes_dic[p];
                    Do_update_hole(new_width, param);
                }
            }
        }
        private void Do_update_hole(double new_width, Hole_modify_param hole)
        {
            //洞和阀应该分开
            var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(hole.rotate_angle - Math.PI * 0.5);
            var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
            var hole_service = new ThDuctPortsDrawValve("", hole.hole_name, hole.hole_layer);
            var insert_p = hole.insert_p + vertical_r * (hole.width - new_width) * 0.5;
            hole_service.Insert_hole(insert_p, new_width, hole.len, hole.rotate_angle);
            ThDuctPortsDrawService.Clear_graph(hole.handle);
        }
        private void Do_update_valve(double new_width, Point2d new_p, Valve_modify_param valve)
        {
            if (valve.valve_visibility == "多叶调节风阀")
            {
                var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = new_p + vertical_r * new_width * 0.5 + start_point.GetAsVector();
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
            if (valve.valve_visibility == "电动多叶调节风阀")
            {
                var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = valve.insert_p + vertical_r * (valve.width - new_width) * 0.5;
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
            if (valve.valve_visibility == "风管止回阀")
            {
                var dir_vec = -ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = valve.insert_p + vertical_r * (valve.width - new_width) * 0.5;
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
            if (valve.valve_visibility == "70度防火阀（反馈）FDS")
            {
                var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                var vertical_l = ThMEPHVACService.Get_left_vertical_vec(dir_vec);
                var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                var insert_p = valve.insert_p + vertical_l * (valve.width - new_width) * 0.5;
                valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
        }
        private void Update_air_valve(Point2d detect_p, Point2d new_p, double new_width)
        {
            foreach (var valve in valves)
            {
                if (valve.valve_visibility == "多叶调节风阀" && valve.judge_p.IsEqualTo(detect_p, tor))
                {
                    var dir_vec = ThMEPHVACService.Get_dir_vec_by_angle(valve.rotate_angle - Math.PI * 0.5);
                    var vertical_r = ThMEPHVACService.Get_right_vertical_vec(dir_vec);
                    var valve_service = new ThDuctPortsDrawValve(valve.valve_visibility, valve.valve_name, valve.valve_layer);
                    var insert_p = new_p + vertical_r * new_width * 0.5 + start_point.GetAsVector();
                    valve_service.Insert_valve(insert_p, new_width, valve.rotate_angle, valve.text_angle);
                    ThDuctPortsDrawService.Clear_graph(valve.handle);
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
        private void Search_connected_shape(Point2d detect_p, out int shape_idx, out int port_idx, out Entity_modify_param shape)
        {
            port_idx = -1;
            shape_idx = -1;
            shape = new Entity_modify_param();
            for (int i = 0; i < shapes.Count; ++i)
            {
                var e = shapes[i];
                for (int j = 0; j < e.pos.Count; ++j)
                {
                    if (detect_p.IsEqualTo(e.pos[j], tor))
                    {
                        shape_idx = i;
                        port_idx = j;
                        shape = shapes[shape_idx];
                        shapes.RemoveAt(shape_idx);
                        break;
                    }
                }
                if (port_idx != -1)
                    break;
            }
        }
    }
}