using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReDrawFactory
    {
        public static Line_Info Create_reducing(EntityModifyParam reducing, int port_idx, string modify_size, bool is_axis)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_size);
            reducing.port_widths[port_idx] = modify_width;
            var sp = new Point3d(reducing.pos[0].X, reducing.pos[0].Y, 0);
            var ep = new Point3d(reducing.pos[1].X, reducing.pos[1].Y, 0);
            var line = new Line(sp, ep);
            return Create_reducing(line, reducing.port_widths[0], reducing.port_widths[1], is_axis);
        }
        public static Line_Info Create_reducing(Point2d sp, Point2d ep, double big_width, double small_width)
        {
            var start_p = new Point3d(sp.X, sp.Y, 0);
            var end_p = new Point3d(ep.X, ep.Y, 0);
            var l = new Line(start_p, end_p);
            return Create_reducing(l, big_width, small_width, false);
        }
        public static Line_Info Create_reducing(Line l, double big_width, double small_width, bool is_axis)
        {
            var geo = ThDuctPortsFactory.Create_reducing_geo(l, big_width, small_width, is_axis);
            var flg = ThDuctPortsFactory.Create_reducing_flg(geo);
            var center_line = ThDuctPortsFactory.Create_reducing_center(l);
            ThMEPHVACService.Get_duct_ports(l, out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(geo, flg, center_line, ports, ports_ext);
        }
        public static Line_Info Create_tee(EntityModifyParam tee, int port_idx, double modify_width)
        {
            var branch_vec = tee.pos[0] - tee.pos_ext[0];
            var main_small_vec = tee.pos[2] - tee.pos_ext[2];
            var type = ThMEPHVACService.Is_collinear(branch_vec, main_small_vec) ?
                       Tee_Type.BRANCH_COLLINEAR_WITH_OTTER : Tee_Type.BRANCH_VERTICAL_WITH_OTTER;
            tee.port_widths[port_idx] = modify_width;
            return ThDuctPortsFactory.Create_tee(tee.port_widths[1], tee.port_widths[0], tee.port_widths[2], type);
        }
        public static Line_Info Create_cross(EntityModifyParam cross, string modify_duct_size, int port_idx)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_duct_size);
            var org_width = cross.port_widths[port_idx];
            cross.port_widths[port_idx] = modify_width;
            var big = Math.Max(cross.port_widths[2], cross.port_widths[3]);
            var small = Math.Min(cross.port_widths[2], cross.port_widths[3]);
            var in_2vec = cross.pos[0] - cross.pos_ext[0];
            var modify_2vec = cross.pos[port_idx] - cross.pos_ext[port_idx];
            var in_vec = new Vector3d(in_2vec.X, in_2vec.Y, 0);
            var modify_vec = new Vector3d(modify_2vec.X, modify_2vec.Y, 0);
            if (port_idx == 2 || port_idx == 3)
            {
                var z = in_vec.CrossProduct(modify_vec).Z;
                if (modify_width > org_width)
                {
                    return z < 0 ? ThDuctPortsFactory.Create_cross(cross.port_widths[0], small, cross.port_widths[1], big) :
                                   ThDuctPortsFactory.Create_cross(cross.port_widths[0], big, cross.port_widths[1], small);
                }
                else
                {
                    return z < 0 ? ThDuctPortsFactory.Create_cross(cross.port_widths[0], big, cross.port_widths[1], small) :
                                   ThDuctPortsFactory.Create_cross(cross.port_widths[0], small, cross.port_widths[1], big);
                }
            }
            else
            {
                // 主路管段修改
                return ThDuctPortsFactory.Create_cross(cross.port_widths[0], small, cross.port_widths[1], big);
            }
        }
    }
}