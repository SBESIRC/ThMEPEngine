using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Alg;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReadComponent
    {
        public static Dictionary<Polyline, ObjectId> Read_all_component()
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
                        Get_group_bound(g, out Point3d low_left_p, out Point3d high_right_p);
                        poly.CreateRectangle(low_left_p.ToPoint2D(), high_right_p.ToPoint2D());
                        bounds2IdDic.Add(poly, g.ObjectId);
                    }
                }
                return bounds2IdDic;
            }
        }
        private static void Get_group_bound(Group g, out Point3d low_left_p, out Point3d high_right_p)
        {
            Extents3d? groupExtents = null;
            using (var db = AcadDatabase.Active())
            {
                low_left_p = high_right_p = Point3d.Origin;
                var g_ids = g.GetAllEntityIds();
                foreach (var id in g_ids)
                {
                    var e = db.Element<Entity>(id);
                    if (groupExtents == null)
                    {
                        groupExtents = e.Bounds;
                        low_left_p = groupExtents.Value.MinPoint;
                        high_right_p = groupExtents.Value.MaxPoint;
                    }
                    else
                    {
                        var cur_bound = (Extents3d)e.Bounds;
                        low_left_p = ThMEPHVACService.Get_min_point(low_left_p, cur_bound.MinPoint);
                        high_right_p = ThMEPHVACService.Get_max_point(high_right_p, cur_bound.MaxPoint);
                    }
                }
            }
        }
        public static Dictionary<ObjectId, Polyline> Read_group_id2geo_dic()
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
                        var type = (string)values.ElementAt(1).Value;
                        if (type == "Tee" || type == "Cross" || type == "Reducing" || type == "Elbow")
                        {
                            var entitys = Get_group_entitys(g);
                            var pl = ThMEPHAVCBounds.getConnectorBounds(entitys, 1);
                            dic.Add(id, pl);
                        }
                    }
                }
            }
            return dic;
        }
        private static DBObjectCollection Get_group_entitys(Group g)
        {
            var entity_ids = g.GetAllEntityIds();
            var entitys = new DBObjectCollection();
            foreach (var e_id in entity_ids)
            {
                var e = e_id.GetDBObject();
                if (!(e is Line))
                    continue;
                entitys.Add(e);
            }
            return entitys;
        }
        public static List<ObjectId> Read_group_ids_by_type(string range)
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
                        var type = (string)values.ElementAt(1).Value;
                        if (type == range)
                            ids.Add(id);
                    }
                }
            }
            return ids;
        }
        public static List<BlockReference> Read_blk_by_name(string blk_name)
        {
            using (var db = AcadDatabase.Active())
            {
                return db.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => !b.BlockTableRecord.IsNull)
                    .Where(b => b.GetEffectiveName().Contains(blk_name)).ToList();
            }
        }
        public static List<ObjectId> Read_blk_ids_by_name(string blk_name)
        {
            return Read_blk_by_name(blk_name).Select(o => o.ObjectId).ToList();
        }
        public static List<DBText> Read_duct_texts()
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
        public static List<AlignedDimension> Read_dimension()
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
        public static List<Leader> Read_leader()
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
        public static Dictionary<string, Point3d> GetPortExtsOfGroup(ObjectId groupId)
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
                        if (type != "PortExt")
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
            foreach (var value in dic.Values)
            {
                var subEntityIds = value.GetAllEntityIds();
                if (subEntityIds.Contains(subObjId))
                {
                    return value.ObjectId;
                }
            }

            return ObjectId.Null;
        }
    }
}