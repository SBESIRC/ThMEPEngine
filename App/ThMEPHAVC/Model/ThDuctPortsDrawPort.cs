using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPort
    {
        public double textAngle;
        public string portName;
        public string portLayer;
        public ThDuctPortsDrawPort(string portLayer, string portName, double textAngle)
        {
            this.portName = portName;
            this.textAngle = textAngle;
            this.portLayer = portLayer;
        }
        public void DrawPorts(EndlineSegInfo info, string portRange, Vector3d orgDisVec, double portWidth, double portHeight, double avgAirVolume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var dirVec = ThMEPHVACService.GetEdgeDirection(info.seg.l);
                double angle = ThMEPHVACService.GetPortRotateAngle(dirVec);
                foreach (var pos in info.portsInfo)
                {
                    if (portRange.Contains("下"))
                    {
                        var p = pos.position + orgDisVec;
                        InsertPort(p, angle + (Math.PI * 0.5), portWidth, portHeight, portRange, avgAirVolume);
                    }
                    else
                    {
                        var curDuctW = ThMEPHVACService.GetWidth(pos.ductSize);
                        GetSidePortInsertPos(dirVec, pos.position, curDuctW, out Point3d pL, out Point3d pR);
                        pL += orgDisVec;
                        pR += orgDisVec;
                        if (pos.haveRight)
                            InsertPort(pR, angle - Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                        if (pos.haveLeft)
                            InsertPort(pL, angle + Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                    }
                }
            }
        }
        public void InsertPort(PortModifyParam param)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                InsertPort(param.pos, param.rotateAngle, param.portWidth, param.portHeight, param.portRange, param.portAirVolume);
            };
        }
        public void InsertPort(Point3d pos, double angle, double portWidth, double portHeight, string portRange, double portAirVolume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var attr = new Dictionary<string, string> { { "风量", portAirVolume.ToString() + "m3/h" } };
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(portLayer, portName, pos, new Scale3d(), angle, attr);
                ThMEPHVACService.SetAttr(obj, attr, 0);
                textAngle = angle <= (1.5 * Math.PI) ? angle - Math.PI : angle;
                ThDuctPortsDrawService.SetPortDynBlockProperity(obj, portWidth, portHeight, portRange, textAngle, attr);
            }
        }
        public static void GetSidePortInsertPos(Vector3d dirVec, Point3d pos, double ductWidth, out Point3d pL, out Point3d pR)
        {
            var verticalLeft = ThMEPHVACService.GetLeftVerticalVec(dirVec);
            pL = pos + verticalLeft * (ductWidth * 0.5 + 100);
            var verticalRight = ThMEPHVACService.GetRightVerticalVec(dirVec);
            pR = pos + verticalRight * (ductWidth * 0.5 + 100);
        }
    }
}
