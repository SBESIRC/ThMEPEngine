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
        private double textAngle;
        private string portMarkName;
        private string portMarkLayer;
        public ThDuctPortsDrawPortMark(double textAngle, string portMarkName, string portMarkLayer)
        {
            this.textAngle = textAngle;
            this.portMarkName = portMarkName;
            this.portMarkLayer = portMarkLayer;
        }
        public void InsertMark(ThMEPHVACParam param, double portWidth, double portHeight, double textAngle, Point3d p)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                string portSize = portWidth.ToString() + 'x' + portHeight.ToString();
                var strs = param.scale.Split(':');
                double scaleH = (strs.Length == 2) ? Double.Parse(strs[1]) : 100;
                int portNum = param.portNum;
                if (param.portRange.Contains("侧"))
                    portNum *= 2;
                double avgAirVolume = param.airVolume / param.portNum;
                avgAirVolume = (Math.Ceiling(avgAirVolume / 10)) * 10;
                var strVolume = avgAirVolume.ToString();
                if (param.highAirVolume > 0)
                {
                    double av = param.highAirVolume / param.portNum;
                    av = (Math.Ceiling(av / 10)) * 10;
                    strVolume = av.ToString("0.") + "/" + strVolume;
                }
                if (textAngle >= Math.PI)
                    textAngle -= Math.PI;
                var attr = new Dictionary<string, string> { { "风口名称", param.portName },
                                                            { "尺寸", portSize },
                                                            { "数量", portNum.ToString() },
                                                            { "风量", strVolume},
                                                            { "安装属性", "风口底边距地*.**m"} };
                var obj = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                    portMarkLayer, portMarkName, p, new Scale3d(scaleH, scaleH, scaleH), textAngle, attr);
                ThMEPHVACService.SetAttr(obj, attr, textAngle);
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
