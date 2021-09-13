using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class Fan_modify_param
    {
        public string fan_name;
        public Fan_modify_param(string fan_name) 
        {
            this.fan_name = fan_name;
        }
    }
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
    public class Hole_modify_param
    {
        public Handle handle;
        public string hole_name;
        public string hole_layer;
        public Point2d insert_p;
        public double rotate_angle;
        public double len;
        public double width;
        public Hole_modify_param() { }
    }
    public class Muffler_modify_param
    {
        public Handle handle;
        public string name;
        public string muffler_layer;
        public Point2d insert_p;
        public string muffler_visibility;
        public double len;
        public double width;
        public double height;
        public double text_height;
        public double rotate_angle;
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
        public double text_height;
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
        public Entity_modify_param(string type,
                                   Handle start_id,
                                   List<Point2d> pos,
                                   List<Point2d> pos_ext,
                                   List<double> port_widths)
        {
            this.pos = pos;
            this.type = type;
            this.pos_ext = pos_ext;
            this.start_id = start_id;
            this.port_widths = port_widths;
            handle = ObjectId.Null.Handle;
        }
    }
    public class Duct_modify_param
    {
        public Point2d sp;
        public Point2d ep;
        public Handle handle;
        public Handle start_handle;
        public string type;
        public string duct_size;
        public double air_volume;
        public double elevation;
        public Duct_modify_param() { }
        public Duct_modify_param(string duct_size, 
                                 double air_volume,
                                 double elevation,
                                 Point2d sp,
                                 Point2d ep,
                                 Handle start_handle)
        {
            type = "Duct";
            this.sp = sp;
            this.ep = ep;
            this.elevation = elevation;
            this.duct_size = duct_size;
            this.air_volume = air_volume;
            this.start_handle = start_handle;
        }
    }
    public class VT_elbow_modify_param
    {
        public Handle handle;
        public Point2d detect_p;
        public VT_elbow_modify_param() { }
    }
}