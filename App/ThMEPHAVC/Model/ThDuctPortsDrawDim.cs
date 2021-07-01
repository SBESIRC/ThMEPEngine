using System;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawDim
    {
        public string scale;
        public string dimension_layer;
        public ThDuctPortsDrawDim(string dimension_layer_, string scale_)
        {
            scale = scale_;
            dimension_layer = dimension_layer_;
        }
        public void Draw_dimension(List<Duct_ports_Info> infos, Point2d dir_wall_point, Point2d ver_wall_point, Point3d start_pos)
        {
            Vector3d vec = ThDuctPortsService.Get_edge_direction(infos[0].l);
            if (!ThDuctPortsService.Is_vertical(vec, Vector3d.XAxis) &&
                !ThDuctPortsService.Is_vertical(vec, Vector3d.YAxis))
                return;
            Insert_ver_dimension(infos, ver_wall_point, start_pos);
            Insert_dir_dimension(infos, dir_wall_point, start_pos);
        }
        private void Insert_ver_dimension(List<Duct_ports_Info> infos, Point2d ver_wall_point, Point3d start_pos)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                if (infos.Count > 0 && ver_wall_point.GetDistanceTo(Point2d.Origin) > 1e-3)
                {
                    Point3d wall_point = new Point3d(ver_wall_point.X, ver_wall_point.Y, 0) + start_pos.GetAsVector();
                    var port_info = infos[infos.Count - 1].ports_info;
                    if (port_info.Count > 0)
                    {
                        var p = port_info[port_info.Count - 1].position + start_pos.GetAsVector();
                        var layerId = db.Layers.ElementOrDefault(dimension_layer).ObjectId;
                        if (infos.Count > 0)
                        {
                            var info = infos[0];
                            Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(info.l);
                            var positions = info.ports_info;
                            if (positions.Count > 0)
                            {
                                var dim = Create_align_dim(wall_point, p, dir_vec, layerId);
                                db.ModelSpace.Add(dim);
                                dim.SetDatabaseDefaults();
                            }
                        }
                    }
                }
            }
        }
        private AlignedDimension Create_align_dim(Point3d p1, Point3d p2, Vector3d vertical_vec, ObjectId layerId)
        {
            string style = ThDuctPortsService.Get_dim_style(scale);
            using (var adb = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, adb.Database);
                return new AlignedDimension
                {
                    XLine1Point = p1,
                    XLine2Point = p2,
                    DimensionText = "",
                    DimLinePoint = ThDuctPortsService.Get_mid_point(p1, p2) + vertical_vec * 2000,
                    ColorIndex = 256,
                    DimensionStyle = id,
                    LayerId = layerId,
                    Linetype = "ByLayer"
                };
            }
        }
        private void Insert_dir_dimension(List<Duct_ports_Info> infos, Point2d dir_wall_point, Point3d start_pos)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var layerId = db.Layers.ElementOrDefault(dimension_layer).ObjectId;
                Insert_wall_point(infos, dir_wall_point);
                var dis_vec = start_pos.GetAsVector();
                for (int i = 0; i < infos.Count; ++i)
                {
                    var info = infos[i];
                    Vector3d dir_vec = ThDuctPortsService.Get_edge_direction(info.l);
                    Vector3d vertical_vec = Get_dimension_vertical_vec(dir_vec);
                    for (int j = 0; j < info.ports_info.Count - 1; ++j)
                    {
                        if (info.ports_info[j].air_volume > 0 &&
                            info.ports_info[j + 1].air_volume > 0)
                        {
                            var dim = Create_align_dim(info.ports_info[j].position + dis_vec, 
                                                       info.ports_info[j + 1].position + dis_vec, 
                                                       vertical_vec, 
                                                       layerId);
                            db.ModelSpace.Add(dim);
                            dim.SetDatabaseDefaults();
                        }
                    }
                    Draw_gap_dimension(i, infos, dis_vec, vertical_vec, dir_wall_point, layerId, db);
                }
            }
        }
        private void Insert_wall_point(List<Duct_ports_Info> infos, Point2d wall_point)
        {
            if (wall_point.GetDistanceTo(Point2d.Origin) < 1e-3)
                return;
            Point3d wall_p = new Point3d(wall_point.X, wall_point.Y, 0);
            if (Insert_wall_point_not_in_line(infos, wall_p))
                return;
            foreach (var info in infos)
            {
                if (info.ports_info.Count > 0)
                {
                    Point3d srt_port_p = info.ports_info[0].position;
                    Point3d end_port_p = info.ports_info[info.ports_info.Count - 1].position;
                    Point3d srt_p = info.l.StartPoint;
                    Point3d end_p = info.l.EndPoint;
                    if (ThDuctPortsService.Is_between_points(wall_p, srt_port_p, end_port_p) ||
                        ThDuctPortsService.Is_between_points(wall_p, srt_p, end_p))
                    {
                        Search_nearest_point(info, wall_p, out int min_idx);
                        Do_insert_wall_point(info, wall_p, min_idx);
                        return;
                    }
                }
            }
            if (Insert_wall_point_in_reducing(infos, wall_p))
                return;
            throw new NotImplementedException();
        }
        private bool Insert_wall_point_not_in_line(List<Duct_ports_Info> infos, Point3d wall_p)
        {
            var first_info = infos[0];
            var last_info = infos[infos.Count - 1];
            Point3d total_srt_p = (first_info.ports_info.Count > 0) ? first_info.ports_info[0].position : first_info.l.StartPoint;
            var info = last_info.ports_info;
            Point3d total_end_p = (info.Count > 0) ? info[info.Count - 1].position : last_info.l.EndPoint;
            if (!ThDuctPortsService.Is_between_points(wall_p, total_srt_p, total_end_p))
            {
                if (wall_p.DistanceTo(total_srt_p) < wall_p.DistanceTo(total_end_p))
                    first_info.ports_info.Insert(0, new Port_Info(1, wall_p));
                else
                    last_info.ports_info.Insert(last_info.ports_info.Count, new Port_Info(1, wall_p));
                return true;
            }
            return false;
        }
        private void Search_nearest_point(Duct_ports_Info info, Point3d wall_p, out int min_idx)
        {
            min_idx = 0;
            double min_dis = Double.MaxValue;
            for (int i = 0; i < info.ports_info.Count; ++i)
            {
                double cur_dis = wall_p.DistanceTo(info.ports_info[i].position);
                if (cur_dis < min_dis)
                {
                    min_dis = cur_dis;
                    min_idx = i;
                }
            }
        }
        private void Do_insert_wall_point(Duct_ports_Info info, Point3d wall_p, int min_idx)
        {
            if (info.ports_info.Count > 0)
            {
                Point3d min_p = info.ports_info[min_idx].position;
                Vector3d dir1 = (wall_p - min_p).GetNormal();
                Vector3d dir2 = ThDuctPortsService.Get_edge_direction(info.l);
                // 插入的墙点的风量设为1与变径处的0风量做区分
                if (dir1 != dir2)
                    info.ports_info.Insert(min_idx, new Port_Info(1, wall_p));
                else
                    info.ports_info.Insert(min_idx + 1, new Port_Info(1, wall_p));
            }
        }
        private bool Insert_wall_point_in_reducing(List<Duct_ports_Info> infos, Point3d wall_p)
        {
            Point3d pre_srt_p = Point3d.Origin;
            for (int i = 0; i < infos.Count; ++i)
            {
                Point3d srt_p = infos[i].l.StartPoint;
                Point3d end_p = infos[i].l.EndPoint;
                if (i == 0)
                {
                    pre_srt_p = end_p;
                    continue;
                }
                if (ThDuctPortsService.Is_between_points(wall_p, pre_srt_p, srt_p))
                {
                    infos[i].ports_info.Insert(0, new Port_Info(1, wall_p));
                    return true;
                }
                pre_srt_p = end_p;
            }
            return false;
        }
        private Vector3d Get_dimension_vertical_vec(Vector3d dir_vec)
        {
            Vector3d vertical_vec;
            if (Math.Abs(dir_vec.X) < 1e-3)
            {
                vertical_vec = (dir_vec.Y > 0) ? ThDuctPortsService.Get_right_vertical_vec(dir_vec) :
                                                 ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            }
            else if (dir_vec.X > 0)
                vertical_vec = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            else
                vertical_vec = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            return vertical_vec;
        }
        private void Draw_gap_dimension(int idx, 
                                        List<Duct_ports_Info> infos,
                                        Vector3d dis_vec,
                                        Vector3d vertical_vec,
                                        Point2d dir_wall_point,
                                        ObjectId layerId,
                                        AcadDatabase db)
        {
            if (idx < infos.Count - 1)
            {
                var info = infos[idx];
                var next_info = infos[idx + 1];
                if (next_info.ports_info.Count > 0)
                {
                    Point3d nearest_p = Search_nearest_point(info, next_info, dir_wall_point, out Point3d next_p);
                    var dim = Create_align_dim(nearest_p + dis_vec, next_p + dis_vec, vertical_vec, layerId);
                    db.ModelSpace.Add(dim);
                    dim.SetDatabaseDefaults();
                }
            }
        }
        private Point3d Search_nearest_point(Duct_ports_Info info, 
                                             Duct_ports_Info next_info, 
                                             Point2d dir_wall_point, 
                                             out Point3d next_p)
        {
            next_p = next_info.ports_info[0].position;
            Point3d wall_point = new Point3d(dir_wall_point.X, dir_wall_point.Y, 0);
            Point3d last_p = info.ports_info[info.ports_info.Count - 1].position;
            if (ThDuctPortsService.Is_between_points(wall_point, next_p, last_p) && 
                wall_point.DistanceTo(Point3d.Origin) > 1e-3)
                return wall_point;
            else
                return last_p;
        }
    }
}