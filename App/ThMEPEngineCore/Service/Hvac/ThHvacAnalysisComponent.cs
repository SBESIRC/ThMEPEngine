using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPEngineCore.Service.Hvac
{
    public class GroupIdx
    {
        public int geoIdx = 0;
        public int flgIdx = 0;
        public int centerLineIdx = 0;
    }
    public class ThHvacAnalysisComponent
    {
        public static DuctModifyParam GetDuctParamById(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var ductList = GetValueList(ids, ThHvacCommon.RegAppName_Duct_Info);
            return AnayDuctparam(ductList, id);
        }
        public static EntityModifyParam GetConnectorParamById(ObjectId id)
        {
            var ids = new ObjectId[] { id };
            var entityList = GetValueList(ids, ThHvacCommon.RegAppName_Duct_Info);
            return AnayConnectorparam(entityList, id);
        }
        public static TypedValueList GetValueList(IEnumerable<ObjectId> gIds, string regAppName)
        {
            using (var db = AcadDatabase.Active())
            {
                var list = new TypedValueList();
                foreach (var gId in gIds)
                {
                    list = gId.GetXData(regAppName);
                    if (list == null)
                        continue;
                    break;
                }
                return list;
            }
        }
        public static DuctModifyParam AnayDuctparam(TypedValueList list, ObjectId groupId)
        {
            using (var db = AcadDatabase.Active())
            {
                return AnayDuctparam(list, groupId, db.Database);
            }
        }
        public static DuctModifyParam AnayDuctparam(TypedValueList list, ObjectId groupId, Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var param = new DuctModifyParam();
                var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                if (!values.Any())
                    return param;
                if (values.Count() != 4)
                    return param;
                int inc = 0;
                param.handle = groupId.Handle;
                param.type = (string)values.ElementAt(inc++).Value;
                if (param.type != "Duct" && param.type != "Vertical_bypass")
                    return param;
                param.airVolume = Double.Parse((string)values.ElementAt(inc++).Value);
                param.elevation = Double.Parse((string)values.ElementAt(inc++).Value);
                param.ductSize = (string)values.ElementAt(inc++).Value;
                var group = db.Element<Group>(groupId);
                var allEntityIds = group.GetAllEntityIds();
                if (allEntityIds.Count() != 5)
                    return param;
                var centerLineId = allEntityIds[4];
                if (centerLineId == null)
                    return param;
                var centerline = db.Element<Line>(centerLineId);
                param.sp = centerline.StartPoint;
                param.ep = centerline.EndPoint;
                return param;
            }
        }
        private static EntityModifyParam AnayConnectorparam(TypedValueList list, ObjectId groupId)
        {
            var param = new EntityModifyParam();
            if (list.Count > 0)
            {
                var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                int inc = 0;
                param.handle = groupId.Handle;
                param.type = (string)values.ElementAt(inc++).Value;
                if (!values.Any() || param.type == "Duct")
                    return param;
                using (var db = AcadDatabase.Active())
                {
                    var g = db.Element<Group>(groupId);
                    var ids = g.GetAllEntityIds();
                    if (ids.IsNull() || ids.Count() == 0)
                        return param;
                    var idxInfo = GetGroupStartIdxByType(param.type, ids.Count());
                    var centerLines = GetConnectorCenterLine(ids, idxInfo);
                    var portWidths = GetConnectorPortWidth(ids, idxInfo);
                    var centerP = (centerLines[0] as Line).StartPoint;
                    var dic = CreatePortMap(centerLines, portWidths);
                    return new EntityModifyParam() { type = param.type, handle = param.handle, portWidths = dic, centerP = centerP };
                }
            }
            return param;
        }

        private static Dictionary<Point3d, string> CreatePortMap(List<Line> centerLines, List<string> portWidths)
        {
            var dic = new Dictionary<Point3d, string>();
            if (centerLines.Count == 1)
            {
                // Reducing
                dic.Add(centerLines[0].StartPoint, portWidths[0]);
                dic.Add(centerLines[0].EndPoint, portWidths[1]);
            }
            else
            {
                for (int i = 0; i < portWidths.Count; ++i)
                    dic.Add(centerLines[i].EndPoint, portWidths[i]);
            }
            return dic;
        }
        
        private static List<string> GetConnectorPortWidth(IEnumerable<ObjectId> ids, GroupIdx idxInfo)
        {
            var lineIds = ids.ToArray();
            switch (ids.Count())
            {
                case ThHvacCommon.REDUCING_ALL_NUM: return GetReducingWidths(lineIds, idxInfo);
                case ThHvacCommon.AXIS_REDUCING_ALL_NUM: return GetReducingWidths(lineIds, idxInfo);
                case ThHvacCommon.ELBOW_ALL_NUM: return GetElbowWidths(lineIds, idxInfo);
                case ThHvacCommon.R_TEE_ALL_NUM: return GetTeeWidths(lineIds, idxInfo);
                case ThHvacCommon.V_TEE_ALL_NUM: return GetTeeWidths(lineIds, idxInfo);
                case ThHvacCommon.V_TEE_ALL_NUM + 1: return GetTeeWidths(lineIds, idxInfo);
                case ThHvacCommon.CROSS_ALL_NUM: return CreateCrossWidths(lineIds, idxInfo);
                default: throw new NotImplementedException("[CheckError]: No such connector!");
            }
        }

        private static List<string> GetReducingWidths(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var flg1 = db.Element<Line>(lineIds[idxInfo.flgIdx]);
                var flg2 = db.Element<Line>(lineIds[idxInfo.flgIdx + 1]);
                var dis1 = ShrinkFlg(flg1);
                var dis2 = ShrinkFlg(flg2);
                return new List<string>() { dis1, dis2 };
            }
        }

        private static List<string> CreateCrossWidths(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var mainFlg = db.Element<Line>(lineIds[idxInfo.flgIdx]);
                var outFlg = db.Element<Line>(lineIds[idxInfo.flgIdx + 1]);
                var outterBranchFlg = db.Element<Line>(lineIds[idxInfo.flgIdx + 2]);
                var innerBranchFlg = db.Element<Line>(lineIds[idxInfo.flgIdx + 3]);

                var mainDis = ShrinkFlg(mainFlg);
                var outDis = ShrinkFlg(outFlg);
                var outterBranchDis = ShrinkFlg(outterBranchFlg);
                var innerBranchDis = ShrinkFlg(innerBranchFlg);
                return new List<string>() { mainDis, outDis, outterBranchDis, innerBranchDis };
            }
        }
        
        private static List<string> GetTeeWidths(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var mainFlg = db.Element<Line>(lineIds[idxInfo.flgIdx]);
                var innerBranchFlg = db.Element<Line>(lineIds[idxInfo.flgIdx + 1]);
                var otherBranchFlg = db.Element<Line>(lineIds[idxInfo.flgIdx + 2]);

                var mainDis = ShrinkFlg(mainFlg);
                var innerBranchDis = ShrinkFlg(innerBranchFlg);
                var otherBranchDis = ShrinkFlg(otherBranchFlg);
                return new List<string>() { mainDis, innerBranchDis, otherBranchDis };
            }
        }

        private static List<string> GetElbowWidths(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var flgLine = db.Element<Line>(lineIds[idxInfo.flgIdx]);
                var dis = ShrinkFlg(flgLine);
                return new List<string>() { dis, dis };
            }
        }
        private static string ShrinkFlg(Line l)
        {
            var flgExtendLen = 45;
            return (l.Length - 2 * flgExtendLen).ToString() + "x" + "0";
        }
        private static List<Line> GetConnectorCenterLine(IEnumerable<ObjectId> ids, GroupIdx idxInfo)
        {
            var lineIds = ids.ToArray();
            switch (ids.Count())
            {
                case ThHvacCommon.REDUCING_ALL_NUM: return CreateReducingCenterLine(lineIds, idxInfo);
                case ThHvacCommon.AXIS_REDUCING_ALL_NUM: return CreateReducingCenterLine(lineIds, idxInfo);
                case ThHvacCommon.ELBOW_ALL_NUM: return CreateElbowCenterLine(lineIds, idxInfo);
                case ThHvacCommon.R_TEE_ALL_NUM: return CreateTeeCenterLine(lineIds, idxInfo);
                case ThHvacCommon.V_TEE_ALL_NUM: return CreateTeeCenterLine(lineIds, idxInfo);
                case ThHvacCommon.V_TEE_ALL_NUM + 1: return CreateTeeCenterLine(lineIds, idxInfo);
                case ThHvacCommon.CROSS_ALL_NUM: return CreateCrossCenterLine(lineIds, idxInfo);
                default: throw new NotImplementedException("[CheckError]: No such connector!");
            }
        }

        private static List<Line> CreateReducingCenterLine(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var mainLine = db.Element<Line>(lineIds[idxInfo.centerLineIdx]);
                return new List<Line>() { mainLine };
            }
        }

        private static List<Line> CreateCrossCenterLine(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var mainLine = db.Element<Line>(lineIds[idxInfo.centerLineIdx]);
                var mainSmall = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 1]);
                var outterBranch = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 2]);
                var innerBranch = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 3]);
                return new List<Line>() { mainLine, mainSmall, outterBranch, innerBranch };
            }
        }
        private static List<Line> CreateTeeCenterLine(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                var mainLine = db.Element<Line>(lineIds[idxInfo.centerLineIdx]);
                var innerBranch = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 1]);
                var otherBranch = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 2]);
                return new List<Line>() { mainLine, innerBranch, otherBranch };
            }
        }
        private static List<Line> CreateElbowCenterLine(ObjectId[] lineIds, GroupIdx idxInfo)
        {
            using (var db = AcadDatabase.Active())
            {
                // 用两条直线代替elbow的中心线
                var arcId = lineIds[idxInfo.centerLineIdx];
                var arc = db.Element<Arc>(arcId);
                var endExtLine = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 1]);
                var srtExtLine = db.Element<Line>(lineIds[idxInfo.centerLineIdx + 2]);
                var cp = arc.Center;
                var arcSrtVec = arc.StartPoint - cp;
                var arcEndVec = arc.EndPoint - cp;
                var shadowCp = cp + (arcSrtVec + arcEndVec);
                var extLen = 50;
                var srtVec = (arc.StartPoint - shadowCp).GetNormal(); //shadowCp srt切点向量
                var realSrtEp = arc.StartPoint + srtVec * extLen;
                var srtTanLine = new Line(shadowCp, realSrtEp);
                var endVec = (arc.EndPoint - shadowCp).GetNormal();   //shadowCp end切点向量
                var realEndEp = arc.EndPoint + endVec * extLen;
                var endTanLine = new Line(shadowCp, realEndEp);
                return new List<Line>() { srtTanLine, endTanLine };
            }
        }

        private static GroupIdx GetGroupStartIdxByType(string type, int componentNum)
        {
            switch (type)
            {
                case "Elbow": 
                    return new GroupIdx() { flgIdx = ThHvacCommon.ELBOW_GEO_NUM, centerLineIdx = ThHvacCommon.ELBOW_GEO_NUM + ThHvacCommon.ELBOW_FLG_NUM };
                case "Cross": 
                    return new GroupIdx() { flgIdx = ThHvacCommon.CROSS_GEO_NUM, centerLineIdx = ThHvacCommon.CROSS_GEO_NUM + ThHvacCommon.CROSS_FLG_NUM };
                case "Reducing": 
                    return new GroupIdx() { flgIdx = ThHvacCommon.REDUCING_GEO_NUM, centerLineIdx = ThHvacCommon.REDUCING_GEO_NUM + ThHvacCommon.REDUCING_FLG_NUM };
                case "AxisReducing": 
                    return new GroupIdx() { flgIdx = ThHvacCommon.AXIS_REDUCING_GEO_NUM, centerLineIdx = ThHvacCommon.AXIS_REDUCING_GEO_NUM + ThHvacCommon.AXIS_REDUCING_FLG_NUM };
                case "Tee":
                    if (componentNum == ThHvacCommon.R_TEE_GEO_NUM + ThHvacCommon.R_TEE_FLG_NUM + ThHvacCommon.R_TEE_CENTERLINE_NUM)
                        return new GroupIdx() { flgIdx = ThHvacCommon.R_TEE_GEO_NUM, centerLineIdx = ThHvacCommon.R_TEE_GEO_NUM + ThHvacCommon.R_TEE_FLG_NUM };
                    else if (componentNum == ThHvacCommon.V_TEE_GEO_NUM + ThHvacCommon.V_TEE_FLG_NUM + ThHvacCommon.V_TEE_CENTERLINE_NUM)
                        return new GroupIdx() { flgIdx = ThHvacCommon.V_TEE_GEO_NUM, centerLineIdx = ThHvacCommon.V_TEE_GEO_NUM + ThHvacCommon.V_TEE_FLG_NUM };
                    else
                        return new GroupIdx() { flgIdx = ThHvacCommon.V_TEE_GEO_NUM + 1, centerLineIdx = ThHvacCommon.V_TEE_GEO_NUM + ThHvacCommon.V_TEE_FLG_NUM + 1 };
                default: throw new NotImplementedException("[CheckError]: No such connector!");
            }
        }
        public static Handle CovertObjToHandle(object o)
        {
            return new Handle(Convert.ToInt64((string)o, 16));
        }
        public static Dictionary<string, Tuple<Point3d, string>> GetPortsOfGroup(ObjectId groupId)
        {
            var rst = new Dictionary<string, Tuple<Point3d, string>>();

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
                        var portWidth = (string)values.ElementAt(2).Value;
                        rst.Add(portIndex, new Tuple<Point3d, string>((entity as DBPoint).Position, portWidth));
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
    }
}
