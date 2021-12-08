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

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsInterpreter
    {
        public static void GetVtElbow(out List<VTElbowModifyParam> vt_elbows)
        {
            vt_elbows = new List<VTElbowModifyParam>();
            var vt_elbows_ids = ThDuctPortsReadComponent.ReadGroupIdsByType("Vertical_elbow");
            foreach (var id in vt_elbows_ids)
            {
                var param = GetVtElbowById(id);
                if (param.handle != ObjectId.Null.Handle)
                    vt_elbows.Add(param);
            }
        }
        public static VTElbowModifyParam GetVtElbowById(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var duct_list = ThHvacAnalysisComponent.GetValueList(ids, ThHvacCommon.RegAppName_Duct_Info);
            return GetVtElbowParam(duct_list, id.Handle);
        }
        public static void GetDuctsDic(out Dictionary<Polyline, DuctModifyParam> dic)
        {
            dic = new Dictionary<Polyline, DuctModifyParam>();
            var ductIds = ThDuctPortsReadComponent.ReadGroupIdsByType("Duct");
            var tor = new Tolerance(1.5, 1.5);
            foreach (var id in ductIds)
            {
                var param = ThHvacAnalysisComponent.GetDuctParamById(id);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep, tor))
                    continue;
                var poly = CreateDuctExtends(param);
                dic.Add(poly, param);                    
            }
        }
        public static void GetShapesDic(out Dictionary<Polyline, EntityModifyParam> dic)
        {
            dic = new Dictionary<Polyline, EntityModifyParam>();
            var id2geoDic = ThDuctPortsReadComponent.ReadGroupId2geoDic();
            foreach (var id in id2geoDic.Keys)
            {
                var param = ThHvacAnalysisComponent.GetConnectorParamById(id);
                if (param.handle != ObjectId.Null.Handle)
                    dic.Add(id2geoDic[id], param);
            }
        }
        private static Polyline CreateDuctExtends(DuctModifyParam param)
        {
            var dirVec = (param.ep - param.sp).GetNormal();
            var sp2 = param.sp - dirVec;
            var ep2 = param.ep + dirVec;
            var sp = new Point3d(sp2.X, sp2.Y, 0);
            var ep = new Point3d(ep2.X, ep2.Y, 0);
            var l = new Line(sp, ep);
            var width = ThMEPHVACService.GetWidth(param.ductSize);
            return ThMEPHVACService.GetLineExtend(l, width + 2);//对管段的外包框上下左右都扩1
        }        
        public static void GetTextsDic(out Dictionary<Polyline, TextModifyParam> dic)
        {
            dic = new Dictionary<Polyline, TextModifyParam>();
            var texts = ThDuctPortsReadComponent.ReadDuctTexts();
            foreach (var t in texts)
            {
                var poly = new Polyline();
                poly.CreateRectangle(t.Bounds.Value.MinPoint.ToPoint2D(), t.Bounds.Value.MaxPoint.ToPoint2D());
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
        public static void GetFanDic(out Dictionary<Polyline, FanModifyParam> dic)
        {
            dic = new Dictionary<Polyline, FanModifyParam>();
            var fanIds = ThDuctPortsReadComponent.ReadBlkIdsByName("轴流风机");
            fanIds.AddRange(ThDuctPortsReadComponent.ReadBlkIdsByName("离心风机"));
            foreach (var id in fanIds)
            {
                var fan = new ThDbModelFan(id);
                if (fan.airVolume < 0)
                    continue;
                var l = new Line(fan.FanInletBasePoint, fan.FanOutletBasePoint);
                // 风机进出口组成的长条矩形，进出口是否水平共线无所谓
                var poly = ThMEPHVACService.GetLineExtend(l, 2);
                var param = new FanModifyParam() { fanName = id.GetBlockName() };
                dic.Add(poly, param);
            }
        }
        public static void GetValvesDic(out Dictionary<Polyline, ValveModifyParam> dic)
        {
            dic = new Dictionary<Polyline, ValveModifyParam>();
            GetValvesDicByName(dic, "风阀");
            GetValvesDicByName(dic, "防火阀");
        }
        public static void GetValvesDicByName(Dictionary<Polyline, ValveModifyParam> dic, string valve_name)
        {
            var valveIds = ThDuctPortsReadComponent.ReadBlkIdsByName(valve_name);
            foreach (var id in valveIds)
            {
                var blk = (BlockReference)id.GetEntity();
                var param = GetValveParam(id, valve_name);
                var poly = ThMEPHAVCBounds.GetValveBounds(blk, param);
                dic.Add(poly, param);
            }
        }
        public static void GetHolesDic(out Dictionary<Polyline, HoleModifyParam> dic)
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
        public static void GetMufflerDic(out Dictionary<Polyline, MufflerModifyParam> dic)
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
        public static void GetPortsDic(out Dictionary<Polyline, PortModifyParam> dic)
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
        private static TextModifyParam GetTextParam(DBText text)
        {
            return new TextModifyParam() { pos = text.Position,  
                                           handle = text.Handle,
                                           height = text.Height,
                                           rotateAngle = text.Rotation,
                                           textString = text.TextString };
        }
        private static ValveModifyParam GetValveParam(ObjectId id, string valve_name)
        {
            var param = new ValveModifyParam();
            ThDuctPortsDrawService.GetValveDynBlockProperity(id, out Point3d insert_p, out double width,
                    out double height, out double text_angle, out double rotate_angle, out string valve_visibility);
            param.handle = id.Handle;
            param.valveName = valve_name;
            param.valveLayer = id.GetBlockLayer();
            param.valveVisibility = valve_visibility;
            param.insertP = insert_p.ToPoint2D();
            param.rotateAngle = rotate_angle;
            param.width = width;
            param.height = height;
            param.textAngle = text_angle;
            return param;
        }
        private static HoleModifyParam GetHoleParam(ObjectId id, string hole_name)
        {
            var param = new HoleModifyParam();
            ThDuctPortsDrawService.GetHoleDynBlockProperity(id, out Point3d insert_p, out double len, out double width, out double rotate_angle);
            param.handle = id.Handle;
            param.holeName = hole_name;
            param.holeLayer = id.GetBlockLayer();
            param.insertP = insert_p.ToPoint2D();
            param.len = len;
            param.width = width;
            param.rotateAngle = rotate_angle;
            return param;
        }
        private static MufflerModifyParam GetMufflerParam(ObjectId id, string muffler_name)
        {
            var param = new MufflerModifyParam();
            ThDuctPortsDrawService.GetMufflerDynBlockProperity(id, out Point3d insert_p, out string visibility, out double len,
                                                                   out double height, out double width, out double text_height, out double rotate_angle);
            param.handle = id.Handle;
            param.name = muffler_name;
            param.mufflerLayer = id.GetBlockLayer();
            param.insertP = insert_p.ToPoint2D();
            param.len = len;
            param.width = width;
            param.mufflerVisibility = visibility;
            param.height = height;
            param.textHeight = text_height;
            param.rotateAngle = rotate_angle;
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
            param.pos = pos.ToPoint2D();
            return param;
        }
        public static VTElbowModifyParam GetVtElbowParam(TypedValueList list, Handle group_handle)
        {
            var param = new VTElbowModifyParam();
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
                return param;
            param.handle = group_handle;
            var type = (string)values.ElementAt(1).Value;
            if (type != "Vertical_elbow")
                return param;
            using (var db = AcadDatabase.Active())
            {
                var id = db.Database.GetObjectId(false, group_handle, 0);
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
        public static string GetEntityType(ObjectId[] obj_ids)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var id in obj_ids)
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
        public static TypedValueList GetValueList(ObjectId[] obj_ids)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var id in obj_ids)
                {
                    var g_ids = id.GetGroups();
                    if (g_ids == null)
                        continue;
                    list = ThHvacAnalysisComponent.GetValueList(g_ids, ThHvacCommon.RegAppName_Duct_Info);
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