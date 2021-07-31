using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    class ThDuctPortsShapeService
    {
        public static Matrix3d Create_cross_trans_mat(Entity_modify_param cross)
        {
            var in_2vec = cross.pos[0] - cross.pos_ext[0];
            double inner_width = cross.port_widths[2];
            double outter_width = cross.port_widths[3];
            int idx = inner_width > outter_width ? 2 : 3;
            var in_2big = cross.pos[idx] - cross.pos_ext[idx];
            var in_vec = new Vector3d(in_2vec.X, in_2vec.Y, 0);
            var big_vec = new Vector3d(in_2big.X, in_2big.Y, 0);
            double rotate_angle =  Get_cross_trans_info(inner_width, outter_width, in_vec, big_vec, out bool is_flip);
            var center_point = Get_entity_center_p(cross);
            return ThDuctPortsService.Get_trans_mat(is_flip, rotate_angle, center_point);
        }
        public static Matrix3d Create_tee_trans_mat(Entity_modify_param tee)
        {
            var branch_2vec = tee.pos[0] - tee.pos_ext[0];
            var in_2vec = tee.pos[1] - tee.pos_ext[1];
            var branch_vec = new Vector3d(branch_2vec.X, branch_2vec.Y, 0);
            var in_vec = new Vector3d(in_2vec.X, in_2vec.Y, 0);
            double rotate_angle = Get_tee_trans_info(in_vec, branch_vec, out bool is_flip);
            var center_point = Get_entity_center_p(tee);
            return ThDuctPortsService.Get_trans_mat(is_flip, rotate_angle, center_point);
        }
        public static Matrix3d Create_elbow_trans_mat(Entity_modify_param elbow)
        {
            var in_2vec = elbow.pos[0] - elbow.pos_ext[0];
            var out_2vec = elbow.pos[1] - elbow.pos_ext[1];
            double open_angle = in_2vec.GetAngleTo(out_2vec);
            var in_vec = new Vector3d(in_2vec.X, in_2vec.Y, 0);
            var out_vec = new Vector3d(out_2vec.X, out_2vec.Y, 0);
            var center_point = Get_entity_center_p(elbow);
            double rotate_angle = Get_elbow_trans_info(open_angle, in_vec, out_vec, out bool is_flip);
            return ThDuctPortsService.Get_trans_mat(is_flip, rotate_angle, center_point);
        }
        public static Point2d Get_entity_center_p(Entity_modify_param entity)
        {
            if (entity.type == "Elbow")
                return Get_elbow_center_p(entity);
            else if (entity.type == "Tee")
                return Get_tee_center_p(entity);
            else if (entity.type == "Cross")
                return Get_cross_center_p(entity);
            else
                throw new NotImplementedException();
        }
        public static Point2d Get_cross_center_p(Entity_modify_param cross)
        {
            var in_2vec = cross.pos[0] - cross.pos_ext[0];
            double inner_width = cross.port_widths[2];
            double outter_width = cross.port_widths[3];
            int idx = inner_width > outter_width ? 2 : 3;
            var shrink_len = cross.port_widths[idx] + 50;
            return cross.pos[0] - in_2vec * shrink_len;
        }
        public static Point2d Get_tee_center_p(Entity_modify_param tee)
        {
            var in_2vec = tee.pos[1] - tee.pos_ext[1];
            var shrink_len = Get_tee_main_shrink(tee);
            return tee.pos[1] - in_2vec * shrink_len;
        }
        public static Point2d Get_elbow_center_p(Entity_modify_param elbow)
        {
            var in_2vec = elbow.pos[0] - elbow.pos_ext[0];
            var open_angle = Get_elbow_open_angle(elbow);
            var shrink_len = Get_elbow_shrink(open_angle, elbow.port_widths[0], 0, 0.7);
            return elbow.pos[0] - in_2vec * shrink_len;
        }
        public static double Get_cross_trans_info(double inner_width,
                                                  double outter_width,
                                                  Vector3d in_vec,
                                                  Vector3d big_vec,
                                                  out bool is_flip)
        {
            is_flip = false;
            double rotate_angle = in_vec.GetAngleTo(-Vector3d.YAxis);
            double z = in_vec.CrossProduct(-Vector3d.YAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z < 0)
                rotate_angle += Math.PI;
            if (!ThDuctPortsService.Is_equal(inner_width, outter_width))
            {
                if (ThDuctPortsService.Is_outter(in_vec, big_vec))
                    is_flip = true;
            }
            return rotate_angle;
        }
        public static double Get_tee_trans_info(Vector3d in_vec, Vector3d branch_vec, out bool is_flip)
        {
            double rotate_angle;
            if (ThDuctPortsService.Is_outter(in_vec, branch_vec))
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
            return rotate_angle;
        }
        private static double Get_tee_main_shrink(Entity_modify_param tee)
        {
            var branch_vec = tee.pos[0] - tee.pos_ext[0];
            var other_vec = tee.pos[2] - tee.pos_ext[2];
            var shrink = tee.port_widths[0] + 50;
            if (ThDuctPortsService.Is_collinear(branch_vec, other_vec))
            {
                if (tee.port_widths[0] < tee.port_widths[2])
                    shrink = tee.port_widths[2] + 50;
            }
            return shrink;
        }
        public static double Get_elbow_trans_info(double open_angle, Vector3d in_vec, Vector3d out_vec, out bool is_flip)
        {
            if (in_vec.CrossProduct(out_vec).Z < 0)
            {
                is_flip = true;
                return Get_elbow_rotate_angle(in_vec, Vector3d.XAxis, open_angle, ref is_flip);
            }
            else
            {
                is_flip = false;
                return Get_elbow_rotate_angle(in_vec, -Vector3d.XAxis, open_angle, ref is_flip);
            }
        }
        public static double Get_elbow_shrink(double open_angle, double width, double reducing_len, double K)
        {
            if (open_angle > 0.5 * Math.PI)
            {
                Point2d center_point = new Point2d(-0.7 * width, -Math.Abs(0.7 * width * Math.Tan(0.5 * (Math.PI - open_angle))));
                return Math.Abs(center_point.Y) + reducing_len + 50;
            }
            else if (ThDuctPortsService.Is_equal(open_angle, 0.5 * Math.PI))
                return K * (width + reducing_len) + 50;
            else if (open_angle > 0 && open_angle < 0.5 * Math.PI)
                throw new NotImplementedException();
            else
                return 0;
        }
        public static double Get_elbow_rotate_angle(Vector3d in_vec, Vector3d judger_vec, double open_angle, ref bool is_flip)
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
                throw new NotImplementedException();
        }
        public static double Get_elbow_open_angle(Entity_modify_param param)
        {
            var dir_vec1 = param.pos[0] - param.pos_ext[0];
            var dir_vec2 = param.pos[1] - param.pos_ext[1];
            return dir_vec1.GetAngleTo(dir_vec2);
        }
    }
}
