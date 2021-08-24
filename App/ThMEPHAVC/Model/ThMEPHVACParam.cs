using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public enum Tee_Type
    {
        BRANCH_COLLINEAR_WITH_OTTER = 0,
        BRANCH_VERTICAL_WITH_OTTER = 1
    };
    public class Special_graph_Info
    {
        //图形的中心点为lines[0].startpoint
        public double K { get; set; }
        public List<Line> lines { get; set; } //lines[0]为in_line 其余为out_lines
        public List<double> every_port_width { get; set; }
        public Special_graph_Info(List<Line> lines_, List<double> every_port_width_)
        {
            K = 0.7;
            lines = lines_;
            every_port_width = every_port_width_;
        }
    };
    public class ThMEPHVACParam
    {
        public bool is_redraw;
        public int port_num;
        public double air_speed;
        public double air_volume;
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
    public class Fence_Info
    {
        public double left;
        public double right;
        public double top;
        public double bottom;
        public Fence_Info()
        {
            right = top = 0;
            left = bottom = double.MaxValue;
        }
    }
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
    public class Side_Port_Info
    {
        public bool is_left;
        public List<Handle> port_handles;
        public Side_Port_Info(bool is_left, List<Handle> port_handles)
        {
            this.is_left = is_left;
            this.port_handles = port_handles;
        }
    }
    public class Fan_duct_Info
    {
        public Point3d sp;
        public Point3d ep;
        public string size;
        public double src_shrink;
        public double dst_shrink;
        public Fan_duct_Info() { }
        public Fan_duct_Info (Point3d sp, Point3d ep, string size)
        {
            this.sp = sp;
            this.ep = ep;
            this.size = size;
            src_shrink = dst_shrink = 0;
        }
        public Fan_duct_Info(Point3d sp, Point3d ep, string size, double src_shrink, double dst_shrink)
        {
            this.sp = sp;
            this.ep = ep;
            this.size = size;
            this.src_shrink = src_shrink;
            this.dst_shrink = dst_shrink;
        }
    }
}
