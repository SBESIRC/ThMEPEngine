﻿using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Algorithm;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Hvac;
using NFox.Cad;
using ThCADExtension;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReadComponent
    {
        public static Dictionary<Polyline, Handle> ReadAllComponent(Point3d srtP)
        {
            using (var db = AcadDatabase.Active())
            {
                var bounds2IdDic = new Dictionary<Polyline, Handle>();
                var connector = ReadGroupId2geoDic(srtP);
                foreach (var c in connector)
                    bounds2IdDic.Add(c.Value, c.Key.Handle);
                GetDuctBounds(srtP, out _, out Dictionary<Polyline, DuctModifyParam> ductsDic);
                foreach (var d in ductsDic)
                    bounds2IdDic.Add(d.Key, d.Value.handle);
                return bounds2IdDic;
            }
        }
        public static Dictionary<ObjectId, Polyline> ReadGroupId2geoDic(Point3d srtP)
        {
            using (var db = AcadDatabase.Active())
            {
                var dic = new Dictionary<ObjectId, Polyline>();
                var groups = db.Groups;
                var mat = Matrix3d.Displacement(-srtP.GetAsVector());
                foreach (var g in groups)
                {
                    var id = g.ObjectId;
                    var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (list != null)
                    {
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type == "Tee" || type == "Cross" || type == "Reducing" || type == "Elbow")
                        {
                            var entitys = GetGroupEntitys(g);
                            var pl = ThMEPHAVCBounds.GetConnectorBounds(entitys, 1);
                            if (pl.Bounds != null)
                            {
                                pl.TransformBy(mat);
                                dic.Add(id, pl);
                            }
                        }
                    }
                }
                return dic;
            }
        }
        private static DBObjectCollection GetGroupEntitys(Group g)
        {
            var entityIds = g.GetAllEntityIds();
            var entitys = new DBObjectCollection();
            foreach (var eId in entityIds)
            {
                var e = eId.GetDBObject();
                if (!(e is Line))
                    continue;
                entitys.Add(e);
            }
            return entitys;
        }
        public static List<ObjectId> ReadGroupIdsByType(string range)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = new List<ObjectId>();
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var id = g.ObjectId;
                    var current = db.Element<Group>(g.ObjectId);
                    var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (list != null)
                    {
                        var allEntityIds = g.GetAllEntityIds();
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type == range)
                            ids.Add(id);
                    }
                }
                return ids;
            }
        }

        public static List<ObjectId> ReadPortComponents()
        {
            var components = new List<ObjectId>();
            components.AddRange(ReadBlkIdsByName(ThHvacCommon.AI_PORT));
            components.AddRange(ReadBlkIdsByName(ThHvacCommon.AI_BROKEN_LINE));
            components.AddRange(ReadBlkIdsByName(ThHvacCommon.AI_VERTICAL_PIPE));
            return components;
        }

        private static List<BlockReference> ReadBlkByName(string blkName)
        {
            using (var db = AcadDatabase.Active())
            {
                return db.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => IsBlkByName(b, blkName)).ToList();
            }
        }
        public static string GetEffectiveBlkByName(BlockReference blockReference)
        {
            using (var db = AcadDatabase.Active())
            {
                if (blockReference.BlockTableRecord.IsNull)
                {
                    return string.Empty;
                }

                string name;
                if (blockReference.DynamicBlockTableRecord.IsValid)
                {
                    name = db.Element<BlockTableRecord>(blockReference.DynamicBlockTableRecord).Name;
                }
                else
                {
                    name = blockReference.Name;
                }

                return name;
            }
        }
        private static bool IsBlkByName(BlockReference blockReference, string blkName)
        {
            using (var db = AcadDatabase.Active())
            {
                if (blockReference.BlockTableRecord.IsNull)
                {
                    return false;
                }

                string name;
                if (blockReference.DynamicBlockTableRecord.IsValid)
                {
                    name = db.Element<BlockTableRecord>(blockReference.DynamicBlockTableRecord).Name;
                }
                else
                {
                    name = blockReference.Name;
                }

                return name.Contains(blkName);
            }
        }

        public static List<ObjectId> ReadBlkIdsByName(string blkName)
        {
            return ReadBlkByName(blkName).Select(o => o.ObjectId).ToList();
        }
        public static List<DBText> ReadDuctTexts()
        {
            var texts = new List<DBText>();
            string reg_size = "[0-9]{3,4}x[0-9]{3,4}";
            using (var db = AcadDatabase.Active())
            {
                var dbTexts = db.ModelSpace.OfType<DBText>();
                foreach (var t in dbTexts)
                {
                    if (System.Text.RegularExpressions.Regex.Match(t.TextString, reg_size).Success ||
                        (t.TextString.Contains("h") && t.TextString.Contains("m")))
                        texts.Add(t);
                }
            }
            return texts;
        }
        public static List<AlignedDimension> ReadDimension()
        {
            using (var db = AcadDatabase.Active())
            {
                var dimensions = new List<AlignedDimension>();
                var dims = db.ModelSpace.OfType<AlignedDimension>();
                foreach (var d in dims)
                {
                    dimensions.Add(d);
                }
                return dimensions;
            }
        }
        public static List<Leader> ReadLeader()
        {
            using (var db = AcadDatabase.Active())
            {
                var leaders = new List<Leader>();
                var leads = db.ModelSpace.OfType<Leader>();
                foreach (var l in leads)
                    leaders.Add(l);
                return leaders;
            }
        }
        public static Dictionary<string, Point3d> GetBypassPortsOfGroup(ObjectId groupId)
        {
            var rst = new Dictionary<string, Point3d>();

            var id2GroupDic = GetObjectId2GroupDic();
            if (!id2GroupDic.ContainsKey(groupId))
                return rst;
            var groupObj = id2GroupDic[groupId];
            var allObjIdsInGroup = groupObj.GetAllEntityIds();
            using (var db = AcadDatabase.Active())
            {
                foreach (var id in allObjIdsInGroup)
                {
                    var entity = db.Element<Entity>(id);
                    if (!(entity is DBPoint))
                        continue;
                    var ptXdata = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (ptXdata != null)
                    {
                        var values = ptXdata.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type != "Port")
                            continue;
                        var portIndex = (string)values.ElementAt(1).Value; // 0, 1 ,2
                        rst.Add(portIndex, (entity as DBPoint).Position);
                    }
                }
            }
            return rst;
        }

        public static Dictionary<ObjectId, Group> GetObjectId2GroupDic()
        {
            var dic = new Dictionary<ObjectId, Group>();
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var id = g.ObjectId;
                    var info = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (info != null)
                        dic.Add(id, g);
                }
            }
            return dic;
        }

        public static ObjectId GetGroupIdsBySubEntityId(ObjectId subObjId)
        {
            var dic = GetObjectId2GroupDic();
            foreach (var g in dic.Values)
            {
                var subEntityIds = g.GetAllEntityIds();
                if (subEntityIds.Contains(subObjId))
                {
                    return g.ObjectId;
                }
            }
            return ObjectId.Null;
        }
        public static int GetColor(DBObjectCollection lines)
        {
            // 只用于移到原点的线集
            var index = new ThCADCoreNTSSpatialIndex(lines);
            var pl = new Polyline();
            pl.CreatePolygon(Point2d.Origin, 4, 10);
            var res = index.SelectCrossingPolygon(pl);
            var l = res[0] as Line;
            return l.ColorIndex;
        }
        public static DBObjectCollection GetCenterlineByLayer(string layerName)
        {
            using (var db = AcadDatabase.Active())
            {
                var centerLines = new DBObjectCollection();
                var lines = db.ModelSpace.OfType<Curve>();
                foreach (var l in lines)
                    if (l.Layer == layerName)
                        centerLines.Add(l.Clone() as Curve);
                return centerLines;
            }
        }
        public static DBObjectCollection GetBoundsByLayer(string layerName)
        {
            using (var db = AcadDatabase.Active())
            {
                var centerLines = new DBObjectCollection();
                var lines = db.ModelSpace.OfType<Polyline>();
                foreach (var l in lines)
                    if (l.Layer == layerName)
                        centerLines.Add(l.Clone() as Polyline);
                return centerLines;
            }
        }
        public static void GetPortBounds(Point3d srtP, out Dictionary<Polyline, PortModifyParam> dicPlToPort)
        {
            using (var db = AcadDatabase.Active())
            {
                var mat = Matrix3d.Displacement(-srtP.GetAsVector());
                dicPlToPort = new Dictionary<Polyline, PortModifyParam>();
                var portIds = ReadBlkIdsByName(ThHvacCommon.AI_PORT);
                foreach (var id in portIds)
                {
                    ThDuctPortsDrawService.GetPortDynBlockProperity(id, out Point3d pos, out string portRange, out double portHeight, out double portWidth, out double rotateAngle);
                    var p = pos.TransformBy(mat);
                    var pl = new Polyline();
                    if (portRange.Contains("侧"))
                    {
                        // 侧风口脚长为100
                        pl.CreatePolygon(p.ToPoint2D(), 4, 110);
                    }
                    else
                    {
                        pl.CreatePolygon(p.ToPoint2D(), 4, 10);
                    }
                    var airVolume = GetAirVolume(id.GetAttributeInBlockReference("风量"));
                    dicPlToPort.Add(pl, new PortModifyParam() { portAirVolume = airVolume, pos = p, portRange = portRange, portHeight = portHeight, portWidth = portWidth, rotateAngle = rotateAngle, handle = id.Handle });
                }
            }
        }
        private static void GetDuctBounds(Point3d srtP, out ThCADCoreNTSSpatialIndex ductsIndex, out Dictionary<Polyline, DuctModifyParam> ductsDic)
        {
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            ThDuctPortsInterpreter.GetDucts(out List<DuctModifyParam> ducts);// 管段外包框到管段参数的映射
            ductsDic = new Dictionary<Polyline, DuctModifyParam>();
            foreach (var d in ducts)
            {
                d.sp = ThMEPHVACService.RoundPoint(d.sp.TransformBy(mat), 6);
                d.ep = ThMEPHVACService.RoundPoint(d.ep.TransformBy(mat), 6);
                var l = new Line(d.sp, d.ep);
                var w = ThMEPHVACService.GetWidth(d.ductSize) * 0.5;
                var pl = l.Buffer(w);
                ductsDic.Add(pl, d);
            }
            ductsIndex = new ThCADCoreNTSSpatialIndex(ductsDic.Keys.ToCollection());
        }
        // 用于仅读取风口数量时，将侧风口和下送风口的外包框都映射到中心线上
        private static void GetDuctVerticalOft(DuctModifyParam duct, out Vector3d lVec, out Vector3d rVec, out Vector3d dirVec)
        {
            var w = ThMEPHVACService.GetWidth(duct.ductSize) * 0.5 + 100;// 侧风口脚长为100
            dirVec = (duct.ep - duct.sp).GetNormal();
            lVec = -ThMEPHVACService.GetLeftVerticalVec(dirVec) * w;
            rVec = -ThMEPHVACService.GetRightVerticalVec(dirVec) * w;
        }
        private static bool AdjustSidePortPos(ref Point3d p, ThCADCoreNTSSpatialIndex ductsIndex, Dictionary<Polyline, DuctModifyParam> ductsDic)
        {
            // 侧风口脚长为100
            var pl = new Polyline();
            pl.CreatePolygon(p.ToPoint2D(), 4, 150);
            var res = ductsIndex.SelectCrossingPolygon(pl);
            if (res.Count == 1)
            {
                var duct = ductsDic[res[0] as Polyline];
                GetDuctVerticalOft(duct, out Vector3d lRegressVec, out Vector3d rRegressVec, out Vector3d dirVec);
                var vec = (p - duct.sp).GetNormal();
                if (dirVec.CrossProduct(vec).Z > 0)
                    p += lRegressVec;
                else
                    p += rRegressVec;
                p = ThMEPHVACService.RoundPoint(p, 6);
                return true;
            }
            return false;
        }
        public static void GetCenterPortBounds(PortParam portParam, 
                                               out Dictionary<Polyline, PortModifyParam> dicPlToPort,
                                               out Dictionary<Point3d, List<Handle>> sidePortHandle)
        {
            using (var db = AcadDatabase.Active())
            {
                var srtP = portParam.srtPoint;
                var mat = Matrix3d.Displacement(-srtP.GetAsVector());
                dicPlToPort = new Dictionary<Polyline, PortModifyParam>();
                var portIds = ReadBlkIdsByName(ThHvacCommon.AI_PORT);
                GetDuctBounds(srtP, out ThCADCoreNTSSpatialIndex ductsIndex, out Dictionary<Polyline, DuctModifyParam> ductsDic);
                sidePortHandle = new Dictionary<Point3d, List<Handle>>();
                foreach (var id in portIds)
                {
                    ThDuctPortsDrawService.GetPortDynBlockProperity(id, out Point3d pos, out string portRange, out double portHeight, out double portWidth, out double rotateAngle);
                    var p = pos.TransformBy(mat);
                    var pl = new Polyline();
                    if (portRange.Contains("侧"))
                    {
                        // p 只保留6位小数
                        if (!AdjustSidePortPos(ref p, ductsIndex, ductsDic))
                            continue;
                        if (!sidePortHandle.ContainsKey(p))
                        {
                            pl.CreatePolygon(p.ToPoint2D(), 4, 10);
                            var airVolume = GetAirVolume(id.GetAttributeInBlockReference("风量")) * 2;
                            dicPlToPort.Add(pl, new PortModifyParam() { portAirVolume = airVolume, pos = p, portRange = portRange, portHeight = portHeight, portWidth = portWidth, rotateAngle = rotateAngle, handle = id.Handle });
                            sidePortHandle.Add(p, new List<Handle>() { id.Handle });
                        }
                        else
                            sidePortHandle[p].Add(id.Handle);
                    }
                    else
                    {
                        pl.CreatePolygon(p.ToPoint2D(), 4, 10);
                        var airVolume = GetAirVolume(id.GetAttributeInBlockReference("风量"));
                        dicPlToPort.Add(pl, new PortModifyParam() { portAirVolume = airVolume, pos = p, portRange = portRange, portHeight = portHeight, portWidth = portWidth, rotateAngle = rotateAngle, handle = id.Handle });
                    }
                }
            }
        }
        public static DBObjectCollection GetPortBoundsByPortAirVolume(PortParam portParam, out Dictionary<int, PortInfo> dicPlToAirVolume)
        {
            using (var db = AcadDatabase.Active())
            {
                var portBounds = new DBObjectCollection();
                dicPlToAirVolume = new Dictionary<int, PortInfo>();
                var portIds = ReadPortComponents();

                var portsBlk = portIds.Select(o => db.Element<BlockReference>(o)).ToList();
                if (portParam.param.portRange.Contains("下") || 
                    portParam.param.portRange == "方形散流器" ||
                    portParam.param.portRange == "圆形风口")
                    GetDownPortBoundsByPortAirVolume(portParam, portBounds, portsBlk, dicPlToAirVolume);
                else if (portParam.param.portRange.Contains("侧"))
                    GetSidePortBoundsByPortAirVolume(portParam, portBounds, portsBlk, dicPlToAirVolume);
                else
                {
                    // 不处理
                }
                return portBounds;
            }
        }
        private static void GetDownPortBoundsByPortAirVolume(PortParam portParam, DBObjectCollection portBounds, List<BlockReference> portsBlk, Dictionary<int, PortInfo> dicPlToAirVolume)
        {
            foreach (var port in portsBlk)
            {
                var blkName = GetEffectiveBlkByName(port);
                ThMEPHVACService.GetWidthAndHeight(portParam.param.portSize, out double w, out double h);
                var extLen = Math.Min(w, h) * 0.5;
                if (blkName == ThHvacCommon.AI_PORT)
                {
                    var centerP = ThMEPHAVCBounds.GetDownPortCenterPoint(port, portParam);
                    var portBound = new Polyline();
                    portBound.CreatePolygon(centerP.ToPoint2D(), 4, extLen);
                    var polygon = new DBObjectCollection() { portBound }.BuildMPolygon();
                    portBounds.Add(polygon);
                    var airVolume = GetAirVolume(port.Id.GetAttributeInBlockReference("风量"));
                    dicPlToAirVolume.Add(polygon.GetHashCode(), 
                        new PortInfo() { portAirVolume = airVolume, position = centerP, id = port.Id, effectiveName = ThHvacCommon.AI_PORT });
                }
                else
                    AddPortCompToDic(portParam, port, dicPlToAirVolume, portBounds);
            }
        }
        private static void GetSidePortBoundsByPortAirVolume(PortParam portParam, DBObjectCollection portBounds, List<BlockReference> portsBlk, Dictionary<int, PortInfo> dicPlToAirVolume)
        {
            var ductBounds = GetDuctInfo(out Dictionary<int, double> dicDuctInfo);
            var ductIndex = new ThCADCoreNTSSpatialIndex(ductBounds);
            foreach (var port in portsBlk)
            {
                // 使用侧回风口的OBB
                var blkName = GetEffectiveBlkByName(port);
                if (blkName == ThHvacCommon.AI_PORT)
                {
                    var portPl = new Polyline();
                    portPl.CreateRectangle(port.Bounds.Value.MinPoint.ToPoint2D(), port.Bounds.Value.MaxPoint.ToPoint2D());
                    var res = ductIndex.SelectCrossingPolygon(portPl);
                    if (res.Count == 0)
                        continue;
                    if (res.Count != 1)
                        throw new NotImplementedException("[CheckError]: port cross with multi duct!");
                    var ductWidth = dicDuctInfo[(res[0] as Polyline).GetHashCode()];
                    var centerP = ThMEPHAVCBounds.GetSidePortCenterPoint(port, portParam.srtPoint, portParam.param.portSize, ductWidth);
                    var portBound = new Polyline();
                    portBound.CreatePolygon(centerP.ToPoint2D(), 4, 10);
                    var polygon = new DBObjectCollection() { portBound }.BuildMPolygon();
                    portBounds.Add(polygon);
                    var airVolume = GetAirVolume(port.Id.GetAttributeInBlockReference("风量")) * 2; // 一对侧风口
                    dicPlToAirVolume.Add(polygon.GetHashCode(), new PortInfo() { portAirVolume = airVolume, position = centerP });
                }
                else
                    AddPortCompToDic(portParam, port, dicPlToAirVolume, portBounds);
            }
        }

        private static void AddPortCompToDic(PortParam portParam, BlockReference blk, Dictionary<int, PortInfo> dicPlToAirVolume, DBObjectCollection portBounds)
        {
            var blkName = GetEffectiveBlkByName(blk);
            var bound = new Polyline();
            var mat = Matrix3d.Displacement(-portParam.srtPoint.GetAsVector());
            if (blkName == ThHvacCommon.AI_BROKEN_LINE || blkName == ThHvacCommon.AI_VERTICAL_PIPE)
            {
                var p = blk.Position.TransformBy(mat);
                double len = 10;
                if (blkName == ThHvacCommon.AI_VERTICAL_PIPE)
                {
                    ThDuctPortsDrawService.GetVerticalPipeDynBlockProperity(blk.Id, out double pipeWidth, out double pipeHeight);
                    len = Math.Max(pipeWidth, pipeHeight) * 0.5 + 10;
                }
                bound.CreatePolygon(p.ToPoint2D(), 4, len);
                var strVolume = blk.Id.GetAttributeInBlockReference("风量");
                double airVolume = GetAirVolume(strVolume);
                var polygon = new DBObjectCollection() { bound }.BuildMPolygon();
                portBounds.Add(polygon);
                dicPlToAirVolume.Add(polygon.GetHashCode(), 
                    new PortInfo() { portAirVolume = airVolume, position = p, id = blk.Id, effectiveName = blkName });
            }
            else
            {
                // 不处理
            }
        }

        private static DBObjectCollection GetDuctInfo(out Dictionary<int, double> dicDuctInfo)
        {
            dicDuctInfo = new Dictionary<int, double>();
            var ductIds = ThHvacGetComponent.ReadDuctIds();
            var bounds = new DBObjectCollection();
            foreach (var id in ductIds)
            {
                var param = ThHvacAnalysisComponent.GetDuctParamById(id);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep))
                    continue;
                var w = ThMEPHVACService.GetWidth(param.ductSize);
                var dirVec = (param.ep - param.sp).GetNormal();
                // 线长前后缩1方便与中心线相交，管宽扩1方便与侧回风口相交
                var pl = ThMEPHVACService.GetLineExtend(param.sp + dirVec, param.ep - dirVec, w + 2);
                bounds.Add(pl);
                dicDuctInfo.Add(pl.GetHashCode(), w);
            }
            return bounds;
        }
        private static void SplitArc(Arc arc, DBObjectCollection lines)
        {
            var pl = arc.TessellateArcWithChord(300);
            var arcLines = ThMEPHVACLineProc.Explode(new DBObjectCollection() { pl });
            foreach (Line l in arcLines)
                lines.Add(l);
        }
        private static DBObjectCollection ProcLines(DBObjectCollection curves)
        {
            var lines = new DBObjectCollection();
            foreach (Curve c in curves)
            {
                if (c is Line)
                    lines.Add(c);
                else if (c is Polyline polyline)
                {
                    polyline = polyline.DPSimplify(1);
                    var t = ThMEPHVACLineProc.Explode(new DBObjectCollection() { polyline });
                    foreach (Curve l in t)
                    {
                        if (l is Line)
                            lines.Add(l);
                        else if ((l is Arc))
                        {
                            var arc = l as Arc;
                            SplitArc(arc, lines);
                        }
                    }    
                }
                else if (c is Arc)
                {
                    var arc = c as Arc;
                    SplitArc(arc, lines);
                }
            }
            return lines;
        }
        public static DBObjectCollection ReadSmokeLine()
        {
            var wallBounds = GetBoundsByLayer(ThHvacCommon.AI_ROOM_BOUNDS);
            var broker = GetCenterlineByLayer(ThHvacCommon.AI_SMOKE_BROKE);
            var lines = ProcLines(wallBounds);
            var t = ProcLines(broker);
            foreach (Line l in t)
                lines.Add(l);
            return lines;
        }

        private static double GetAirVolume(string s)
        {
            if (s.IsNullOrEmpty())
                return 0;
            var str = s.Split('m');
            return str.Count() > 0 ? Double.Parse(str[0]) : 0;
        }
    }
}