using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThVTee
    {
        private string bypassSize;
        private Point3d inVtPos;
        private Point3d outVtPos;
        public List<LineGeoInfo> vtElbow;
        public ThVTee(Point3d in_vt_pos, Point3d out_vt_pos, string bypass_size)
        {
            this.bypassSize = bypass_size;
            this.inVtPos = in_vt_pos;
            this.outVtPos = out_vt_pos;
            vtElbow = new List<LineGeoInfo>();
            Create_vt_elbow();
        }
        public void Create_vt_elbow()
        {
            ThMEPHVACService.GetWidthAndHeight(bypassSize, out double w, out double h);
            Get_vt_rotate_angle(out double i_vt_angle, out double o_vt_angle);
            Record_vt_elbow_info(w, h, i_vt_angle, inVtPos);
            Record_vt_elbow_info(w, h, o_vt_angle, outVtPos);
        }
        private void Get_vt_rotate_angle(out double i_vt_angle, out double o_vt_angle)
        {
            var dir_vec = (outVtPos - inVtPos).GetNormal();
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
        private void Record_vt_elbow_info(double w, double h, double ro_angle, Point3d ext_p)
        {
            var dis_mat = Matrix3d.Displacement(ext_p.GetAsVector()) *
                          Matrix3d.Rotation(ro_angle, Vector3d.ZAxis, Point3d.Origin);
            var geo = ThVerticalBypassFactory.Create_vt_elbow_geo(w, h);
            foreach (Line l in geo)
                l.TransformBy(dis_mat);
            var flg = ThVerticalBypassFactory.Create_vt_elbow_flg(w, h);
            foreach (Line l in flg)
                l.TransformBy(dis_mat);
            vtElbow.Add(new LineGeoInfo(geo, flg, null));
        }
    }
}