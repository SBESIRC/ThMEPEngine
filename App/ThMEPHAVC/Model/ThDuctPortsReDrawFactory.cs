using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReDrawFactory
    {
        public static Line_Info Create_reducing(Entity_modify_param reducing, int port_idx, string modify_size)
        {
            var modify_width = ThMEPHVACService.Get_width(modify_size);
            reducing.port_widths[port_idx] = modify_width;
            var sp = new Point3d(reducing.pos[0].X, reducing.pos[0].Y, 0);
            var ep = new Point3d(reducing.pos[1].X, reducing.pos[1].Y, 0);
            var line = new Line(sp, ep);
            return Create_reducing(line, reducing.port_widths[0], reducing.port_widths[1], false);
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
        public static Line_Info Create_tee(Entity_modify_param tee, int port_idx, double modify_width)
        {
            var branch_vec = tee.pos[0] - tee.pos_ext[0];
            var main_small_vec = tee.pos[2] - tee.pos_ext[2];
            var type = ThMEPHVACService.Is_collinear(branch_vec, main_small_vec) ?
                       Tee_Type.BRANCH_COLLINEAR_WITH_OTTER : Tee_Type.BRANCH_VERTICAL_WITH_OTTER;
            tee.port_widths[port_idx] = modify_width;
            return ThDuctPortsFactory.Create_tee(tee.port_widths[1], tee.port_widths[0], tee.port_widths[2], type);
        }
        public static Line_Info Create_cross(Entity_modify_param cross, int port_idx, double modify_width)
        {
            cross.port_widths[port_idx] = modify_width;
            if (cross.port_widths[2] != cross.port_widths[3])
            {
                if (cross.port_widths[2] < cross.port_widths[3])
                {
                    double tmp = cross.port_widths[2];
                    cross.port_widths[2] = cross.port_widths[3];
                    cross.port_widths[3] = tmp;
                }
            }
            return ThDuctPortsFactory.Create_cross(cross.port_widths[0], cross.port_widths[2], cross.port_widths[1], cross.port_widths[3]);
        }
    }
}