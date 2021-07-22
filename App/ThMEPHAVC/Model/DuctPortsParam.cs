using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class DuctPortsParam
    {
        public int port_num;
        public double air_speed;
        public double air_volumn;
        public double elevation;
        public double main_height;
        public string scale;
        public string scenario;
        public string port_size;
        public string port_name;
        public string port_range;
        public string in_duct_size;
    }
    public class Trans_info
    {
        public bool is_flip;
        public double rotate_angle;
        public Point2d center_point;
        public Trans_info(bool is_flip_, double rotate_angle_, Point2d center_point_)
        {
            is_flip = is_flip_;
            rotate_angle = rotate_angle_;
            center_point = center_point_;
        }
        public Trans_info(Trans_info info)
        {
            is_flip = info.is_flip;
            rotate_angle = info.rotate_angle;
            center_point = info.center_point;
        }
    }
    public class Elbow_Info
    {
        public double open_angle;
        public double duct_width;
        public Trans_info trans;
        public Elbow_Info(double open_angle_, double duct_width_, Trans_info info)
        {
            open_angle = open_angle_;
            duct_width = duct_width_;
            trans = new Trans_info(info);
        }
    }
    public class Tee_Info
    {
        public double main_width;
        public double branch;
        public double other;
        public Trans_info trans;
        public Tee_Info(double main_width, double branch, double other, Trans_info info)
        {
            this.main_width = main_width;
            this.branch = branch;
            this.other = other;
            trans = new Trans_info(info);
        }
    }
    public class Cross_Info
    {
        public double i_width;
        public double o_width1;
        public double o_width2;
        public double o_width3;
        public Trans_info trans;
        public Cross_Info(double i_width_, double o_width1_, double o_width2_, double o_width3_, Trans_info info)
        {
            i_width = i_width_;
            o_width1 = o_width1_;
            o_width2 = o_width2_;
            o_width3 = o_width3_;
            trans = new Trans_info(info);
        }
    }
}
