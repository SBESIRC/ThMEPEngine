using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPortMark
    {
        public static void Insert_mark( DuctPortsParam param,
                                        double port_width,
                                        double port_height,
                                        string port_mark_name,
                                        string port_mark_layer,
                                        Point3d p)
        {
            string port_size = port_width.ToString() + 'x' + port_height.ToString();
            double h = ThDuctPortsService.Get_text_height(param.scale);
            double scale_h = h * 2 / 3;
            double single_port_volume = param.air_volume / param.port_num;
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_mark_layer, port_mark_name, p, new Scale3d(scale_h, scale_h, 1), 0,
                          new Dictionary<string, string> { { "风口名称", param.port_name },
                                                           { "尺寸", port_size },
                                                           { "数量", param.port_num.ToString() },
                                                           { "风量", single_port_volume.ToString("0.")} });
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
