using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public enum CompareFrameType
    {
        DOOR,WINDOW,ROOM,FIRECOMPONENT
    }
    public class ThFrameExactor
    {
        public DBObjectCollection curGraph;
        public DBObjectCollection reference;
        public Dictionary<int, ObjectId> dicCode2Id;
        public ThFrameExactor(CompareFrameType type)
        {
            switch (type)
            {
                case CompareFrameType.DOOR: GetDoorFrame(); break;
                case CompareFrameType.ROOM: GetRoomFrame(); break;
                case CompareFrameType.WINDOW: GetWindowFrame(); break;
                case CompareFrameType.FIRECOMPONENT: GetFireComponentFrame(); break;
                default: throw new NotImplementedException("不支持该类型框线");
            }
            DoProcCurGraphPl();
            DoProcReferencePl();
        }
        private void DoProcReferencePl()
        {
            var t = new DBObjectCollection();
            foreach (Polyline pl in reference)
            {
                var p = pl.WashClone() as Polyline;
                if (p != null)
                    t.Add(p);
            }
            reference.Clear();
            reference = t;
        }

        private void DoProcCurGraphPl()
        {
            dicCode2Id = new Dictionary<int, ObjectId>();
            var t = new DBObjectCollection();
            foreach (Polyline pl in curGraph)
            {
                var tpl = pl.WashClone() as Polyline;
                if (tpl == null)
                    continue;
                tpl.Closed = true;
                var pls = tpl.MakeValid();
                foreach (Polyline p in pls)
                {
                    t.Add(p);
                    dicCode2Id.Add(p.GetHashCode(), pl.Id);
                }
            }
            curGraph.Clear();
            curGraph = t;
        }
        public void GetRoomFrame()
        {
            using (var adb = AcadDatabase.Active())
            {
                GetCurGraph(adb, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                GetRoomReference(adb);
            }
        }
        public void GetWindowFrame()
        {
            using (var adb = AcadDatabase.Active())
            {
                GetCurGraph(adb, ThMEPEngineCoreLayerUtils.WINDOW);
                GetWindowReference(adb);
            }
        }
        public void GetDoorFrame()
        {
            using (var adb = AcadDatabase.Active())
            {
                GetCurGraph(adb, ThMEPEngineCoreLayerUtils.DOOR);
                GetDoorReference(adb);
            }
        }
        public void GetFireComponentFrame()
        {
            using (var adb = AcadDatabase.Active())
            {
                GetCurGraph(adb, ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT);
                GetFireComponentReference(adb);
            }
        }
        private void GetDoorReference(AcadDatabase adb)
        {
            throw new NotImplementedException();
        }
        private void GetWindowReference(AcadDatabase adb)
        {
            throw new NotImplementedException();
        }
        private void GetFireComponentReference(AcadDatabase adb)
        {
            throw new NotImplementedException();
        }
        private void GetRoomReference(AcadDatabase adb)
        {
            var roomEngine = new ThRoomOutlineRecognitionEngine();
            roomEngine.Recognize(adb.Database, new Point3dCollection());
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            reference = new DBObjectCollection();
            rooms.Select(o => o.Boundary)
                 .OfType<Polyline>()
                 .ForEachDbObject(o => reference.Add(o));
        }
        private void GetCurGraph(AcadDatabase adb, string layer)
        {
            var layerFilter = new List<string>() { layer };
            curGraph = adb.ModelSpace
                .OfType<Polyline>()
                .Where(o => layerFilter.Contains(o.Layer))
                .ToCollection();
        }
    }
}