using System;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.CAD;

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
        public void DrawPorts(EndlineSegInfo info, 
                              PortParam portParam, 
                              Vector3d orgDisVec, 
                              double portWidth, 
                              double portHeight, 
                              double avgAirVolume,
                              out List<SegInfo> verticalPipes)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var portRange = portParam.param.portRange;
                var dirVec = ThMEPHVACService.GetEdgeDirection(info.seg.l);
                double angle = ThMEPHVACService.GetPortRotateAngle(dirVec);
                verticalPipes = new List<SegInfo>();
                var h = GetVerticalPipeHeight(avgAirVolume);
                var vec = Vector3d.ZAxis * ((h + 100) * 0.5);
                foreach (var pos in info.portsInfo)
                {
                    if (portParam.verticalPipeEnable)
                    {
                        GetSidePortInsertPos(dirVec, pos.position, h, out Point3d pL, out Point3d pR);
                        InsertPort(pR + orgDisVec, angle - Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                        InsertPort(pL + orgDisVec, angle + Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                        var sp = pos.position - vec;
                        var ep = pos.position + vec;
                        verticalPipes.Add(new SegInfo()
                        {
                            l = new Line(sp, ep),
                            horizontalVec = dirVec,
                            airVolume = avgAirVolume,
                            ductSize = (portWidth + 200).ToString() + "x" + h.ToString()
                        });
                    }
                    else
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
        }
        private double GetVerticalPipeHeight(double airVolume)
        {
            var selector = new ThPortParameter(airVolume);
            return selector.DuctSizeInfor.DuctHeight;
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
