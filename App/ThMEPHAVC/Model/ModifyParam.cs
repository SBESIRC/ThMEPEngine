using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class Entity_modify_param
    {
        public ObjectId start_id;
        public List<Point2d> pos;
        public List<Point2d> pos_ext;
        public Entity_modify_param(ObjectId start_id_,
                                   List<Point2d> pos_,
                                   List<Point2d> pos_ext_)
        {
            pos = pos_;
            pos_ext = pos_ext_;
            start_id = start_id_;
        }

    }
    public class Duct_modify_param
    {
        public string duct_size;
        public double air_volume;
        public Entity_modify_param identity_info;
        public Duct_modify_param() { }
        public Duct_modify_param(string duct_size_, 
                                 double air_volume_,
                                 Entity_modify_param info)
        {
            duct_size = duct_size_;
            air_volume = air_volume_;
            identity_info = new Entity_modify_param(info.start_id, info.pos, info.pos_ext);
        }
    }
    public class Entity_param
    {
        public ObjectId id;
        public string entity_name;
        public Entity_param(ObjectId id_, string entity_name_)
        {
            id = id_;
            entity_name = entity_name_;
        }
    }
}
