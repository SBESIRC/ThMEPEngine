using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawPort
    {
        public string portName;
        public string portLayer;
        public ThDuctPortsDrawPort(string portLayer, string portName)
        {
            this.portName = portName;
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
                            InsertPort(pR, angle - Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume);
                        if (pos.haveLeft)
                            InsertPort(pL, angle + Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume);
                    }
                }
            }
        }
        public void InsertPort(Point3d pos, double angle, double portWidth, double portHeight, string portRange, double portAirVolume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var attNameValues = new Dictionary<string, string> { { "风量", portAirVolume.ToString() + "m3/h" } };
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(portLayer, portName, pos, new Scale3d(), angle, attNameValues);
                ThDuctPortsDrawService.SetPortDynBlockProperity(obj, portWidth, portHeight, portRange);
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
