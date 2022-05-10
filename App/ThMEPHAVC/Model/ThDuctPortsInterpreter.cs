using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Algorithm;
using ThCADCore.NTS;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsInterpreter
    {
        public static void GetVtElbow(out List<VTElbowModifyParam> vtElbows)
        {
            vtElbows = new List<VTElbowModifyParam>();
            var vtElbowsIds = ThDuctPortsReadComponent.ReadGroupIdsByType("Vertical_elbow");
            foreach (var id in vtElbowsIds)
            {
                var param = GetVtElbowById(id);
                if (param.handle != ObjectId.Null.Handle)
                    vtElbows.Add(param);
            }
        }
        public static VTElbowModifyParam GetVtElbowById(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var ductList = ThHvacAnalysisComponent.GetValueList(ids, ThHvacCommon.RegAppName_Duct_Info);
            return GetVtElbowParam(ductList, id.Handle);
        }
        public static void GetDucts(out List<DuctModifyParam> ducts)
        {
            ducts = new List<DuctModifyParam>();
            var ductIds = ThDuctPortsReadComponent.ReadGroupIdsByType("Duct");
            var tor = new Tolerance(1.5, 1.5);
            foreach (var id in ductIds)
            {
                var param = ThHvacAnalysisComponent.GetDuctParamById(id);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep, tor))
                    continue;
                ducts.Add(param);
            }
        }
        public static void GetShapesDic(Point3d srtP, out Dictionary<Polyline, EntityModifyParam> dic)
        {
            dic = new Dictionary<Polyline, EntityModifyParam>();
            var id2geoDic = ThDuctPortsReadComponent.ReadGroupId2geoDic(srtP);
            foreach (var id in id2geoDic.Keys)
            {
                var param = ThHvacAnalysisComponent.GetConnectorParamById(id);
                if (param.handle != ObjectId.Null.Handle && param.portWidths.Count() != 0)
                    dic.Add(id2geoDic[id], param);
            }
        }     
        public static void GetTextsDic(out Dictionary<Polyline, TextModifyParam> dic)
        {
            dic = new Dictionary<Polyline, TextModifyParam>();
            var texts = ThDuctPortsReadComponent.ReadDuctTexts();
            foreach (var t in texts)
            {
                var dy = t.Bounds.Value.MaxPoint.Y - t.Bounds.Value.MinPoint.Y;
                var dirVec = (t.Bounds.Value.MinPoint - t.Bounds.Value.MaxPoint).GetNormal();
                // 朝min的方向外延150，让文字外包框能与duct外包框相交
                var l = new Line(t.Bounds.Value.MinPoint + dirVec * 150, t.Bounds.Value.MaxPoint);
                Polyline poly = l.Buffer(dy);
                dic.Add(poly, GetTextParam(t));
            }
        }
        public static void GetHoseBounds(out DBObjectCollection list)
        {
            list = new DBObjectCollection();
            var hoseIds = ThDuctPortsReadComponent.ReadBlkIdsByName("风机软接");
            foreach (var id in hoseIds)
            {
                var blk = (BlockReference)id.GetEntity();
                ThDuctPortsDrawService.GetHoseDynBlockProperity(id, out double len, out double width);
                var poly = ThMEPHAVCBounds.GetHoseBounds(blk, len, width);
                list.Add(poly);
            }
        }
        public static void GetFanDic(out Dictionary<Polyline, ObjectId> dic)
        {
            using (var db = AcadDatabase.Active())
            {
                dic = new Dictionary<Polyline, ObjectId>();
                var fanIds = ThDuctPortsReadComponent.ReadBlkIdsByName("轴流风机");
                fanIds.AddRange(ThDuctPortsReadComponent.ReadBlkIdsByName("离心风机"));
                foreach (var id in fanIds)
                {
                    var fan = new ThDbModelFan(id);
                    if (fan.airVolume <= 0)
                        continue;
                    var l = new Line(fan.FanInletBasePoint, fan.FanOutletBasePoint);
                    // 风机进出口组成的长条矩形，进出口是否水平共线无所谓
                    var poly = ThMEPHVACService.GetLineExtend(l, 2);
                    dic.Add(poly, id);
                }
            }
        }
        public static void GetValvesDic(out Dictionary<Polyline, ValveModifyParam> dic)
        {
            dic = new Dictionary<Polyline, ValveModifyParam>();
            GetValvesDicByName(dic, "风阀");
            GetValvesDicByName(dic, "防火阀");
        }
        public static void GetValvesDicByName(Dictionary<Polyline, ValveModifyParam> dic, string valveName)
        {
            using (var db = AcadDatabase.Active())
            {
                var valveIds = ThDuctPortsReadComponent.ReadBlkIdsByName(valveName);
                foreach (var id in valveIds)
                {
                    var blk = (BlockReference)id.GetEntity();
                    var param = GetValveParam(id, valveName);
                    var poly = ThMEPHAVCBounds.GetValveBounds(blk, param);
                    dic.Add(poly, param);
                }
            }
        }
        public static void GetHolesDic(out Dictionary<Polyline, HoleModifyParam> dic)
        {
            using (var db = AcadDatabase.Active())
            {
                dic = new Dictionary<Polyline, HoleModifyParam>();
                var holeIds = ThDuctPortsReadComponent.ReadBlkIdsByName("洞口");
                foreach (var id in holeIds)
                {
                    var blk = (BlockReference)id.GetEntity();
                    var poly = new Polyline();
                    poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                    dic.Add(poly, GetHoleParam(id, "洞口"));
                }
            }
        }
        public static void GetMufflerDic(out Dictionary<Polyline, MufflerModifyParam> dic)
        {
            using (var db = AcadDatabase.Active())
            {
                dic = new Dictionary<Polyline, MufflerModifyParam>();
                var holeIds = ThDuctPortsReadComponent.ReadBlkIdsByName("阻抗复合式消声器");
                foreach (var id in holeIds)
                {
                    var blk = (BlockReference)id.GetEntity();
                    var poly = new Polyline();
                    poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                    dic.Add(poly, GetMufflerParam(id, "阻抗复合式消声器"));
                }
            }
        }
        public static void GetPortsDic(out Dictionary<Polyline, PortModifyParam> dic)
        {
            using (var db = AcadDatabase.Active())
            {
                dic = new Dictionary<Polyline, PortModifyParam>();
                var holeIds = ThDuctPortsReadComponent.ReadBlkIdsByName("风口-AI研究中心");
                foreach (var id in holeIds)
                {
                    var blk = (BlockReference)id.GetEntity();
                    var poly = new Polyline();
                    poly.CreateRectangle(blk.Bounds.Value.MinPoint.ToPoint2D(), blk.Bounds.Value.MaxPoint.ToPoint2D());
                    dic.Add(poly, GetPortParam(id));
                }
            }
        }
        private static TextModifyParam GetTextParam(DBText text)
        {
            return new TextModifyParam() { pos = text.Position,  
                                           handle = text.Handle,
                                           height = text.Height,
                                           rotateAngle = text.Rotation,
                                           textString = text.TextString };
        }
        private static ValveModifyParam GetValveParam(ObjectId id, string valveName)
        {
            var param = new ValveModifyParam();
            ThDuctPortsDrawService.GetValveDynBlockProperity(id, out Point3d insertP, out double width,
                    out double height, out double textAngle, out double rotateAngle, out string valveVisibility);
            param.handle = id.Handle;
            param.valveName = valveName;
            param.valveLayer = id.GetBlockLayer();
            param.valveVisibility = valveVisibility;
            param.insertP = insertP;
            param.rotateAngle = rotateAngle;
            param.width = width;
            param.height = height;
            param.textAngle = textAngle;
            return param;
        }
        private static HoleModifyParam GetHoleParam(ObjectId id, string holeName)
        {
            var param = new HoleModifyParam();
            ThDuctPortsDrawService.GetHoleDynBlockProperity(id, out Point3d insertP, out double len, out double width, out double rotateAngle);
            param.handle = id.Handle;
            param.holeName = holeName;
            param.holeLayer = id.GetBlockLayer();
            param.insertP = insertP;
            param.len = len;
            param.width = width;
            param.rotateAngle = rotateAngle;
            return param;
        }
        private static MufflerModifyParam GetMufflerParam(ObjectId id, string mufflerName)
        {
            var param = new MufflerModifyParam();
            ThDuctPortsDrawService.GetMufflerDynBlockProperity(id, out Point3d insertP, out string visibility, out double len,
                                                                   out double height, out double width, out double textHeight, out double rotateAngle);
            param.handle = id.Handle;
            param.name = mufflerName;
            param.mufflerLayer = id.GetBlockLayer();
            param.insertP = insertP;
            param.len = len;
            param.width = width;
            param.mufflerVisibility = visibility;
            param.height = height;
            param.textHeight = textHeight;
            param.rotateAngle = rotateAngle;
            return param;
        }
        public static PortModifyParam GetPortParam(ObjectId id)
        {
            var param = new PortModifyParam();
            ThDuctPortsDrawService.GetPortDynBlockProperity(id, out Point3d pos, out string portRange,
                out double portHeight, out double portWidth, out double rotateAngle);
            param.handle = id.Handle;
            param.rotateAngle = rotateAngle;
            param.portWidth = portWidth;
            param.portHeight = portHeight;
            param.portRange = portRange;
            param.pos = pos;
            return param;
        }
        public static VTElbowModifyParam GetVtElbowParam(TypedValueList list, Handle groupHandle)
        {
            var param = new VTElbowModifyParam();
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
                return param;
            param.handle = groupHandle;
            var type = (string)values.ElementAt(0).Value;
            if (type != "Vertical_elbow")
                return param;
            using (var db = AcadDatabase.Active())
            {
                var id = db.Database.GetObjectId(false, groupHandle, 0);
                var portIndex2PositionDic = ThDuctPortsReadComponent.GetBypassPortsOfGroup(id);
                if (portIndex2PositionDic.Count == 0)
                    return param;
                //param.detectP = portIndex2PositionDic["0"].ToPoint2D();
            }
            return param;
        }
        public static string GetEntityType(ObjectId id)
        {
            var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
            if (list != null)
            {
                var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                if (values.Any())
                {
                    return (string)values.ElementAt(0).Value;
                }
            }
            return String.Empty;
        }
        public static string GetEntityType(ObjectId[] objIds)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var id in objIds)
                {
                    var groups = id.GetGroups();
                    if (groups == null)
                        return String.Empty;
                    foreach (var g in groups)
                    {
                        var type = GetEntityType(g);
                        if (type != "")
                            return type;
                    }
                }
                return String.Empty;
            }
        }
        public static TypedValueList GetValueList(ObjectId[] objIds)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var id in objIds)
                {
                    var gIds = id.GetGroups();
                    if (gIds == null)
                        continue;
                    list = ThHvacAnalysisComponent.GetValueList(gIds, ThHvacCommon.RegAppName_Duct_Info);
                    if (list == null)
                        continue;
                    if (list.Count != 0)
                        break;
                }
                return list;
            }
        }
    }
}