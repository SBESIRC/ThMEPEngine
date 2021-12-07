using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPortMark
    {
        public static void InsertMark( ThMEPHVACParam param,
                                        double port_width,
                                        double port_height,
                                        string port_mark_name,
                                        string port_mark_layer,
                                        Point3d p)
        {
            string port_size = port_width.ToString() + 'x' + port_height.ToString();
            double h = ThMEPHVACService.GetTextHeight(param.scale);
            double scale_h = h * 2 / 3;
            int port_num = param.portNum;
            if (param.portRange.Contains("侧"))
                port_num *= 2;
            double avgAirVolume = param.airVolume / param.portNum;
            avgAirVolume = (Math.Ceiling(avgAirVolume / 10)) * 10;
            var strVolume = avgAirVolume.ToString();
            if (param.highAirVolume > 0)
            {
                double av = param.highAirVolume / param.portNum;
                av = (Math.Ceiling(av / 10)) * 10;
                strVolume = av.ToString("0.") + "/" + strVolume;
            }
            using (var acadDb = AcadDatabase.Active())
            {
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(port_mark_layer, port_mark_name, p, new Scale3d(scale_h, scale_h, 1), 0,
                          new Dictionary<string, string> { { "风口名称", param.portName },
                                                           { "尺寸", port_size },
                                                           { "数量", port_num.ToString() },
                                                           { "风量", strVolume} });
            }
        }
        public static void InsertLeader(Point3d srt_p, Point3d end_p, string port_mark_layer)
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
