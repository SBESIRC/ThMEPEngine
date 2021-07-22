using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReadComponent
    {
        public static DBObjectCollection Read_all_component()
        {
            using (var db = AcadDatabase.Active())
            {
                var comps = new DBObjectCollection();
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var list = g.ObjectId.GetXData("Info");
                    if (list != null)
                    {
                        var poly = new Polyline();
                        var bound = g.Bounds.Value;
                        poly.CreateRectangle(bound.MinPoint.ToPoint2D(), bound.MaxPoint.ToPoint2D());
                        comps.Add(poly);
                    }
                }
                return comps;
            }
        }
        public static List<ObjectId> Read_shape_ids()
        {
            var ids = new List<ObjectId>();
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var id = g.ObjectId;
                    var list = id.GetXData("Info");
                    if (list != null)
                    {
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(1).Value;
                        if (type == "Tee" || type == "Cross" || type == "Reducing" || type == "Elbow")
                            ids.Add(id);
                    }
                }
            }
            return ids;
        }
        public static List<ObjectId> Read_duct_ids()
        {
            var ids = new List<ObjectId>();
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                foreach (var g in groups)
                {
                    var id = g.ObjectId;
                    var list = id.GetXData("Info");
                    if (list != null)
                    {
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(1).Value;
                        if (type == "Duct")
                            ids.Add(id);
                    }
                }
            }
            return ids;
        }
        public static List<ObjectId> Read_valve_ids()
        {
            var ids = new List<ObjectId>();
            using (var db = AcadDatabase.Active())
            {
                var blks = db.ModelSpace.OfType<BlockReference>();
                var blkName = "风阀";
                var valveBlks = blks.Where(b => b.GetEffectiveName().Contains(blkName));
                ids.AddRange(valveBlks.Select(blk => blk.ObjectId));
            }
            return ids;
        }
        public static List<DBText> Read_texts()
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
                foreach(var id in allObjIdsInGroup)
                {
                    var entity = db.Element<Entity>(id);
                    if (!(entity is DBPoint))
                        continue;
                    var ptXdata = id.GetXData("Info");
                    if (ptXdata != null)
                    {
                        var values = ptXdata.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type != "Port")
                            continue;
                        var portIndex = (string)values.ElementAt(1).Value; // 0, 1 ,2
                        var portWidth = (string)values.ElementAt(2).Value;
                        rst.Add(portIndex, new Tuple<Point3d,string>((entity as DBPoint).Position, portWidth));
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
                    var ptXdata = id.GetXData("Info");
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
                    var info = id.GetXData("Info");
                    if (info != null)
                        dic.Add(id, g);
                }
            }
            return dic;
        }

        public static ObjectId GetGroupIdsBySubEntityId(ObjectId subObjId)
        {
            var dic = GetObjectId2GroupDic();
            foreach(var value in dic.Values)
            {
                var subEntityIds = value.GetAllEntityIds();
                if(subEntityIds.Contains(subObjId))
                {
                    return value.ObjectId;
                }
            }

            return ObjectId.Null;
        }

        public void Read_groups()
        {
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                foreach(var g in groups)
                {
                    var id = g.ObjectId;
                    var list = id.GetXData("Info");
                    if (list != null)
                    {
                        Analysis_group(g, db);
                    }
                }
            }
        }
        private void Analysis_group(Group g, AcadDatabase db)
        {
            Recognize_primitives(g, db, out List<Arc> arcs, out List<Line> lines);
            Recognize_shape(arcs, lines);
        }
        private void Recognize_primitives(Group g, 
                                          AcadDatabase db,
                                          out List<Arc> arcs,
                                          out List<Line> lines)
        {
            arcs = new List<Arc>();
            lines = new List<Line>();
            var ids = g.GetAllEntityIds();
            foreach (var id in ids)
            {
                var e = db.Element<Entity>(id);
                if (e is Line)
                    lines.Add(e as Line);
                else if (e is Arc)
                    arcs.Add(e as Arc);
                else
                    throw new NotImplementedException();
            }
        }
        private void Recognize_shape(List<Arc> arcs, List<Line> lines)
        {
            if (arcs.Count == 0)
            {
                if (lines.Count == 5)
                    ThDuctPortsPrimitiveFence.Create_line_fence(lines);
                else
                    throw new NotImplementedException();
            }
            else
            {

            }
        }
#if false
        //private Polyline GetGroupPolyline(ObjectId groupId)
        //{
        //Polyline pl;

        //todo:
        //due to group type, compute poly

        //return pl;
        //}
        private void Init(List<ObjectId> groupIds)
        {
            //Extents3d.ToPolyline()
            var allGroupExtents = new List<Polyline>();
            var _PL2GroupIdDic = new Dictionary<Polyline, ObjectId>();
            var groupPlObjs = new DBObjectCollection();

            foreach (var id in groupIds)
            {
                //todo: get all polyline of group with 'id'
                var pl = GetGroupPolyline(id);
                _PL2GroupIdDic.Add(pl, id);
                groupPlObjs.Add(pl);
            }
            var _GroupPlSpatialIndex = new ThCADCoreNTSSpatialIndex(groupPlObjs);

            //todo: _GroupPlSpatialIndex.SelectCrossingPolygon(selectionWnd)
        }
#endif
    }
}
