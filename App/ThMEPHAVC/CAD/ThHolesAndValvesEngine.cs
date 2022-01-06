﻿using System;
using System.Linq;
using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Model;
using ThCADCore.NTS;

namespace ThMEPHVAC.CAD
{
    public class ThHolesAndValvesEngine
    {
        public List<ThValveGroup> roomValves { get; set; }
        public List<ThValveGroup> notRoomValves { get; set; }
        private Matrix3d disMat;
        private Point3d srtP;
        private ThCADCoreNTSSpatialIndex index;
        public ThHolesAndValvesEngine(ThDbModelFan fanmodel,
                                      DBObjectCollection wallobjects,
                                      DBObjectCollection bypassobjects,
                                      FanParam param,
                                      ThFanAnalysis anayRes)
        {
            double teeWidth = ThMEPHVACService.GetWidth(param.bypassSize);
            double roomWidth = ThMEPHVACService.GetWidth(param.roomDuctSize);
            double notRoomWidth = ThMEPHVACService.GetWidth(param.notRoomDuctSize);
            srtP = fanmodel.isExhaust ? fanmodel.FanInletBasePoint : fanmodel.FanOutletBasePoint;
            disMat = Matrix3d.Displacement(srtP.GetAsVector());
            index = new ThCADCoreNTSSpatialIndex(CollectCenterlines(anayRes.auxLines));
            //非送风场景 room是入风口 (送，补) room是出风口
            roomValves = GetRoomValveGroupWithoutWall(roomWidth, fanmodel);
            notRoomValves = GetNotRoomValveGroupWithoutWall(notRoomWidth, fanmodel);
            if (wallobjects.Count > 0)
            {
                roomValves.AddRange(GetFireAndHoleWithWall(fanmodel, wallobjects, bypassobjects, roomWidth, teeWidth, anayRes.roomLines, true));
                notRoomValves.AddRange(GetFireAndHoleWithWall(fanmodel, wallobjects, bypassobjects, notRoomWidth, teeWidth, anayRes.notRoomLines, false));
            }
        }
        private DBObjectCollection CollectCenterlines(List<Line> ls)
        {
            var lines = new DBObjectCollection();

            foreach (Line l in ls)
                lines.Add(l);
            return lines;
        }
        public void RunInletValvesInsertEngine()
        {
            foreach (var valvegroup in roomValves)
            {
                foreach (var model in valvegroup.ValvesInGroup)
                {
                    if (ThDuctUtils.IsHoleModel(model.ValveBlockName))
                    {
                        ThValvesAndHolesInsertEngine.InsertHole(model);
                    }
                    else
                    {
                        model.IsInlet = true;
                        ThValvesAndHolesInsertEngine.InsertValve(model);
                    }
                    ThValvesAndHolesInsertEngine.EnableValveAndHoleLayer(model);
                }
            }
        }
        public void RunOutletValvesInsertEngine()
        {
            foreach (var valvegroup in notRoomValves)
            {
                foreach (var model in valvegroup.ValvesInGroup)
                {
                    if (ThDuctUtils.IsHoleModel(model.ValveBlockName))
                    {
                        ThValvesAndHolesInsertEngine.InsertHole(model);
                    }
                    else
                    {
                        model.IsInlet = false;
                        ThValvesAndHolesInsertEngine.InsertValve(model);
                    }
                    ThValvesAndHolesInsertEngine.EnableValveAndHoleLayer(model);
                }
            }
        }
        bool IsBypass(Point3d srtP, Point3d endP, DBObjectCollection bypassLines)
        {
            if (bypassLines == null || bypassLines.Count == 0)
                return false;
            var tor = new Tolerance(1.5, 1.5);
            Line dect_line = new Line(srtP, endP);
            foreach (Line l in bypassLines)
            {
                if (ThMEPHVACService.IsSameLine(dect_line, l, tor))
                    return true;
            }
            return false;
        }
        private List<ThValveGroup> GetFireAndHoleWithWall(ThDbModelFan fanmodel,
                                                          DBObjectCollection wallobjects,
                                                          DBObjectCollection bypassobjects,
                                                          double ductWidth,
                                                          double teeWidth,
                                                          HashSet<Line> lines,
                                                          bool isRoom)
        {
            var valveGroups = new List<ThValveGroup>();
            var walllines = wallobjects.Cast<Line>();
            foreach (Line l in lines)
            {
                var dirVec = ThMEPHVACService.GetEdgeDirection(l);
                bool isBypass = IsBypass(l.StartPoint, l.EndPoint, bypassobjects);
                double width = isBypass ? teeWidth : ductWidth;
                foreach (var wallline in walllines)
                {
                    var IntersectPoints = new Point3dCollection();
                    var ex_l = ThMEPHVACService.ExtendLine(l, 1.5);
                    ex_l.IntersectWith(wallline, Intersect.OnBothOperands, IntersectPoints, new IntPtr(), new IntPtr());
                    if (IntersectPoints.Count > 0)
                    {                        
                        var vec = new Vector2d(dirVec.X, dirVec.Y);
                        var holeAngle = vec.Angle >= 0.5 * Math.PI ? vec.Angle - 0.5 * Math.PI : vec.Angle + 1.5 * Math.PI;
                        var insertP = IntersectPoints[0].TransformBy(disMat);
                        var groupparameters = new ThValveGroupParameters()
                        {
                            GroupInsertPoint = insertP,
                            DuctWidth = width,
                            RotationAngle = holeAngle,
                            FanScenario = fanmodel.scenario,
                            ValveToFanSpacing = 2000,
                        };
                        // 排烟场景room侧是入风口，非排烟场景room侧是出风口
                        var isIn = fanmodel.isExhaust ? isRoom : !isRoom;
                        var valvegroup = new ThValveGroup(groupparameters, fanmodel.isExhaust, isIn);
                        valvegroup.SetFireHoleGroup(fanmodel.Data.BlockLayer);
                        valveGroups.Add(valvegroup);
                    }
                }
            }
            return valveGroups;
        }
        private void UpdateSrtVec(ref Vector3d inVec, ref Vector3d outVec)
        {
            if (inVec.IsEqualTo(Point3d.Origin.GetAsVector()) && outVec.IsEqualTo(Point3d.Origin.GetAsVector()))
            {
                return ;
            }
            // 如果起始为0向量，与另一边反向(仅能处理in out共线时)
            if (inVec.IsEqualTo(Point3d.Origin.GetAsVector()))
                inVec = -outVec;
            if (inVec.IsEqualTo(Point3d.Origin.GetAsVector()))
                outVec = -inVec;
        }
        private List<ThValveGroup> GetNotRoomValveGroupWithoutWall(double notRoomWidth, ThDbModelFan fanmodel)
        {
            var valvegroups = new List<ThValveGroup>();
            var inVec = GetDir(fanmodel.FanInletBasePoint);
            var outVec = GetDir(fanmodel.FanOutletBasePoint);
            if (inVec.IsEqualTo(Point3d.Origin.GetAsVector()) && outVec.IsEqualTo(Point3d.Origin.GetAsVector()))
            {
                ThMEPHVACService.PromptMsg("[CheckError]: Can not find start vec!");
                return new List<ThValveGroup>();
            }
            var dirVec = (!fanmodel.isExhaust) ? inVec : outVec;
            var vec = new Vector2d(dirVec.X, dirVec.Y);
            var holeAngle = vec.Angle >= 0.5 * Math.PI ? vec.Angle - 0.5 * Math.PI : vec.Angle + 1.5 * Math.PI;
            var insertP = (!fanmodel.isExhaust) ? fanmodel.FanInletBasePoint : fanmodel.FanOutletBasePoint;
            var fanWidth = (!fanmodel.isExhaust) ? fanmodel.fanInWidth : fanmodel.fanOutWidth;
            var param = new ThValveGroupParameters()
            {
                GroupInsertPoint = insertP,
                DuctWidth = notRoomWidth,
                RotationAngle = holeAngle,
                FanScenario = fanmodel.scenario,
                ValveToFanSpacing = 2000,       // remain enough spaces
            };
            var startOft = CalcStartOft(fanWidth, notRoomWidth, fanmodel, false);
            // 非服务侧，排风风机对应出风口，非排风风机对应入风口
            var isIn = fanmodel.isExhaust ? false : true;
            var valveGroup = new ThValveGroup(param, fanmodel.Data.BlockLayer, isIn, fanmodel.isExhaust);
            CorrectValveOft(valveGroup, dirVec, startOft);
            valvegroups.Add(valveGroup);
            return valvegroups;
        }
        private Vector3d GetDir(Point3d p)
        {
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            var movep = p.TransformBy(mat);
            var pl = ThMEPHVACService.CreateDetector(movep);
            var res = index.SelectCrossingPolygon(pl);
            if (res.Count != 1)
                return Point3d.Origin.GetAsVector();
            var l = res[0] as Line;
            var otherP = ThMEPHVACService.GetOtherPoint(l, movep, new Tolerance(1.5, 1.5));
            return (otherP - movep).GetNormal();
        }
        private List<ThValveGroup> GetRoomValveGroupWithoutWall(double roomWidth, ThDbModelFan fanmodel)
        {
            var valvegroups = new List<ThValveGroup>();
            var inVec = GetDir(fanmodel.FanInletBasePoint);
            var outVec = GetDir(fanmodel.FanOutletBasePoint);
            if (inVec.IsEqualTo(Point3d.Origin.GetAsVector()) && outVec.IsEqualTo(Point3d.Origin.GetAsVector()))
            {
                ThMEPHVACService.PromptMsg("[CheckError]: Can not find start vec!");
                return new List<ThValveGroup>();
            }
            var dirVec = (fanmodel.isExhaust) ? inVec : outVec;
            var vec = new Vector2d(dirVec.X, dirVec.Y);
            var holeAngle = vec.Angle >= 0.5 * Math.PI ? vec.Angle - 0.5 * Math.PI : vec.Angle + 1.5 * Math.PI;
            var insertP = (fanmodel.isExhaust) ? fanmodel.FanInletBasePoint : fanmodel.FanOutletBasePoint;
            var fanWidth = (fanmodel.isExhaust) ? fanmodel.fanInWidth : fanmodel.fanOutWidth;
            var groupparameters = new ThValveGroupParameters()
            {
                GroupInsertPoint = insertP,
                DuctWidth = roomWidth,
                RotationAngle = holeAngle,
                FanScenario = fanmodel.scenario,
                ValveToFanSpacing = 2000,       // remain enough spaces
            };
            var startOft = CalcStartOft(fanWidth, roomWidth, fanmodel, true);
            // 服务侧，排风风机对应入风口，非排风风机对应出风口
            var isIn = fanmodel.isExhaust ? true : false;
            var valvegroup = new ThValveGroup(groupparameters, fanmodel.Data.BlockLayer, isIn, fanmodel.isExhaust);
            CorrectValveOft(valvegroup, dirVec, startOft);
            valvegroups.Add(valvegroup);
            return valvegroups;
        }
        private double CalcStartOft(double fanWidth, double ductWidth, ThDbModelFan fan, bool isRoom)
        {
            var scenario = fan.scenario;
            var intakeform = fan.IntakeForm;
            var flag = (scenario == "消防补风" || scenario == "消防排烟" || scenario == "消防加压送风");
            // 500 预留阀到上下翻的距离
            // 排风风机进风口是room侧
            var fanFlag = isRoom ? fan.isExhaust : !fan.isExhaust;
            if (fanFlag)
            {
                return (intakeform.Contains("上进") || intakeform.Contains("下进")) ?
                        500 : CalcReducingLen(flag, fanWidth, ductWidth);
            }
            else
            {
                return (intakeform.Contains("上出") || intakeform.Contains("下出")) ?
                        500 : CalcReducingLen(flag, fanWidth, ductWidth);
            }
            throw new NotImplementedException("[CheckError]: ValveGroupPosionType ");
        }
        private double CalcReducingLen(bool flag, double fanWidth, double ductWidth)
        {
            var reducingLen = ThDuctPortsShapeService.GetReducingLen(fanWidth, ductWidth);
            if (reducingLen > 200)
            {
                var hoseLen = flag ? 0 : 200;
                reducingLen -= hoseLen;// ThFanAnalysis.cs Line: 609
                if (reducingLen < 200)
                    reducingLen = 200;
            }
            if (!flag)
                reducingLen += 200;// 软接本身长度
            return reducingLen;
        }
        private void CorrectValveOft(ThValveGroup valvegroup, Vector3d dirVec, double startOft)
        {
            double len = 0;
            var hole = new ThValve();
            var fire = new ThValve();
            foreach (var v in valvegroup.ValvesInGroup)
            {
                if (v.ValveBlockName == "洞口")
                    hole = v;
                else
                {
                    len += v.Length;
                    if (v.ValveBlockName == "防火阀")
                        fire = v;
                }                    
            }
            valvegroup.ValvesInGroup.Remove(hole);
            valvegroup.ValvesInGroup.Remove(fire);
            len += startOft;
            foreach (var v in valvegroup.ValvesInGroup)
                v.ValvePosition += (dirVec * len);
        }
    }
}
