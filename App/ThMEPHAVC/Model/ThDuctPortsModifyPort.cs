using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPHVAC.Model
{
    public class Duct_Info
    {
        public Point3d sp;
        public Point3d ep;
        public int port_num;
        public double air_volume;
        public List<Point3d> port_pos;
        public Duct_Info(Point3d sp, Point3d ep, int port_num, double air_volume, List<Point3d> port_pos)
        {
            this.sp = sp;
            this.ep = ep;
            this.port_num = port_num;
            this.port_pos = port_pos;
            this.air_volume = air_volume;
        }
    }
    public class ThDuctPortsModifyPort
    {
        public bool is_success;
        public List<Duct_Info> ducts;
        public DBObjectCollection center_line;
        public DBObjectCollection exclude_line;
        public double avg_air_volume;
        public Point3d start_p;
        private Point3d sp;
        private Point3d ep;
        private Matrix3d dis_mat;
        private Tolerance tor;
        private double port_width;
        private double port_height;
        private string port_range;
        private DuctPortsParam in_param;
        private HashSet<Polyline> poly_set;
        private HashSet<Handle> handles;
        private ThCADCoreNTSSpatialIndex port_index;
        private ThCADCoreNTSSpatialIndex group_index;
        private ThCADCoreNTSSpatialIndex mp_group_index;
        private Dictionary<Polyline, ObjectId> bounds_2_id_dic;
        public ThDuctPortsModifyPort(ObjectId[] start_id, DuctPortsParam in_param)
        {
            using (var db = AcadDatabase.Active())
            {
                ThDuctPortsInterpreter.Get_basic_param(start_id, out _, out Point2d p);
                Init(p, in_param);
                bounds_2_id_dic = ThDuctPortsReadComponent.Read_all_component();
                var group_bounds = bounds_2_id_dic.Keys.ToCollection();
                Move_bounds_to_org(group_bounds, start_p);
                var conn_handles = Get_conn_comp_index(group_bounds);
                var all_port_blk = ThDuctPortsReadComponent.Read_all_port_by_name("风口-AI研究中心");
                is_success = Get_port_info(all_port_blk);
                if (!is_success)
                    return;
                Get_conn_port(all_port_blk, out List<Handle> cross_port_handle);
                int port_num = Count_port(Point3d.Origin);
                avg_air_volume = in_param.air_volume / port_num;
                Prepare_for_search(out Polyline detect_poly, out Point3d detect_p);
                Count_duct_air_volume(detect_poly, detect_p, out _);
                Delete_org_graph(conn_handles, cross_port_handle, start_id);
            }
        }
        private void Init(Point2d p, DuctPortsParam in_param)
        {
            start_p = new Point3d(p.X, p.Y, 0);
            dis_mat = Matrix3d.Displacement(-start_p.GetAsVector());
            tor = new Tolerance(1.1, 1.1);
            poly_set = new HashSet<Polyline>();
            center_line = new DBObjectCollection();
            exclude_line = new DBObjectCollection();
            ducts = new List<Duct_Info>();
            handles = new HashSet<Handle>();
            this.in_param = in_param;
        }
        private void Prepare_for_search(out Polyline detect_poly, out Point3d detect_p)
        {
            detect_poly = Create_detect_poly(Point3d.Origin);
            detect_p = Point3d.Origin;
            poly_set.Clear();
        }
        private List<Handle> Get_conn_comp_index(DBObjectCollection group_bounds)
        {
            var handles = new List<Handle>();
            group_index = new ThCADCoreNTSSpatialIndex(group_bounds);
            Search_conn_comp(Point3d.Origin);
            var objs = new DBObjectCollection();
            var mp_objs = new DBObjectCollection();
            foreach (var pl in poly_set)
            {
                objs.Add(pl);
                handles.Add(bounds_2_id_dic[pl].Handle);
                mp_objs.Add(pl.ToNTSPolygon().ToDbMPolygon());
            }
            group_index = new ThCADCoreNTSSpatialIndex(objs);
            mp_group_index = new ThCADCoreNTSSpatialIndex(mp_objs);
            poly_set.Clear();
            return handles;
        }
        private void Get_conn_port(List<BlockReference> all_port_blk, out List<Handle> port_handles)
        {
            var cross_port = new List<Polyline>();
            port_handles = new List<Handle>();
            var port_2_id_dic = Create_port_bounds(all_port_blk);
            Move_ports_to_org(port_2_id_dic.Keys.ToList(), start_p);
            foreach (var port in port_2_id_dic.Keys)
            {
                var res = mp_group_index.SelectCrossingPolygon(port);
                if (res.Count > 0)
                {
                    port_handles.AddRange(port_2_id_dic[port]);
                    cross_port.Add(port);
                }
            }
            port_index = new ThCADCoreNTSSpatialIndex(cross_port.ToCollection());
        }
        private Dictionary<Polyline, List<Handle>> Create_port_bounds(List<BlockReference> all_port_blk)
        {
            if (port_range.Contains("下"))
            {
                return Create_down_port_bounds(all_port_blk);
            }
            else if (port_range.Contains("侧"))
            {
                return Create_side_port_bounds(all_port_blk);
            }
            else
                throw new NotImplementedException();
        }
        private Dictionary<Polyline, List<Handle>> Create_down_port_bounds(List<BlockReference> all_port_blk)
        {
            var port_bounds = new Dictionary<Polyline, List<Handle>>();
            var list = new List<Handle>();
            foreach (var port in all_port_blk)
            {
                var pl = new Polyline();
                var dir_vec = ThDuctPortsService.Get_dir_vec(port.Rotation);
                var r_vec = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
                var dis_vec = port_height * r_vec + port_width * dir_vec;
                pl.CreateRectangle(port.Position.ToPoint2D(), (port.Position.ToPoint2D() + dis_vec));
                list.Add(port.Handle);
                port_bounds.Add(pl, list);
                list = new List<Handle>();
            }
            return port_bounds;
        }
        private Dictionary<Polyline, List<Handle>> Create_side_port_bounds(List<BlockReference> cross_port)
        {
            var port_bounds = new Dictionary<Polyline, List<Handle>>();
            var list = new List<Handle>();
            while (cross_port.Count > 0)
            {
                var port = cross_port[0];
                cross_port.RemoveAt(0);
                var min_dis = Double.MaxValue;
                var cloest_port = port;
                foreach (var blk in cross_port)
                {
                    var dis = blk.Position.DistanceTo(port.Position);
                    if (dis < min_dis)
                    {
                        min_dis = dis;
                        cloest_port = blk;
                    }
                }
                cross_port.Remove(cloest_port);
                var pl = Get_side_port_center_polyline(port.Position, cloest_port.Position);
                list.Add(port.Handle); list.Add(cloest_port.Handle);
                port_bounds.Add(pl, list);
                list = new List<Handle>();
            }
            return port_bounds;
        }
        private Polyline Get_side_port_center_polyline(Point3d p1, Point3d p2)
        {
            var pl = new Polyline();
            pl.CreateRectangle(p1.ToPoint2D(), p2.ToPoint2D());
            return Create_detect_poly(pl.GetCentroidPoint());
        }
        private bool Get_port_info(List<BlockReference> port_blks)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var blk in port_blks)
                {
                    var id = db.Database.HandleToObjectId(blk.Handle.ToString());
                    var param = ThDuctPortsInterpreter.Get_port_param(id);
                    port_width = param.port_width;
                    port_range = param.port_range;
                    port_height = param.port_height;
                    break;
                }
                foreach (var blk in port_blks)
                {
                    var id = db.Database.HandleToObjectId(blk.Handle.ToString());
                    var param = ThDuctPortsInterpreter.Get_port_param(id);
                    if (!ThDuctPortsService.Is_equal(port_width, param.port_width) ||
                        port_range != param.port_range ||
                        !ThDuctPortsService.Is_equal(port_height, param.port_height))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        private void Delete_org_graph(List<Handle> conn_group_handle,
                                      List<Handle> cross_port_handle,
                                      ObjectId[] start_id)
        {
            foreach (var handle in conn_group_handle)
                ThDuctPortsDrawService.Clear_graph(handle);
            foreach (var port_handle in cross_port_handle)
                ThDuctPortsDrawService.Clear_graph(port_handle);
            foreach (var id in start_id)
                ThDuctPortsDrawService.Clear_graph(id.Handle);
            Delete_text_dim_valve();
        }
        private void Delete_text_dim_valve()
        {
            ThDuctPortsInterpreter.Get_valves(out List<Valve_modify_param> valves);
            ThDuctPortsInterpreter.Get_texts(out List<Text_modify_param> texts);
            var port_mark = ThDuctPortsReadComponent.Read_blk_by_name("风口标注");
            var dims = ThDuctPortsReadComponent.Read_dimension();
            var leaders = ThDuctPortsReadComponent.Read_leader();
            var dis_mat = Matrix3d.Displacement(start_p.GetAsVector());
            foreach (Line line in center_line)
            {
                var l = line.Clone() as Line;
                l.TransformBy(dis_mat);
                Delete_text(texts, l);
                Delete_dim(dims, l);
                Delete_valve(valves, l);
                Delete_port_mark(port_mark, l);
                Delete_port_leader(leaders, l);
            }
        }
        private void Delete_text(List<Text_modify_param> texts, Line l)
        {
            foreach (var cur_t in texts)
            {
                var p = new Point3d(cur_t.center_point.X, cur_t.center_point.Y, 0);
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < 3000 && handles.Add(cur_t.handle))
                {
                    ThDuctPortsDrawService.Clear_graph(cur_t.handle);
                }
            }
        }
        private void Delete_dim(List<AlignedDimension> dims, Line l)
        {
            foreach (var dim in dims)
            {
                var p = dim.Bounds.Value.CenterPoint();
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < 2000 && handles.Add(dim.Handle))
                {
                    ThDuctPortsDrawService.Clear_graph(dim.Handle);
                }
            }
        }
        private void Delete_valve(List<Valve_modify_param> valves, Line l)
        {
            foreach (var valve in valves)
            {
                var p = new Point3d(valve.judge_p.X, valve.judge_p.Y, 0);
                double dis = l.GetClosestPointTo(p, false).DistanceTo(p);
                if (dis < 20 && handles.Add(valve.handle))
                    ThDuctPortsDrawService.Clear_graph(valve.handle);
            }
        }
        private void Delete_port_mark(List<BlockReference> port_mark, Line l)
        {
            foreach (var mark in port_mark)
            {
                double dis = l.GetClosestPointTo(mark.Position, false).DistanceTo(mark.Position);
                if (dis < 2501 && handles.Add(mark.ObjectId.Handle)) // 2500^2 = 1500^2 + 2000^2
                    ThDuctPortsDrawService.Clear_graph(mark.ObjectId.Handle);
            }
        }
        private void Delete_port_leader(List<Leader> leaders, Line l)
        {
            foreach (var leader in leaders)
            {
                double dis = l.GetClosestPointTo(leader.StartPoint, false).DistanceTo(leader.StartPoint);
                if (dis < 2501 && handles.Add(leader.ObjectId.Handle))
                    ThDuctPortsDrawService.Clear_graph(leader.ObjectId.Handle);
            }
        }
        private void Move_ports_to_org(List<Polyline> port_bounds, Point3d align_p)
        {
            var dis_mat = Matrix3d.Displacement(-align_p.GetAsVector());
            foreach (var port in port_bounds)
            {
                port.TransformBy(dis_mat);
            }
        }
        private void Move_bounds_to_org(DBObjectCollection group_bounds, Point3d align_p)
        {
            var dis_mat = Matrix3d.Displacement(-align_p.GetAsVector());
            foreach (Polyline pl in group_bounds)
            {
                pl.TransformBy(dis_mat);
            }
        }
        private List<Point3d> Move_pts_to_org(List<Point3d> port_pts)
        {
            var pts = new List<Point3d>();
            var dis_mat = Matrix3d.Displacement(-start_p.GetAsVector());
            foreach (var p in port_pts)
                pts.Add(p.TransformBy(dis_mat));
            return pts;
        }
        private double Count_duct_air_volume(Polyline cur_poly, Point3d detect_p, out Polyline pre_poly)
        {
            double sub_air_volume = 0;
            var res = Detect_cross_group(detect_p);
            pre_poly = cur_poly;
            if (res.Count == 1 && poly_set.Count != 0)
            {
                ep = detect_p;
                return port_index.SelectCrossingPolygon(cur_poly).Count * avg_air_volume;
            }
            double air_volume = 0;
            res.Remove(cur_poly);
            foreach (Polyline pl in res)
            {
                if (!poly_set.Add(pl))
                    continue;
                var port_pts = Get_step_point(pl, detect_p);
                foreach (var p in port_pts)
                {
                    sub_air_volume += Count_duct_air_volume(pl, p, out pre_poly);
                    double cur_air_volume = port_index.SelectCrossingPolygon(cur_poly).Count * avg_air_volume;
                    Record_comp(cur_poly, sub_air_volume);
                    air_volume = sub_air_volume + cur_air_volume;
                }
            }
            return air_volume;
        }
        private void Record_comp(Polyline cur_poly, double air_volume)
        {
            using (var db = AcadDatabase.Active())
            {
                if (bounds_2_id_dic.Keys.Contains(cur_poly))
                {
                    var cur_entity = ThDuctPortsInterpreter.Get_shape_by_id(bounds_2_id_dic[cur_poly]);
                    if (cur_entity.type == "Elbow" || cur_entity.type == "Tee" || cur_entity.type == "Cross")
                    {
                        var center_p = ThDuctPortsShapeService.Get_entity_center_p(cur_entity);
                        sp = new Point3d(center_p.X, center_p.Y, 0);
                        sp = sp.TransformBy(dis_mat);
                        var l = new Line(sp, ep);
                        double width = cur_entity.port_widths.Max();
                        int port_num = Get_port_num(sp, ep, out List<Point3d> insert_pts, width);
                        ducts.Add(new Duct_Info(sp, ep, port_num, air_volume, insert_pts));
                        if (ThDuctPortsService.Is_equal(air_volume, 0))
                            exclude_line.Add(l);
                        ep = sp;
                    }
                }
                else
                {
                    // 回到起始点
                    double width = ThDuctPortsService.Get_width(in_param.in_duct_size);
                    int port_num = Get_port_num(Point3d.Origin, ep, out List<Point3d> insert_pts, width);
                    ducts.Add(new Duct_Info(Point3d.Origin, ep, port_num, air_volume, insert_pts));
                }
            }
        }
        private int Get_port_num(Point3d sp, Point3d ep, out List<Point3d> insert_pts, double width)
        {
            insert_pts = new List<Point3d>();
            var l = new Line(sp, ep);
            center_line.Add(l);
            var dir_vec = ThDuctPortsService.Get_edge_direction(l);
            var pl = ThDuctPortsService.Get_line_extend(l, width * 0.5);
            var res = port_index.SelectCrossingPolygon(pl);
            foreach (Polyline port in res)
                insert_pts.Add(port.GetCentroidPoint());
            if (dir_vec.X >= 0)
                insert_pts = insert_pts.OrderBy(o => o.X).ToList();
            else
                insert_pts = insert_pts.OrderByDescending(o => o.X).ToList();
            if (dir_vec.Y >= 0)
                insert_pts = insert_pts.OrderBy(o => o.Y).ToList();
            else
                insert_pts = insert_pts.OrderByDescending(o => o.Y).ToList();
            return res.Count;
        }
        private void Search_conn_comp(Point3d start_p)
        {
            var queue = new Queue<Point3d>();
            queue.Enqueue(start_p);
            while (queue.Count != 0)
            {
                var curPt = queue.Dequeue();
                var poly = new Polyline();
                poly.CreatePolygon(curPt.ToPoint2D(), 4, 10);
                var selectedBounds = group_index.SelectCrossingPolygon(poly);
                foreach (Polyline b in selectedBounds)
                {
                    if (!poly_set.Add(b))
                        continue;
                    var portPts = Get_step_point(b, curPt);
                    portPts.ForEach(pt => queue.Enqueue(pt));
                }
            }
        }
        private int Count_port(Point3d start_p)
        {
            int port_num = 0;
            var queue = new Queue<Point3d>();
            queue.Enqueue(start_p);
            while (queue.Count != 0)
            {
                var curPt = queue.Dequeue();
                var poly = new Polyline();
                poly.CreatePolygon(curPt.ToPoint2D(), 4, 10);
                var selectedBounds = group_index.SelectCrossingPolygon(poly);
                foreach (Polyline pl in selectedBounds)
                {
                    if (!poly_set.Add(pl)) 
                        continue;
                    var res = port_index.SelectCrossingPolygon(pl);
                    port_num += res.Count;
                    var portPts = Get_step_point(pl, curPt);
                    portPts.ForEach(pt => queue.Enqueue(pt));
                }
            }
            return port_num;
        }
        private Polyline Get_polyline(Polyline poly)
        {
            foreach (var p in bounds_2_id_dic)
            {
                if (poly.Bounds.Value.IsEqualTo(p.Key.Bounds.Value, tor))
                {
                    return p.Key;
                }
            }
            throw new NotImplementedException();
        }
        private List<Point3d> Get_step_point(Polyline poly, Point3d exclude_point)
        {
            var p = Get_polyline(poly);
            var id = bounds_2_id_dic[p];
            var ports_dic = ThDuctPortsReadComponent.GetPortsOfGroup(id);
            var port_pts = ports_dic.Values.Select(v => v.Item1).ToList();
            var pts = Move_pts_to_org(port_pts);
            for (int i = 0; i < pts.Count; ++i)
            {
                if (pts[i].IsEqualTo(exclude_point, tor))
                    pts.RemoveAt(i);
            }
            return pts;
        }
        private DBObjectCollection Detect_cross_group(Point3d p)
        {
            var poly = Create_detect_poly(p);
            var res = group_index.SelectCrossingPolygon(poly);
            return res;
        }
        private Polyline Create_detect_poly(Point3d p)
        {
            var poly = new Polyline();
            poly.CreatePolygon(p.ToPoint2D(), 4, 10);
            return poly;
        }
    }
}