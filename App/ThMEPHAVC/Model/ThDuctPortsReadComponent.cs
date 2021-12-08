using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Algorithm;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReadComponent
    {
        public static Dictionary<Polyline, ObjectId> ReadAllComponent()
        {
            var bounds2IdDic = new Dictionary<Polyline, ObjectId>();
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var list = g.ObjectId.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (g.NumEntities != 0 && list != null)
                    {
                        var poly = new Polyline();
                        GetGroupBound(g, out Point3d lowLeftP, out Point3d highRightP);
                        poly.CreateRectangle(lowLeftP.ToPoint2D(), highRightP.ToPoint2D());
                        bounds2IdDic.Add(poly, g.ObjectId);
                    }
                }
                return bounds2IdDic;
            }
        }
        private static void GetGroupBound(Group g, out Point3d lowLeftP, out Point3d highRightP)
        {
            Extents3d? groupExtents = null;
            using (var db = AcadDatabase.Active())
            {
                lowLeftP = highRightP = Point3d.Origin;
                var gIds = g.GetAllEntityIds();
                foreach (var id in gIds)
                {
                    var e = db.Element<Entity>(id);
                    if (groupExtents == null)
                    {
                        groupExtents = e.Bounds;
                        lowLeftP = groupExtents.Value.MinPoint;
                        highRightP = groupExtents.Value.MaxPoint;
                    }
                    else
                    {
                        var cur_bound = (Extents3d)e.Bounds;
                        lowLeftP = ThMEPHVACService.GetMinPoint(lowLeftP, cur_bound.MinPoint);
                        highRightP = ThMEPHVACService.GetMaxPoint(highRightP, cur_bound.MaxPoint);
                    }
                }
            }
        }
        public static Dictionary<ObjectId, Polyline> ReadGroupId2geoDic()
        {
            var dic = new Dictionary<ObjectId, Polyline>();
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
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
                                dic.Add(id, pl);
                        }
                    }
                }
            }
            return dic;
        }
        private static DBObjectCollection GetGroupEntitys(Group g)
        {
            var entityIds = g.GetAllEntityIds();
            var entitys = new DBObjectCollection();
            foreach (var e_id in entityIds)
            {
                var e = e_id.GetDBObject();
                if (!(e is Line))
                    continue;
                entitys.Add(e);
            }
            return entitys;
        }
        public static List<ObjectId> ReadGroupIdsByType(string range)
        {
            var ids = new List<ObjectId>();
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var id = g.ObjectId;
                    var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (list != null)
                    {
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type == range)
                            ids.Add(id);
                    }
                }
            }
            return ids;
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

        public static List<ObjectId> ReadBlkIdsByName(string blk_name)
        {
            return ReadBlkByName(blk_name).Select(o => o.ObjectId).ToList();
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
        public static DBObjectCollection GetCenterlineByLayer(string layerName)
        {
            using (var db = AcadDatabase.Active())
            {
                var center_lines = new DBObjectCollection();
                var lines = db.ModelSpace.OfType<Line>();
                foreach (var l in lines)
                    if (l.Layer == layerName)
                        center_lines.Add(l.Clone() as Line);
                return center_lines;
            }
        }
        public static DBObjectCollection GetBoundsByLayer(string layerName)
        {
            using (var db = AcadDatabase.Active())
            {
                var center_lines = new DBObjectCollection();
                var lines = db.ModelSpace.OfType<Polyline>();
                foreach (var l in lines)
                    if (l.Layer == layerName)
                        center_lines.Add(l.Clone() as Polyline);
                return center_lines;
            }
        }
    }
}