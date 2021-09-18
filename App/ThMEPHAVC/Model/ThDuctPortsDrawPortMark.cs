using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPortMark
    {
        public static void Insert_mark( ThMEPHVACParam param,
                                        double port_width,
                                        double port_height,
                                        string port_mark_name,
                                        string port_mark_layer,
                                        Point3d p)
        {
            string port_size = port_width.ToString() + 'x' + port_height.ToString();
            double h = ThMEPHVACService.Get_text_height(param.scale);
            double scale_h = h * 2 / 3;
            int port_num = param.port_num;
            if (param.port_range.Contains("侧"))
                port_num *= 2;
            double single_port_volume = ((int)((param.air_volume / port_num) / 50)) * 50;
            string str_volume = single_port_volume.ToString("0.");
            if (param.high_air_volume > 0)
            {
                double a_v = ((int)((param.high_air_volume / port_num) / 50)) * 50;
                str_volume = a_v.ToString("0.") + "/" + str_volume;
            }
            using (var acadDb = AcadDatabase.Active())
            {
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_mark_layer, port_mark_name, p, new Scale3d(scale_h, scale_h, 1), 0,
                          new Dictionary<string, string> { { "风口名称", param.port_name },
                                                           { "尺寸", port_size },
                                                           { "数量", port_num.ToString() },
                                                           { "风量", str_volume} });
            }
        }
        public static void Insert_leader(Point3d srt_p, Point3d end_p, string port_mark_layer)
        {
            Leader leader = new Leader { HasArrowHead = true };
            leader.AppendVertex(srt_p);
            leader.AppendVertex(end_p);
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                adb.ModelSpace.Add(leader);
                leader.SetDatabaseDefaults();
                leader.Layer = port_mark_layer;
                leader.ColorIndex = (int)ColorIndex.BYLAYER;
                leader.Linetype = "ByLayer";
            }
        }
    }
}
