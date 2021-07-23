using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class Port_modify_param
    {
        public Point2d pos;
        public Handle handle;
        public string port_range;
        public double port_width;
        public double port_height;
        public double rotate_angle;
        public Port_modify_param() { }
    }
    public class Text_modify_param
    {
        public Point3d pos;
        public Handle handle;
        public Point2d center_point;
        public string text_string;
        public double rotate_angle;
        public Text_modify_param(Handle handle, Point2d center_point, string text_string,
                                 double rotate_angle, Point3d pos)
        {
            this.pos = pos;
            this.handle = handle;
            this.text_string = text_string;
            this.center_point = center_point;
            this.rotate_angle = rotate_angle;
        }
    }
    public class Valve_modify_param
    {
        public Handle handle;
        public string valve_name;
        public string valve_layer;
        public string valve_visibility;
        public Point2d judge_p;
        public Point2d insert_p;
        public double rotate_angle;
        public double width;
        public double height;
        public double text_angle;
        public Valve_modify_param() { }
    }
    public class Entity_modify_param
    {
        public string type;
        public Handle handle;
        public Handle start_id;
        public List<Point2d> pos;
        public List<Point2d> pos_ext;
        public List<double> port_widths;
        public Entity_modify_param()
        {
            type = string.Empty;
            pos = new List<Point2d>();
            handle = ObjectId.Null.Handle;
            pos_ext = new List<Point2d>();
            start_id = ObjectId.Null.Handle;
            port_widths = new List<double>();
        }
        public Entity_modify_param(string type_,
                                   Handle start_id_,
                                   List<Point2d> pos_,
                                   List<Point2d> pos_ext_,
                                   List<double> port_widths_)
        {
            pos = pos_;
            type = type_;
            pos_ext = pos_ext_;
            start_id = start_id_;
            handle = ObjectId.Null.Handle;
            port_widths = port_widths_;
        }
    }
    public class Duct_modify_param
    {
        public Point2d sp;
        public Point2d ep;
        public Handle handle;
        public Handle start_handle;
        public Handle duct_size_handle;
        public Handle elevation_handle;
        public string duct_size;
        public double air_volume;
        public string type;
        public Duct_modify_param() 
        {
            air_volume = 0;
            duct_size = string.Empty;
        }
        public Duct_modify_param(string duct_size_, 
                                 double air_volume_,
                                 Point2d sp_,
                                 Point2d ep_,
                                 Handle start_handle_)
        {
            duct_size = duct_size_;
            air_volume = air_volume_;
            sp = sp_;
            ep = ep_;
            start_handle = start_handle_;
        }
    }
}