using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPortMark
    {
        private double ucsAngle;
        private string portMarkName;
        private string portMarkLayer;
        public ThDuctPortsDrawPortMark(double ucsAngle, string portMarkName, string portMarkLayer)
        {
            this.ucsAngle = ucsAngle;
            this.portMarkName = portMarkName;
            this.portMarkLayer = portMarkLayer;
        }
        public void InsertMark(PortParam portParam, double portWidth, double portHeight, Point3d p)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var param = portParam.param;
                string portSize = portWidth.ToString() + 'x' + portHeight.ToString();
                var strs = param.scale.Split(':');
                double scaleH = (strs.Length == 2) ? Double.Parse(strs[1]) : 100;
                int portNum = param.portNum;
                if (param.portRange.Contains("侧"))
                    portNum *= 2;
                double avgAirVolume = param.airVolume / portNum;
                avgAirVolume = (int)avgAirVolume;
                var strVolume = avgAirVolume.ToString();
                if (param.highAirVolume > 0)
                {
                    double av = param.highAirVolume / portNum;
                    av = (int)av;
                    strVolume = av.ToString("0.") + "/" + strVolume;
                }
                var num = portParam.verticalPipeEnable ? param.portBottomEle : param.elevation;
                var ele = param.portRange.Contains("侧") ? ("风口底边距地" + num.ToString("0.00") + "m") : " ";
                var attr = new Dictionary<string, string> { { "风口名称", param.portName },
                                                            { "尺寸", portSize },
                                                            { "数量", portNum.ToString() },
                                                            { "风量", strVolume},
                                                            { "安装属性", ele} };
                // 设置框的角度
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                    portMarkLayer, portMarkName, p, new Scale3d(scaleH, scaleH, scaleH), -ucsAngle, attr);
                // 设置框内字的角度
                ThMEPHVACService.SetAttr(obj, attr, -ucsAngle);
            }
        }
        public void InsertLeader(Point3d srtP, Point3d endP)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                Leader leader = new Leader { HasArrowHead = true };
                leader.AppendVertex(srtP);
                leader.AppendVertex(endP);
                adb.ModelSpace.Add(leader);
                leader.SetDatabaseDefaults();
                leader.Layer = portMarkLayer;
                leader.ColorIndex = (int)ColorIndex.BYLAYER;
                leader.Linetype = "ByLayer";
            }
        }
    }
}
