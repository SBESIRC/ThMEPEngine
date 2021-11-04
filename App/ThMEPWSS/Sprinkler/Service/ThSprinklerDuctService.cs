using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;

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

        public static void GetDuctsParam(out Dictionary<Polyline, DuctModifyParam> dic, Database database, Matrix3d matrix)
        {
            dic = new Dictionary<Polyline, DuctModifyParam>();
            var tor = new Tolerance(1.5, 1.5);
            foreach (var id in ThHvacGetComponent.ReadDuctIds(database))
            {
                var param = ThHvacAnalysisComponent.GetDuctParamById(id);
                if (param.handle == ObjectId.Null.Handle || param.sp.IsEqualTo(param.ep, tor))
                    continue;
                var poly = GetGeometry(id);
                if (poly != null) 
                {
                    poly.TransformBy(matrix);
                    dic.Add(poly, param);
                }
            }
        }

        private static Polyline GetGeometry(ObjectId groupId)
        {
            var ids = Dreambuild.AutoCAD.DbHelper.GetEntityIdsInGroup(groupId);
            var results = ids.Select(o => o.GetObject(OpenMode.ForRead))
                    .OfType<Curve>()
                    .Where(o => !o.Layer.Contains("H-DUCT-DUAL-MID"))
                    .ToCollection();
            var outline = results
                    .Buffer(0.01)
                    .OfType<Polyline>()
                    .OrderByDescending(o => o.Area)
                    .FirstOrDefault();
            return outline;
        }

        public static void GetFittingsParam(out Dictionary<Polyline, EntityModifyParam> dic, Database database, Matrix3d matrix)
        {
            dic = new Dictionary<Polyline, EntityModifyParam>();
            var geometryDictionary = CreateGeometryDictionary(database, matrix);
            foreach (var id in geometryDictionary.Keys)
            {
                var param = ThHvacAnalysisComponent.GetConnectorParamById(id);
                if (param.handle != ObjectId.Null.Handle)
                {
                    dic.Add(geometryDictionary[id], param);
                }
            }
        }

        private static Dictionary<ObjectId, Polyline> CreateGeometryDictionary(Database database, Matrix3d matrix)
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
                                var entities = GetGroupEntities(g);
                                var results = entities.OfType<Curve>()
                                        .Where(o => !o.Layer.Contains("H-DUCT-DUAL-MID"))
                                        .ToCollection();
                                var outline = results
                                        .Buffer(0.01)
                                        .OfType<Polyline>()
                                        .OrderByDescending(o => o.Area)
                                        .FirstOrDefault();
                                if (outline != null) 
                                {
                                    outline.TransformBy(matrix);
                                    dic.Add(id, outline);
                                }
                            }
                        }
                    }
                }
            }
            return dic;
        }

        private static DBObjectCollection GetGroupEntities(Group g)
        {
            var entity_ids = g.GetAllEntityIds();
            var entities = new DBObjectCollection();
            foreach (var e_id in entity_ids)
            {
                var e = e_id.GetDBObject();
                entities.Add(e);
            }
            return entities;
        }
    }
}
