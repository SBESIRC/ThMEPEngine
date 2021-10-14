using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPWSS.Sprinkler.Model
{
    public class DuctModifyParam
    {
        public Point2d sp;          // 管段起点
        public Point2d ep;          // 管段终点
        public Handle handle;       // 管段组handle
        public Handle start_handle; // 总管段起始点handle
        public string type;
        public string duct_size;
        public double air_volume;
        public double elevation;
        public DuctModifyParam() { }
        public DuctModifyParam(string duct_size,
                                 double air_volume,
                                 double elevation,
                                 Point2d sp,
                                 Point2d ep)
        {
            type = "Duct";
            this.sp = sp;
            this.ep = ep;
            this.elevation = elevation;
            this.duct_size = duct_size;
            this.air_volume = air_volume;
        }
        public DuctModifyParam(string duct_size,
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

    public class EntityModifyParam
    {
        public string type;
        public Handle handle;
        public Handle start_id;
        public List<Point2d> pos;
        public List<Point2d> pos_ext;
        public List<double> port_widths;
        public EntityModifyParam()
        {
            type = string.Empty;
            pos = new List<Point2d>();
            handle = ObjectId.Null.Handle;
            pos_ext = new List<Point2d>();
            start_id = ObjectId.Null.Handle;
            port_widths = new List<double>();
        }
        public EntityModifyParam(string type,
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
}
