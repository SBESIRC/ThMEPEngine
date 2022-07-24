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
        public double ucsAngle;
        public string portName;
        public string portLayer;
        public ThDuctPortsDrawPort(string portLayer, string portName, double ucsAngle)
        {
            this.portName = portName;
            this.ucsAngle = ucsAngle;
            this.portLayer = portLayer;
        }
        public void DrawVerticalPipePorts(EndlineSegInfo info,
                                          ThMEPHVACParam portParam,
                                          Vector3d orgDisVec,
                                          double portWidth,
                                          double portHeight,
                                          double avgAirVolume,
                                          out List<SegInfo> verticalPipes)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var portRange = portParam.portRange;
                var dirVec = ThMEPHVACService.GetEdgeDirection(info.seg.l);
                double angle = ThMEPHVACService.GetPortRotateAngle(dirVec);
                verticalPipes = new List<SegInfo>();
                // 立管长为风口长左右各加100
                var size =  GetVerticalPipeHeight(avgAirVolume, portWidth + 200);
                var h = size.Item2;
                var vec = Vector3d.ZAxis * ((portHeight + 100) * 0.5);
                var portSelfEleVec = vec * 2;
                var mmElevation = portParam.elevation * 1000;
                var mainHeight = ThMEPHVACService.GetHeight(portParam.inDuctSize);
                var selfEleOftVec = Vector3d.ZAxis * (portParam.portBottomEle * 1000);
                foreach (var pos in info.portsInfo)
                {
                    var ductHeight = ThMEPHVACService.GetHeight(pos.ductSize);
                    GetSidePortInsertPos(dirVec, pos.position, h, out Point3d pL, out Point3d pR);
                    InsertPort(pR + orgDisVec + selfEleOftVec, angle - Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                    InsertPort(pL + orgDisVec + selfEleOftVec, angle + Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                    var t = Math.Max(portParam.portBottomEle * 1000 - mmElevation - mainHeight, 50);
                    var sp = pos.position + (mmElevation + mainHeight) * Vector3d.ZAxis;
                    var ep = sp + (portHeight + 50 + t) * Vector3d.ZAxis;
                    verticalPipes.Add(new SegInfo()
                    {
                        l = new Line(sp, ep),
                        horizontalVec = dirVec,
                        airVolume = avgAirVolume,
                        ductSize = (portWidth + 200).ToString() + "x" + h.ToString()
                    });
                }
            }
        }

        private Tuple<double, double> GetVerticalPipeHeight(double airVolume, double verticalPipeWidth)
        {
            var selector = new ThPortParameter(airVolume, verticalPipeWidth, PortRecommendType.VERTICAL_PIPE);
            return new Tuple<double, double>(selector.DuctSizeInfor.DuctWidth, selector.DuctSizeInfor.DuctHeight);
        }

        public void DrawPorts(EndlineSegInfo info, ThMEPHVACParam portParam, Vector3d orgDisVec, double portWidth, double portHeight, double avgAirVolume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var portRange = portParam.portRange;
                var dirVec = ThMEPHVACService.GetEdgeDirection(info.seg.l);
                double angle = ThMEPHVACService.GetPortRotateAngle(dirVec);
                var mmElevation = portParam.elevation * 1000;
                var mainHeight = ThMEPHVACService.GetHeight(portParam.inDuctSize);
                foreach (var pos in info.portsInfo)
                {
                    var ductHeight = ThMEPHVACService.GetHeight(pos.ductSize);
                    var selfEleOftVec = Vector3d.ZAxis * (mmElevation + mainHeight - ductHeight);
                    if (portRange.Contains("下"))
                    {
                        var p = pos.position + orgDisVec;
                        InsertPort(p + selfEleOftVec, angle + (Math.PI * 0.5), portWidth, portHeight, portRange, avgAirVolume);
                    }
                    else
                    {
                        var curDuctW = ThMEPHVACService.GetWidth(pos.ductSize);
                        GetSidePortInsertPos(dirVec, pos.position, curDuctW, out Point3d pL, out Point3d pR);
                        pL += orgDisVec;
                        pR += orgDisVec;
                        if (pos.haveRight)
                            InsertPort(pR + selfEleOftVec, angle - Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
                        if (pos.haveLeft)
                            InsertPort(pL + selfEleOftVec, angle + Math.PI * 0.5, portWidth, portHeight, portRange, avgAirVolume * 0.5);
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
                ucsAngle = angle <= (1.5 * Math.PI) ? angle - Math.PI : angle;
                ThDuctPortsDrawService.SetPortDynBlockProperity(obj, portWidth, portHeight, portRange, ucsAngle, attr);
            }
        }

        public void InsertPort1(Point3d pos, double angle,Dictionary<string,object> dynProperties,Dictionary<string,string> attributes)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            { 
                var blkId = db.ModelSpace.ObjectId.InsertBlockReference(portLayer, portName, pos, new Scale3d(), angle, attributes);
                ThMEPHVACService.SetAttr(blkId, attributes, angle);
                ThDuctPortsDrawService.SetPortDynBlockProperity(blkId, dynProperties);
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
