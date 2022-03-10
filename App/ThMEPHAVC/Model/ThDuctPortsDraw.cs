using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.TCH;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDraw
    {
        private double portWidth;
        private double portHeight;
        private ThDuctPortsDrawService service;
        private PortParam portParam;
        private Vector3d orgDisVec;
        private Matrix3d orgDisMat;
        public ThTCHDrawFactory tchDrawService;
        public ThDuctPortsDraw(PortParam portParam, string curDbPath)
        {
            Init(portParam);
            tchDrawService = new ThTCHDrawFactory(curDbPath, portParam.param.scenario);
        }
        private void Init(PortParam portParam)
        {
            this.portParam = portParam;
            ThMEPHVACService.GetWidthAndHeight(portParam.param.portSize, out double width, out double height);
            portWidth = width;
            portHeight = height;
            service = new ThDuctPortsDrawService(portParam.param.scenario, portParam.param.scale);
            orgDisVec = portParam.srtPoint.GetAsVector();
            orgDisMat = Matrix3d.Displacement(orgDisVec);
        }
        public void Draw(ThDuctPortsAnalysis anayRes, ref ulong gId)
        {
            if (portParam.genStyle == GenerationStyle.Auto && portParam.param.portNum > 0)
                DrawPortMark(anayRes.endLinesInfos); // DrawEndlines的DrawDimension会改变风口个数，所以先插标注

            tchDrawService.ductService.Draw(anayRes.breakedDucts, orgDisMat, true, portParam.param, ref gId);
            tchDrawService.ductService.Draw(anayRes.mainLinesInfos.Values.ToList(), orgDisMat, false, portParam.param, ref gId);
            tchDrawService.reducingService.Draw(anayRes.reducings, orgDisMat, portParam.param.mainHeight, portParam.param.elevation, ref gId);
            tchDrawService.DrawSpecialShape(anayRes.shrinkService.connectors, orgDisMat, portParam.param.mainHeight, portParam.param.elevation, ref gId);

            DrawEndlines(anayRes, ref gId);

            if (portParam.param.scenario == "消防排烟" || portParam.param.scenario == "消防补风" || portParam.param.scenario == "消防加压送风")
                service.fireValveService.InsertValves(portParam.srtPoint, anayRes.endLinesInfos, ThHvacCommon.BLOCK_VALVE_VISIBILITY_FIRE_BEC);
            else
                service.airValveService.InsertValve(portParam.srtPoint, anayRes.endLinesInfos);
            if (portParam.param.portNum == 0)
                DrawBrokenLine(anayRes.endPoints);
            if (portParam.genStyle == GenerationStyle.GenerationWithPortVolume)
                FixEndComp(anayRes.dicPlToAirVolume, anayRes.connPort);
        }

        private void FixEndComp(Dictionary<int, PortInfo> dicPlToAirVolume, List<int> connPort)
        {
            using (var adb = Linq2Acad.AcadDatabase.Active())
            {
                foreach (int code in connPort)
                {
                    var param = dicPlToAirVolume[code];
                    var b = adb.Element<BlockReference>(param.id, true);
                    b.Layer = service.portLayer;
                    if (param.effectiveName == ThHvacCommon.AI_BROKEN_LINE)
                    {
                        var s = ThMEPHVACService.GetADuctSize(param.portAirVolume, portParam.param.scenario);
                        var w = ThMEPHVACService.GetWidth(s);
                        ThDuctPortsDrawService.SetBrokenLineDynBlockProperity(param.id, w);
                    }
                }
            }
        }

        private void DrawBrokenLine(Dictionary<Point3d, Point3d> endPoints)
        {
            foreach (var p in endPoints.Keys)
            {
                var dirVec = (p - endPoints[p]).GetNormal();
                var pos = p.TransformBy(orgDisMat);
                var angle = dirVec.GetAngleTo(-Vector3d.YAxis);
                var w = ThMEPHVACService.GetWidth(portParam.param.inDuctSize);
                service.endCompService.InsertBrokenLine(pos, w, angle, portParam.param.airVolume);
            }
        }

        private void DrawEndlines(ThDuctPortsAnalysis anayRes, ref ulong gId)
        {
            if (portParam.genStyle != GenerationStyle.GenerationWithPortVolume)
            {
                DrawPort(anayRes.endLinesInfos, ref gId);
                // 画Dimension需要插入风口，所以必须先画风口再画Dimension
                service.dimService.DrawDimension(anayRes.endLinesInfos, portParam.srtPoint);
            }
        }

        private void DrawPort(List<EndlineInfo> endLinesInfos, ref ulong gId)
        {
            double avgAirVolume = portParam.param.airVolume / portParam.param.portNum;
            avgAirVolume = Math.Ceiling(avgAirVolume / 10) * 10;
            if (portParam.verticalPipeEnable)
            {
                foreach (var endline in endLinesInfos)
                {
                    foreach (var seg in endline.endlines.Values)
                    {
                        service.portService.DrawVerticalPipePorts(seg, portParam.param, orgDisVec, portWidth, portHeight, avgAirVolume, out List<SegInfo> verticalPipes);
                        tchDrawService.ductService.DrawVerticalPipe(verticalPipes, orgDisMat, portParam.param, ref gId);
                    }
                }
            }
            else
            {
                foreach (var endline in endLinesInfos)
                {
                    foreach (var seg in endline.endlines.Values)
                    {
                        service.portService.DrawPorts(seg, portParam.param, orgDisVec, portWidth, portHeight, avgAirVolume);
                    }
                }
            }
        }
        private void DrawPortMark(List<EndlineInfo> endlines)
        {
            if (endlines.Count == 0)
                return;
            var p = Point3d.Origin;
            foreach (var seg in endlines[0].endlines.Values)
            {
                if (seg.portNum > 0)
                {
                    p = seg.portsInfo[0].position;
                    var dir = ThMEPHVACService.GetEdgeDirection(seg.seg.l);
                    var leftDir = ThMEPHVACService.GetLeftVerticalVec(dir);
                    var w = ThMEPHVACService.GetWidth(seg.portsInfo[0].ductSize) * 0.5;
                    if (leftDir.X > 0)
                    {
                        p += (w * leftDir);
                    }
                    else
                    {
                        p += (-w * leftDir);
                    }
                    break;
                }
            }
            var markP = p + orgDisVec;
            service.markService.InsertMark(portParam.param, portWidth, portHeight, 0, markP);
        }
    }
}