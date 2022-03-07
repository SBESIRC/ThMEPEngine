using System;
using System.Windows.Forms;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
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
        private static PortParam singleInstance;
        public ThHvacFpmCmd()
        {
            ActionName = "风平面";
            CommandName = "THFPM";
            cmdService = new ThHvacCmdService();
            if (singleInstance == null)
            {
                singleInstance = new PortParam();
                singleInstance.param = new ThMEPHVACParam(); ;
            }
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
                                     out Dictionary<string, ThDbModelFan> dicModels,
                                     out Dictionary<Polyline, ObjectId> allFansDic);
            if (portParam.param == null)
                return;
            DrawBrokenLines(portParam, out ObjectIdList brokenLineIds);
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
                    DrawMultiFanMainDuct(anay, knife, startPoint, fanParam, service);                        // 会改变线信息
                }
                var mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
                var wallIndex = ThMEPHVACService.CreateRoomOutlineIndex(portParam.srtPoint);
                foreach (var key in dicFans.Keys)
                {
                    var fan = dicFans[key];
                    var model = dicModels[key];
                    var p = model.FanInletBasePoint.TransformBy(mat);
                    var wallLines = GetWalls(p, wallIndex);
                    portParam.param.inDuctSize = fan.roomDuctSize;
                    if (model.scenario == "消防加压送风")
                        cmdService.PressurizedAirSupply(fan, model, wallLines, portParam, ref fan.bypassLines, flag, allFansDic);
                    else
                        cmdService.NotPressurizedAirSupply(fan, model, wallLines, portParam, flag, allFansDic);
                }
            }
            else
            {
                var ductPort = new ThHvacDuctPortsCmd(portParam, allFansDic);
                ductPort.Execute();
            }
            ThDuctPortsDrawService.ClearGraphs(brokenLineIds);
        }
        private DBObjectCollection GetWalls(Point3d p, ThCADCoreNTSSpatialIndex index)
        {
            var detector = ThMEPHVACService.CreateDetector(p);
            var res = index.SelectCrossingPolygon(detector);
            if (res.Count > 0)
                return new DBObjectCollection() { res[0] as MPolygon };
            else
                return new DBObjectCollection();
        }

        private void DrawBrokenLines(PortParam portParam, out ObjectIdList ids)
        {
            var mat = Matrix3d.Displacement(portParam.srtPoint.GetAsVector());
            ThDuctPortsDrawService.DrawLines(portParam.centerLines, mat, "0", out ids);
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
                                 out Dictionary<string, ThDbModelFan> dicModels,
                                 out Dictionary<Polyline, ObjectId> allFansDic)
        {
            using (var dlg = new fmFpm(singleInstance))
            {
                startPoint = Point3d.Origin;
                portParam = new PortParam();
                connNotRoomLines = new DBObjectCollection();
                dicFans = new Dictionary<string, FanParam>();
                dicModels = new Dictionary<string, ThDbModelFan>();
                allFansDic = new Dictionary<Polyline, ObjectId>();
                isSelectFan = false;
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    isSelectFan = dlg.isSelectFan;
                    if (isSelectFan)
                    {
                        dicFans = dlg.fans;
                        dicModels = dlg.selectFansDic;
                        portParam = dlg.portParam;
                        startPoint = dlg.RoomStartPoint;
                        portParam.srtPoint = startPoint;
                        connNotRoomLines = dlg.connNotRoomLines;
                        allFansDic = dlg.allFansDic;
                        RecordUIParam(dlg.portParam);
                        return true;
                    }
                    else
                    {
                        startPoint = dlg.portParam.srtPoint;
                        portParam = dlg.portParam;
                        allFansDic = dlg.allFansDic;
                        RecordUIParam(dlg.portParam);
                        return true;
                    }
                }
                return false;
            }
        }
        private void RecordUIParam(PortParam portParam)
        {
            singleInstance = portParam;
            singleInstance.genStyle = portParam.genStyle;
            singleInstance.endCompType = portParam.endCompType;
            singleInstance.portInterval = portParam.portInterval;
            singleInstance.verticalPipeEnable = portParam.verticalPipeEnable;
            singleInstance.param.airSpeed = portParam.param.airSpeed;
            singleInstance.param.airVolume = portParam.param.airVolume;
            singleInstance.param.elevation = portParam.param.elevation;
            singleInstance.param.highAirVolume = portParam.param.highAirVolume;
            singleInstance.param.inDuctSize = portParam.param.inDuctSize;
            singleInstance.param.mainHeight = portParam.param.mainHeight;
            singleInstance.param.portName = portParam.param.portName;
            singleInstance.param.portNum = portParam.param.portNum;
            singleInstance.param.portRange = portParam.param.portRange;
            singleInstance.param.portSize = portParam.param.portSize;
            singleInstance.param.scale = portParam.param.scale;
            singleInstance.param.scenario = portParam.param.scenario;
        }
    }
}
