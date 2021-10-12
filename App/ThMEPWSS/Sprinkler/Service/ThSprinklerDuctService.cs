using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Sprinkler.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerDuctService
    {
        public static Database QueryXRefDatabase(Database database, ObjectId blkRecordId)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                XrefGraph xg = acadDatabase.Database.GetHostDwgXrefGraph(true);
                GraphNode root = xg.RootNode;
                return Query(root, acadDatabase, blkRecordId);
            }
        }

        private static Database Query(GraphNode i_root, AcadDatabase acadDb, ObjectId objId)
        {
            for (int o = 0; o < i_root.NumOut; o++)
            {
                XrefGraphNode child = i_root.Out(o) as XrefGraphNode;
                if (child.XrefStatus == XrefStatus.Resolved)
                {
                    if (child.BlockTableRecordId.Equals(objId))
                    {
                        return child.Database;
                    }
                    var db = Query(child, acadDb, objId);
                    if (db != null)
                    {
                        return db;
                    }
                }
            }
            return null;
        }

        public static void Get_ducts_dic(out Dictionary<Polyline, DuctModifyParam> dic, Database database, Matrix3d matrix)
        {
            dic = new Dictionary<Polyline, DuctModifyParam>();
            var ductIds = Read_group_ids_by_type("Duct", database);
            var tor = new Tolerance(1.5, 1.5);
            foreach (var id in ductIds)
            {
                var param = Get_duct_by_id(id, database);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep, tor))
                    continue;
                var poly = GetGeometry(id);
                poly.TransformBy(matrix);
                dic.Add(poly, param);
            }
        }

        private static Polyline GetGeometry(ObjectId groupId)
        {
            var ids = Dreambuild.AutoCAD.DbHelper.GetEntityIdsInGroup(groupId);
            var collection = ids.Select(o => o.GetObject(OpenMode.ForRead))
                                .OfType<Curve>()
                                .Where(o => !o.Layer.Contains("H-DUCT-DUAL-MID"))
                                .ToCollection()
                                .Outline();
            return collection[0] as Polyline;
        }

        public static void Get_shapes_dic(out Dictionary<Polyline, EntityModifyParam> dic, Database database, Matrix3d matrix)
        {
            dic = new Dictionary<Polyline, EntityModifyParam>();
            var id2geo_dic = Read_group_id2geo_dic(database, matrix);
            foreach (var id in id2geo_dic.Keys)
            {
                var param = Get_shape_by_id(id, database);
                if (param.handle != ObjectId.Null.Handle)
                    dic.Add(id2geo_dic[id], param);
            }
        }

        private static List<ObjectId> Read_group_ids_by_type(string range, Database database)
        {
            var ids = new List<ObjectId>();
            using (var db = AcadDatabase.Use(database))
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

        private static DuctModifyParam Get_duct_by_id(ObjectId id, Database database)
        {
            var ids = new ObjectId[] { id };
            var duct_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_duct_param(duct_list, id.Handle, database);
        }

        private static TypedValueList Do_get_value_list(IEnumerable<ObjectId> g_ids, string reg_app_name)
        {
            var list = new TypedValueList();
            foreach (var g_id in g_ids)
            {
                list = g_id.GetXData(reg_app_name);
                if (list == null)
                    continue;
                break;
            }
            return list;
        }

        private static DuctModifyParam Get_duct_param(TypedValueList list, Handle group_handle, Database database)
        {
            var param = new DuctModifyParam();
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
                return param;
            if (values.Count() != 5)
                return param;
            int inc = 0;
            param.handle = group_handle;
            param.start_handle = Covert_obj_to_handle(values.ElementAt(inc++).Value);
            param.type = (string)values.ElementAt(inc++).Value;
            if (param.type != "Duct" && param.type != "Vertical_bypass")
                return param;
            param.air_volume = Double.Parse((string)values.ElementAt(inc++).Value);
            param.elevation = Double.Parse((string)values.ElementAt(inc++).Value);
            param.duct_size = (string)values.ElementAt(inc++).Value;
            using (var db = AcadDatabase.Use(database))
            {
                var id = db.Database.GetObjectId(false, group_handle, 0);
                var portIndex2PositionDic = GetPortsOfGroup(id, database);
                if (portIndex2PositionDic.Count == 0)
                    return param;
                param.sp = portIndex2PositionDic["0"].Item1.ToPoint2D();
                param.ep = portIndex2PositionDic["1"].Item1.ToPoint2D();
            }
            return param;
        }

        private static Handle Covert_obj_to_handle(object o)
        {
            return new Handle(Convert.ToInt64((string)o, 16));
        }

        private static Dictionary<string, Tuple<Point3d, string>> GetPortsOfGroup(ObjectId groupId, Database database)
        {
            var rst = new Dictionary<string, Tuple<Point3d, string>>();

            var id2GroupDic = GetObjectId2GroupDic(database);
            if (!id2GroupDic.ContainsKey(groupId))
                return rst;
            var groupObj = id2GroupDic[groupId];
            var allObjIdsInGroup = groupObj.GetAllEntityIds();
            using (var db = AcadDatabase.Use(database))
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

        private static Dictionary<ObjectId, Group> GetObjectId2GroupDic(Database database)
        {
            var dic = new Dictionary<ObjectId, Group>();
            using (var db = AcadDatabase.Use(database))
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

        private static Dictionary<ObjectId, Polyline> Read_group_id2geo_dic(Database database, Matrix3d matrix)
        {
            var dic = new Dictionary<ObjectId, Polyline>();
            using (var db = AcadDatabase.Use(database))
            {
                var groups = db.Groups;
                using (var ov = new ThCADCoreNTSArcTessellationLength(100))
                {
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
                                var entities = Get_group_entitys(g);
                                var collection = entities.OfType<Curve>()
                                                         .Where(o => !o.Layer.Contains("H-DUCT-DUAL-MID"))
                                                         .ToCollection()
                                                         .Outline();
                                var pline = collection[0] as Polyline;
                                pline.TransformBy(matrix);
                                dic.Add(id, pline);
                            }
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
                entitys.Add(e);
            }
            return entitys;
        }

        private static EntityModifyParam Get_shape_by_id(ObjectId id, Database database)
        {
            var ids = new ObjectId[] { id };
            var entity_list = Do_get_value_list(ids, ThHvacCommon.RegAppName_Duct_Info);
            return Get_entity_param(entity_list, id.Handle, database);
        }

        private static EntityModifyParam Get_entity_param(TypedValueList list, Handle group_handle, Database database)
        {
            var param = new EntityModifyParam();
            if (list.Count > 0)
            {
                var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                int inc = 0;
                param.handle = group_handle;
                param.start_id = Covert_obj_to_handle(values.ElementAt(inc++).Value);
                param.type = (string)values.ElementAt(inc++).Value;
                if (!values.Any() || param.type == "Duct")
                    return param;
                using (var db = AcadDatabase.Use(database))
                {
                    var id = db.Database.GetObjectId(false, group_handle, 0);
                    var portIndex2PositionDic = GetPortsOfGroup(id, database);
                    var portExtIndex2PositionDic = GetPortExtsOfGroup(id, database);
                    for (int i = 0; i < portIndex2PositionDic.Count; ++i)
                    {
                        param.pos.Add(portIndex2PositionDic[i.ToString()].Item1.ToPoint2D());
                        param.pos_ext.Add(portExtIndex2PositionDic[i.ToString()].ToPoint2D());
                        param.port_widths.Add(Double.Parse(portIndex2PositionDic[i.ToString()].Item2));
                    }
                }
            }
            return param;
        }

        private static Dictionary<string, Point3d> GetPortExtsOfGroup(ObjectId groupId, Database database)
        {
            var rst = new Dictionary<string, Point3d>();

            var id2GroupDic = GetObjectId2GroupDic(database);
            if (!id2GroupDic.ContainsKey(groupId))
                return rst;

            var groupObj = id2GroupDic[groupId];

            var allObjIdsInGroup = groupObj.GetAllEntityIds();
            using (var db = AcadDatabase.Use(database))
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
    }
}
