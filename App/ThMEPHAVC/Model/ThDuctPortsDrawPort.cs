using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPort
    {
        public string port_name;
        public string port_layer;
        public ThDuctPortsDrawPort(string port_layer, string port_name)
        {
            this.port_name = port_name;
            this.port_layer = port_layer;
        }
        public void Draw_ports(Duct_ports_Info info, 
                               DuctPortsParam in_param, 
                               Vector3d org_dis_vec,
                               double port_width,
                               double port_height)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var dir_vec = ThDuctPortsService.Get_edge_direction(info.l);
                double angle = ThDuctPortsService.Get_port_rotate_angle(dir_vec);
                foreach (var pos in info.ports_info)
                {
                    if (in_param.port_range.Contains("下"))
                    {
                        var p = ThDuctPortsService.Get_down_port_insert_pos(dir_vec, pos.position, port_width, port_height);
                        p += org_dis_vec;
                        Insert_port(p, angle, port_width, port_height, in_param.port_range);
                    }
                    else
                    {
                        ThDuctPortsService.Get_side_port_insert_pos(dir_vec, pos.position, info.width, port_width, out Point3d pL, out Point3d pR);
                        pL += org_dis_vec;
                        pR += org_dis_vec;
                        Insert_port(pL, angle + Math.PI * 0.5, port_width, port_height, in_param.port_range);
                        Insert_port(pR, angle - Math.PI * 0.5, port_width, port_height, in_param.port_range);
                    }
                }
            }
        }
        public void Insert_port(Point3d pos, double angle, double port_width, double port_height, string port_range)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(port_layer, port_name, pos, new Scale3d(), angle);
                ThDuctPortsDrawService.Set_port_dyn_block_properity(obj, port_width, port_height, port_range);
            }
        }
    }
}
