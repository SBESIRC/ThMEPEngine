using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using ThMEPEngineCore.Command;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcHelper;
using ThMEPHVAC.TCH;
using Autodesk.AutoCAD.Runtime;

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
            string curDbPath = Path.GetTempPath() + "TG20.db";
            string templateDbPath = ThCADCommon.TCHHVACDBPath();
            ulong gId = 0;
            ThHvacCmdService.InitTables(curDbPath, templateDbPath, ref gId);
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
            var service = new ThDuctPortsDrawService(portParam.param.scenario, portParam.param.scale);
            if (isSelectFan)
            {
                var knife = new ThSepereateFansDuct(startPoint, connNotRoomLines, dicFans);
                var anay = new ThFansMainDuctAnalysis(knife.mainDucts, knife.dicLineParam);
                var fanParam = GetInfo(dicFans);
                var flag = dicFans.Count > 1;
                if (flag)
                {
                    ThNotRoomStartComp.DrawEndLineEndComp(ref anay.fanDucts, startPoint, portParam, service);// 先插入comp
                    DrawMultiFanMainDuct(ref gId, anay, startPoint, fanParam, curDbPath, portParam);                        // 会改变线信息
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
                        cmdService.PressurizedAirSupply(ref gId, curDbPath, fan, model, wallLines, portParam, flag, allFansDic, brokenLineIds, service);
                    else
                        cmdService.NotPressurizedAirSupply(ref gId, curDbPath, fan, model, wallLines, portParam, flag, allFansDic, brokenLineIds, service);
                }
            }
            else
            {
                var ductPort = new ThHvacDuctPortsCmd(curDbPath, portParam, allFansDic, service);
                ductPort.Execute(ref gId);
            }
            ThDuctPortsDrawService.ClearGraphs(brokenLineIds);
#if ACAD_ABOVE_2014
            Active.Editor.Command("TIMPORTTG20HVAC", curDbPath, " ");
#else
            ResultBuffer args = new ResultBuffer(
               new TypedValue((int)LispDataType.Text, "_.TIMPORTTG20HVAC"),
               new TypedValue((int)LispDataType.Text, curDbPath),
               new TypedValue((int)LispDataType.Text, " "));
            Active.Editor.AcedCmd(args);
#endif
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

        private void DrawMultiFanMainDuct(ref ulong gId, ThFansMainDuctAnalysis anay, Point3d startPoint, FanParam fanParam, string curDbPath, PortParam portParam)
        {
            // 非服务侧
            var tchDrawService = new ThTCHDrawFactory(curDbPath, fanParam.scenario);
            var mat = Matrix3d.Displacement(startPoint.GetAsVector());
            var mainHeight = ThMEPHVACService.GetHeight(fanParam.notRoomDuctSize);
            var elevation = Double.Parse(fanParam.notRoomElevation);
            tchDrawService.DrawSpecialShape(anay.connectors, mat, mainHeight, elevation, ref gId);
            tchDrawService.ductService.DrawDuct(anay.fanDucts, mat, false, portParam.param, ref gId);
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
                        RecordUIParam(dlg.portParam, dicFans.Values.First().notRoomDuctSize);
                        return true;
                    }
                    else
                    {
                        startPoint = dlg.portParam.srtPoint;
                        portParam = dlg.portParam;
                        allFansDic = dlg.allFansDic;
                        RecordUIParam(dlg.portParam, String.Empty);
                        return true;
                    }
                }
                return false;
            }
        }
        private void RecordUIParam(PortParam portParam, string notRoomDuctSize)
        {
            singleInstance.genStyle = portParam.genStyle;
            singleInstance.endCompType = portParam.endCompType;
            singleInstance.portInterval = portParam.portInterval;
            singleInstance.verticalPipeEnable = portParam.verticalPipeEnable;
            singleInstance.textAirVolume = portParam.textAirVolume;
            singleInstance.param.airSpeed = portParam.param.airSpeed;
            singleInstance.param.airVolume = portParam.param.airVolume;
            singleInstance.param.elevation = portParam.param.elevation;
            singleInstance.param.highAirVolume = portParam.param.highAirVolume;
            singleInstance.param.inDuctSize = portParam.param.inDuctSize;
            singleInstance.param.mainHeight = portParam.param.mainHeight;
            singleInstance.param.portName = portParam.param.portName;
            singleInstance.param.portRange = portParam.param.portRange;
            singleInstance.param.portNum = portParam.param.portNum;
            singleInstance.param.portSize = portParam.param.portSize;
            singleInstance.param.scale = portParam.param.scale;
            singleInstance.param.scenario = portParam.param.scenario;
            
            if (portParam.param.portRange.Contains("侧"))
                singleInstance.param.portNum *= 2;
        }
    }
}
