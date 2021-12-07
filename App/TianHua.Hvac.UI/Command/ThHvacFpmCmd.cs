using System;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using ThMEPEngineCore.Command;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacFpmCmd : ThMEPBaseCommand, IDisposable
    {
        ThHvacCmdService cmdService;
        public ThHvacFpmCmd()
        {
            ActionName = "风平面";
            CommandName = "THFPM";
            cmdService = new ThHvacCmdService();
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            var status = GetFanParam(out bool isSelectFan,
                                     out Point3d startPoint,
                                     out PortParam portParam,
                                     out DBObjectCollection connNotRoomLines,
                                     out Dictionary<string, FanParam> dicFans,
                                     out Dictionary<string, ThDbModelFan> dicModels);
            if (portParam.param == null)
                return;
            DrawBrokenLines(portParam);
            if (!status)
                return;
            if (isSelectFan)
            {
                var knife = new ThSepereateFansDuct(startPoint, connNotRoomLines, dicFans);
                var anay = new ThFansMainDuctAnalysis(knife.mainDucts, knife.dicLineParam);
                var fanParam = GetInfo(dicFans);
                var flag = dicFans.Count > 1;
                if (flag)
                {
                    var service = new ThDuctPortsDrawService(portParam.param.scenario, portParam.param.scale);
                    ThNotRoomStartComp.DrawEndLineEndComp(ref anay.fanDucts, startPoint, portParam, service);// 先插入comp
                    DrawMultiFanMainDuct(anay, knife, startPoint, fanParam, service);           // 会改变线信息
                }
                var mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
                var wallIndex = ThMEPHVACService.CreateRoomOutlineIndex(portParam.srtPoint);
                var srtP = portParam.srtPoint;
                foreach (var key in dicFans.Keys)
                {
                    var fan = dicFans[key];
                    var model = dicModels[key];
                    var p = model.FanInletBasePoint.TransformBy(mat);
                    var wallLines = GetWalls(p, srtP, wallIndex);
                    if (model.scenario == "消防加压送风")
                        cmdService.PressurizedAirSupply(fan, model, wallLines, portParam, ref fan.bypassLines, flag);
                    else
                        cmdService.NotPressurizedAirSupply(fan, model, wallLines, portParam, flag);
                }
            }
            else
            {
                var ductPort = new ThHvacDuctPortsCmd(portParam);
                ductPort.Execute();
            }
        }
        private DBObjectCollection GetWalls(Point3d p, Point3d srtP, ThCADCoreNTSSpatialIndex index)
        {
            var detector = ThMEPHVACService.CreateDetector(p);
            var wallLines = index.SelectCrossingPolygon(detector);
            var lines = new DBObjectCollection();
            if (wallLines.Count > 0)
            {
                var a = wallLines.OfType<Entity>().SelectMany(x =>
                {
                    var obj = new DBObjectCollection();
                    x.Explode(obj);
                    return obj.Cast<Polyline>().SelectMany(y =>
                    {
                        var lineObj = new DBObjectCollection();
                        y.Explode(lineObj);
                        return lineObj.Cast<Line>();
                    });
                }).ToList();
                a.RemoveAt(a.Count() - 1);
                foreach (Line l in a)
                    lines.Add(l);
                var mat = Matrix3d.Displacement(srtP.GetAsVector());
                foreach (Line l in lines)
                    l.TransformBy(mat);
            }
            return lines;
        }

        private void DrawBrokenLines(PortParam portParam)
        {
            var mat = Matrix3d.Displacement(portParam.srtPoint.GetAsVector());
            ThDuctPortsDrawService.DrawLines(portParam.centerLines, mat, "0", out _);
        }

        private void DrawMultiFanMainDuct(ThFansMainDuctAnalysis anay, ThSepereateFansDuct knife, Point3d startPoint, FanParam fanParam, ThDuctPortsDrawService service)
        {
            var mat = Matrix3d.Displacement(startPoint.GetAsVector());
            service.DrawSpecialShape(anay.connectors, mat);
            service.DrawDuct(anay.fanDucts, mat);
            // mainHeight是anay.textAlignment中的最大值
            double mainHeight = GetMainHeight(anay.textAlignment);
            service.DrawMainDuctText(anay.textAlignment, startPoint, fanParam, mainHeight);
            service.DrawMainDuctText(knife.textAlignment, startPoint, fanParam, mainHeight);
        }

        private double GetMainHeight(List<TextAlignLine> textAlignment)
        {
            double maxW = 0;
            var maxDuctSize = String.Empty;
            foreach (var text in textAlignment)
            {
                var w = ThMEPHVACService.GetWidth(text.ductSize);
                if (maxW < w)
                {
                    maxW = w;
                    maxDuctSize = text.ductSize;
                }
            }
            return ThMEPHVACService.GetHeight(maxDuctSize);
        }

        private FanParam GetInfo(Dictionary<string, FanParam> dicFans)
        {
            foreach (var fan in dicFans.Values)
                return fan;
            throw new NotImplementedException("[checkerror]: can not find a fan record!");
        }
        private bool GetFanParam(out bool isSelectFan,
                                 out Point3d startPoint,
                                 out PortParam portParam,
                                 out DBObjectCollection connNotRoomLines,
                                 out Dictionary<string, FanParam> dicFans,
                                 out Dictionary<string, ThDbModelFan> dicModels)
        {
            using (var dlg = new fmFpm())
            {
                startPoint = Point3d.Origin;
                portParam = new PortParam();
                connNotRoomLines = new DBObjectCollection();
                dicFans = new Dictionary<string, FanParam>();
                dicModels = new Dictionary<string, ThDbModelFan>();
                isSelectFan = false;
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    isSelectFan = dlg.isSelectFan;
                    if (isSelectFan)
                    {
                        dicFans = dlg.fans;
                        dicModels = dlg.fansDic;
                        portParam = dlg.portParam;
                        startPoint = dlg.RoomStartPoint;
                        portParam.srtPoint = startPoint;
                        connNotRoomLines = dlg.connNotRoomLines;
                        return true;
                    }
                    else
                    {
                        startPoint = dlg.portParam.srtPoint;
                        portParam = dlg.portParam;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
