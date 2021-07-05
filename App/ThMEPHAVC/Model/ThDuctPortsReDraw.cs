using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReDraw
    {
        public ThDuctPortsReDraw(ObjectId[] ids,
                                 string cur_duct_width,
                                 double air_volume,
                                 DuctPortsParam in_param)
        {
            Line l;
            using (var db = AcadDatabase.Active())
            {
                l = db.Element<Entity>(ids[4]) as Line;
            }
            Remove_former_lines(ids);
            using (var db = AcadDatabase.Active())
            {
                double width = ThDuctPortsService.Get_width(cur_duct_width);
                ThDuctPortsService.Get_line_pos_info(l, out double angle, out Point3d center_point);
                ThDuctPortsFactory.Get_duct_geo_flg_center_line(l, width, angle, center_point, out DBObjectCollection geo, out DBObjectCollection flg, out DBObjectCollection center_line);
                var mat = Matrix3d.Identity;
                var service = new ThDuctPortsDrawService(in_param.scenario, in_param.scale);
                ThDuctPortsDrawService.Draw_lines(geo, mat, service.geo_layer, out ObjectIdList geo_ids);
                ThDuctPortsDrawService.Draw_lines(flg, mat, service.flg_layer, out ObjectIdList flg_ids);
                ThDuctPortsDrawService.Draw_lines(center_line, mat, service.center_layer, out ObjectIdList center_ids);
                var param = new Duct_modify_param() { air_volume = air_volume, duct_size = cur_duct_width };
                ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, param);
            }
                
        }
        private void Remove_former_lines(ObjectId[] ids)
        {
            using (var adb = AcadDatabase.Active())
            {
                foreach (var id in ids)
                    adb.Element<Entity>(id, true).Erase();
            }
        }
    }
}
