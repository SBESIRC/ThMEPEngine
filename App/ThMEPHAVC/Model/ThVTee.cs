using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThVTee
    {
        private string bypass_size;
        private Point3d in_vt_pos;
        private Point3d out_vt_pos;
        public List<Line_Info> vt_elbow;
        public ThVTee(Point3d in_vt_pos, Point3d out_vt_pos, string bypass_size)
        {
            this.bypass_size = bypass_size;
            this.in_vt_pos = in_vt_pos;
            this.out_vt_pos = out_vt_pos;
            vt_elbow = new List<Line_Info>();
            Create_vt_elbow();
        }
        public void Create_vt_elbow()
        {
            ThMEPHVACService.Seperate_size_info(bypass_size, out double w, out double h);
            Get_vt_rotate_angle(out double i_vt_angle, out double o_vt_angle);
            var dir_vec = (out_vt_pos - in_vt_pos).GetNormal();
            var elbow_height = ThMEPHVACService.Get_height(bypass_size);
            var p = in_vt_pos + (dir_vec * elbow_height * 0.5);
            Record_vt_elbow_info(w, h, i_vt_angle, p, in_vt_pos);
            p = out_vt_pos - (dir_vec * elbow_height * 0.5);
            Record_vt_elbow_info(w, h, o_vt_angle, p, out_vt_pos);
        }
        private void Get_vt_rotate_angle(out double i_vt_angle, out double o_vt_angle)
        {
            var dir_vec = (out_vt_pos - in_vt_pos).GetNormal();
            i_vt_angle = dir_vec.GetAngleTo(-Vector3d.XAxis);
            var z = dir_vec.CrossProduct(-Vector3d.XAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z > 0)
                i_vt_angle = 2 * Math.PI - i_vt_angle;
            dir_vec = -dir_vec;
            o_vt_angle = dir_vec.GetAngleTo(-Vector3d.XAxis);
            z = dir_vec.CrossProduct(-Vector3d.XAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z > 0)
                o_vt_angle = 2 * Math.PI - o_vt_angle;
        }
        private void Record_vt_elbow_info(double w, double h, double ro_angle, Point3d conn_p, Point3d ext_p)
        {
            var dis_mat = Matrix3d.Displacement(ext_p.GetAsVector()) *
                          Matrix3d.Rotation(ro_angle, Vector3d.ZAxis, Point3d.Origin);
            var geo = ThVerticalBypassFactory.Create_vt_elbow_geo(w, h);
            foreach (Line l in geo)
                l.TransformBy(dis_mat);
            var flg = ThVerticalBypassFactory.Create_vt_elbow_flg(w, h);
            foreach (Line l in flg)
                l.TransformBy(dis_mat);
            var ports = new List<Point3d>() { conn_p };
            var ports_ext = new List<Point3d>() { ext_p };
            vt_elbow.Add(new Line_Info(geo, flg, null, ports, ports_ext));
        }
    }
}
