﻿using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacCmdService
    {
        public bool isIntegrate = true;
        public double ioBypassSepDis = 30;
        public ThHvacCmdService() { }
        public List<ObjectId> GetFans()
        {
            using (var db = AcadDatabase.Active())
            {
                var fanIds = new List<ObjectId>();
                var objIds = GetFromPrompt("请选择风机", false);
                foreach (ObjectId id in objIds)
                {
                    var obj = id.GetDBObject();
                    if (obj.IsRawModel())
                        fanIds.Add(id);
                }
                return fanIds;
            }
        }
        public ObjectIdCollection GetFromPrompt(string prompt, bool only_able)
        {
            var options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                SingleOnly = only_able
            };
            var result = Active.Editor.GetSelection(options);
            if (result.Status == PromptStatus.OK)
            {
                return result.Value.GetObjectIds().ToObjectIdCollection();
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
        public DBObjectCollection GetBypass()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objIds = GetFromPrompt("请选择旁通管", false);
                if (objIds.Count == 0)
                    return new DBObjectCollection();
                var bypass = objIds.Cast<ObjectId>().Select(o => o.GetDBObject().Clone() as Curve).ToCollection();
                return ThMEPHVACLineProc.Explode(bypass);
            }
        }
        public void ProcBypass(string teePattern, double type3SepDis, ref DBObjectCollection bypass, out Line maxBypassLine)
        {
            maxBypassLine = ThMEPHVACService.GetMaxLine(bypass);
            // 给较长的线段上插点
            if (teePattern == "RBType3")
                CutMaxBypass(ref bypass, maxBypassLine, type3SepDis);//修改线集里的旁通
        }
        public void CutMaxBypass(ref DBObjectCollection bypass, Line maxBypass, double type3SepDis)
        {
            bypass.Remove(maxBypass);
            double shrinkLen = type3SepDis * 0.5;
            var dir_vec = ThMEPHVACService.GetEdgeDirection(maxBypass);
            var mid_p = ThMEPHVACService.GetMidPoint(maxBypass);
            var p = mid_p - shrinkLen * dir_vec;
            bypass.Add(new Line(maxBypass.StartPoint, p));
            p = mid_p + shrinkLen * dir_vec;
            bypass.Add(new Line(p, maxBypass.EndPoint));
        }
        public bool CheckoutInput(string teePattern, DBObjectCollection bypassLines, DBObjectCollection centerLines)
        {
            if ((teePattern != "RBType4" && teePattern != "RBType5") && bypassLines.Count == 0)
            {
                ThMEPHVACService.PromptMsg("未选择旁通旁通管");
                return false;
            }
            if (bypassLines.Polygonize().Count > 1)
            {
                ThMEPHVACService.PromptMsg("旁通线闭合");
                return false;
            }
            if (centerLines.Polygonize().Count > 1)
            {
                ThMEPHVACService.PromptMsg("中心线闭合");
                return false;
            }
            return true;
        }
        public void NotPressurizedAirSupply(FanParam fanParam, 
                                            ThDbModelFan fan,
                                            DBObjectCollection wallLines, 
                                            PortParam portParam, 
                                            bool haveMultiFan,
                                            Dictionary<Polyline, ObjectId> allFansDic)
        {
            var bypassLines = new DBObjectCollection();
            var anayRes = new ThFanAnalysis(ioBypassSepDis, fan, fanParam, portParam, bypassLines, wallLines, haveMultiFan);
            if (anayRes.centerLines.Count == 0)
                return;
            var valveHole = new ThHolesAndValvesEngine(fan, wallLines, bypassLines, fanParam, anayRes);
            InsertValve(fan.isExhaust, fanParam.roomEnable, fanParam.notRoomEnable, valveHole);
            _ = new ThFanDraw(anayRes, fanParam.roomEnable, fanParam.notRoomEnable);
            if (isIntegrate)
            {
                var srtP = portParam.srtPoint;
                TransFanParamToPortParam(portParam, fanParam, srtP, anayRes.auxLines[0]);
                var ductPort = new ThHvacDuctPortsCmd(portParam, allFansDic);
                ductPort.Execute();
            }
        }

        public void PressurizedAirSupply(FanParam fanParam,
                                         ThDbModelFan fan,
                                         DBObjectCollection wallLines,
                                         PortParam portParam,
                                         ref DBObjectCollection bypassLines,
                                         bool haveMultiFan,
                                         Dictionary<Polyline, ObjectId> allFansDic)

        {
            ProcBypass(fanParam.bypassPattern, ioBypassSepDis, ref bypassLines, out Line maxBypass);
            if (!CheckoutInput(fanParam.bypassPattern, bypassLines, fanParam.centerLines))
                return;
            var anayRes = new ThFanAnalysis(ioBypassSepDis, fan, fanParam, portParam, bypassLines, wallLines, haveMultiFan);
            if (anayRes.centerLines.Count == 0)
            {
                ThMEPHVACService.PromptMsg("未搜索到与风机相连的中心线");
                return;
            }
            RecordBypassAlignmentLine(maxBypass, fanParam, fan, bypassLines, anayRes.textRoomAlignment, anayRes.moveSrtP);
            // 先画阀，pinter会移动中心线导致墙线与中心线交不上
            var valveHole = new ThHolesAndValvesEngine(fan, wallLines, bypassLines, fanParam, anayRes);
            InsertValve(fan.isExhaust, fanParam.roomEnable, fanParam.notRoomEnable, valveHole);
            var pinter = new ThFanDraw(anayRes, fanParam.roomEnable, fanParam.notRoomEnable);
            InsertElectricValve(fanParam, fan, maxBypass, pinter);
            if (fanParam.bypassPattern == "RBType4" || fanParam.bypassPattern == "RBType5")
            {
                var vtPinter = new ThDrawVBypass(fan.airVolume, fanParam.scale, fan.scenario, anayRes.moveSrtP, fanParam.bypassSize, fanParam.roomElevation);
                if (fanParam.bypassPattern == "RBType4")
                    vtPinter.Draw4VerticalBypass(anayRes.vt.vtElbow, anayRes.inVtPos, anayRes.outVtPos);
                else
                    vtPinter.Draw5VerticalBypass(anayRes.vt.vtElbow, anayRes.inVtPos, anayRes.outVtPos);
            }
            if (isIntegrate)
            {
                var srtP = portParam.srtPoint;
                TransFanParamToPortParam(portParam, fanParam, srtP, anayRes.auxLines[0]);
                var ductPort = new ThHvacDuctPortsCmd(portParam, allFansDic);
                ductPort.Execute();
            }
        }

        private void TransFanParamToPortParam(PortParam portParam, FanParam fanParam, Point3d srtP, Line realSrtOftLine)
        {
            portParam.srtPoint = srtP;
            portParam.param.airSpeed = fanParam.airSpeed;
            portParam.param.airVolume = fanParam.airVolume;
            portParam.param.highAirVolume = fanParam.airHighVolume;
            portParam.param.portNum = fanParam.portNum;
            portParam.param.portName = fanParam.portName;
            portParam.param.portSize = fanParam.portSize;
            portParam.param.portRange = fanParam.portRange;
            portParam.portInterval = fanParam.portInterval;
            portParam.centerLines = new DBObjectCollection();
            foreach (Line l in fanParam.centerLines)
                portParam.centerLines.Add(l);
            if (realSrtOftLine.Length > 0)
            {
                // 找到起始线并更新
                var tor = new Tolerance(1.5, 1.5);
                foreach (Line l in portParam.centerLines)
                { 
                    if (l.StartPoint.IsEqualTo(realSrtOftLine.EndPoint, tor))
                    {
                        portParam.centerLines.Remove(l);
                        var dirVec = ThMEPHVACService.GetEdgeDirection(l);
                        var disVec = dirVec * realSrtOftLine.Length;
                        var newSrtP = Point3d.Origin + disVec;
                        portParam.centerLines.Add(new Line(newSrtP, l.EndPoint));
                        portParam.srtDisVec = disVec;
                        break;
                    }
                }
            }
        }

        public static Point3d? GetPointFromPrompt(string prompt, out Matrix3d ucsMat)
        {
            var startRes = Active.Editor.GetPoint(prompt);
            ucsMat = Active.Editor.CurrentUserCoordinateSystem;
            if (startRes.Status==PromptStatus.OK)
            {
                var wcsPt = startRes.Value.TransformBy(ucsMat);
                return new Point3d(wcsPt.X, wcsPt.Y, 0);
            }
            else
            {
                return null;
            }
        }
        private void InsertValve(bool isExhaust, bool roomEnable, bool notRoomEnable, ThHolesAndValvesEngine valve_hole)
        {
            if (isExhaust)
            {
                if (roomEnable && !notRoomEnable)
                    valve_hole.RunInletValvesInsertEngine();
                else if (!roomEnable && notRoomEnable)
                    valve_hole.RunOutletValvesInsertEngine();
                else
                {
                    valve_hole.RunInletValvesInsertEngine();
                    valve_hole.RunOutletValvesInsertEngine();
                }
            }
            else
            {
                if (roomEnable && !notRoomEnable)
                    valve_hole.RunOutletValvesInsertEngine();
                else if (!roomEnable && notRoomEnable)
                    valve_hole.RunInletValvesInsertEngine();
                else
                {
                    valve_hole.RunInletValvesInsertEngine();
                    valve_hole.RunOutletValvesInsertEngine();
                }
            }
        }
        private void RecordBypassAlignmentLine(Line maxBypass,
                                               FanParam param,
                                               ThDbModelFan fan,
                                               DBObjectCollection bypass,
                                               List<TextAlignLine> textAlignment,
                                               Point3d moveSrtP)
        {
            if (param.bypassSize == null)
                return;
            var dis_mat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            if (bypass.Count == 0)
            {
                var p1 = fan.FanInletBasePoint.TransformBy(dis_mat);
                var p2 = fan.FanOutletBasePoint.TransformBy(dis_mat);
                textAlignment.Add(new TextAlignLine() { l = new Line(p1, p2) , ductSize = param.bypassSize , isRoom = true });
            }
            else
            {
                if (param.bypassPattern == "RBType3")
                {
                    maxBypass.TransformBy(dis_mat);
                }
                textAlignment.Add(new TextAlignLine() { l = maxBypass, ductSize = param.bypassSize , isRoom = true });
            }
        }
        private void InsertElectricValve(FanParam param, ThDbModelFan fan, Line maxBypass, ThFanDraw pinter)
        {
            var dirVec = (param.bypassPattern == "RBType4" || param.bypassPattern == "RBType5") ?
                            (fan.FanOutletBasePoint - fan.FanInletBasePoint).GetNormal() :
                            ThMEPHVACService.GetEdgeDirection(maxBypass);
            var angle = dirVec.GetAngleTo(-Vector3d.XAxis);
            var z = dirVec.CrossProduct(-Vector3d.XAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z > 0)
                angle = Math.PI * 2 - angle;
            var l = (param.bypassPattern == "RBType4" || param.bypassPattern == "RBType5") ?
                     new Line(fan.FanOutletBasePoint, fan.FanInletBasePoint) : maxBypass;
            var insertP = ThMEPHVACService.GetMidPoint(l);
            var width = ThMEPHVACService.GetWidth(param.bypassSize);
            pinter.InsertElectricValve(insertP.GetAsVector(), width, angle + 0.5 * Math.PI);
        }
    }
}
