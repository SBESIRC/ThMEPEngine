using System;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

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
        public ThDuctPortsDraw(PortParam portParam)
        {
            Init(portParam);
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
        public void Draw(ThDuctPortsAnalysis anayRes)
        {
            if (portParam.genStyle == GenerationStyle.Auto && portParam.param.portNum > 0)
                DrawPortMark(anayRes.endLinesInfos); // DrawEndlines的DrawDimension会改变风口个数，所以先插标注
            DrawEndlines(anayRes);
            DrawMainlines(anayRes.mainLinesInfos);
            service.DrawSpecialShape(anayRes.shrinkService.connectors, orgDisMat);
            
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

        private void DrawEndlines(ThDuctPortsAnalysis anayRes)
        {
            service.DrawDuct(anayRes.breakedDucts, orgDisMat);
            service.DrawReducing(anayRes.reducings, orgDisMat);
            service.DrawSideDuctText(anayRes.textAlignment, portParam.srtPoint, portParam.param);
            if (portParam.genStyle != GenerationStyle.GenerationWithPortVolume)
            {
                DrawPort(anayRes.endLinesInfos);
                // 画Dimension需要插入风口，所以必须先画风口再画Dimension
                service.dimService.DrawDimension(anayRes.endLinesInfos, portParam.srtPoint);
            }
        }

        private void DrawPort(List<EndlineInfo> endLinesInfos)
        {
            double avgAirVolume = portParam.param.airVolume / portParam.param.portNum;
            avgAirVolume = (Math.Ceiling(avgAirVolume / 10)) * 10;
            foreach (var endline in endLinesInfos)
            {
                foreach (var seg in endline.endlines.Values)
                {
                    service.portService.DrawPorts(seg, portParam.param.portRange, orgDisVec, portWidth, portHeight, avgAirVolume);
                }
            }
        }
        private void DrawPortMark(List<EndlineInfo> endlines)
        {
            if (endlines.Count == 0)
                return;
            var p = Point3d.Origin;
            double textAngle = 0;
            foreach (var seg in endlines[0].endlines.Values)
            {
                if (seg.portNum > 0)
                {
                    p = seg.portsInfo[0].position;
                    var dir = ThMEPHVACService.GetEdgeDirection(seg.seg.l);
                    textAngle = ThMEPHVACService.GetPortRotateAngle(dir);
                    break;
                }
            }
            var markP = p + orgDisVec;
            service.markService.InsertMark(portParam.param, portWidth, portHeight, textAngle - 0.5 * Math.PI, markP);
            //service.markService.InsertLeader(p + orgDisVec, markP);
        }
        private void DrawMainlines(Dictionary<int, SegInfo> mainLinesInfos)
        {
            foreach (var info in mainLinesInfos.Values)
            {
                var l = info.GetShrinkedLine();
                var mainlines = GetMainDuct(info);
                if (mainlines.centerLines.Count < 1)
                    continue;// 管长太小
                ThMEPHVACService.GetLinePosInfo(l, out double angle, out Point3d centerPoint);
                var mat = Matrix3d.Displacement(centerPoint.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat = orgDisMat * mat;
                ThDuctPortsDrawService.DrawLines(mainlines.geo, mat, service.geoLayer, out ObjectIdList geoIds);
                ThDuctPortsDrawService.DrawLines(mainlines.flg, mat, service.geoLayer, out ObjectIdList flgIds);
                ThDuctPortsDrawService.DrawLines(mainlines.centerLines, orgDisMat, service.centerLayer, out ObjectIdList centerIds);
                // port根据中心线变化
                var elevation = portParam.param.elevation.ToString();
                double airVolume = ThMEPHVACService.RoundNum(info.airVolume, 50);
                var param = ThMEPHVACService.CreateDuctModifyParam(mainlines.centerLines, info.ductSize, elevation, airVolume);
                ThDuctPortsRecoder.CreateDuctGroup(geoIds, flgIds, centerIds, param);
                var dirVec = (l.EndPoint - l.StartPoint).GetNormal();
                service.textService.DrawMainlineTextInfo(angle, info.ductSize, centerPoint, dirVec, orgDisMat, portParam);
            }
        }
        private LineGeoInfo GetMainDuct(SegInfo info)
        {
            var l = info.GetShrinkedLine();
            if (l.Length < 10)
                return new LineGeoInfo();
            var ductWidth = ThMEPHVACService.GetWidth(info.ductSize);
            var outlines = ThDuctPortsFactory.CreateDuct(l.Length, ductWidth);
            var centerLine = new DBObjectCollection { l };
            var outline1 = outlines[0] as Line;
            var outline2 = outlines[1] as Line;
            var flg = new DBObjectCollection{new Line(outline1.StartPoint, outline2.StartPoint),
                                             new Line(outline1.EndPoint, outline2.EndPoint)};
            return new LineGeoInfo(outlines, flg, centerLine);
        }
    }
}