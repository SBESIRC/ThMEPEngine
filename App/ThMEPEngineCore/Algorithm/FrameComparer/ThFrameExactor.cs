﻿using System;
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
using DotNetARX;
using Dreambuild.AutoCAD;

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
        private Point3dCollection fence;
        public ThFrameExactor(CompareFrameType type, Point3dCollection fence)
        {
            curGraph = new DBObjectCollection();
            reference = new DBObjectCollection();
            var pl = new Polyline();
            pl.CreateRectangle(fence[0].ToPoint2d(), fence[1].ToPoint2d());
            this.fence = pl.Vertices();
            if (type == CompareFrameType.ROOM)
                GetRoomFrame();
            else if (type == CompareFrameType.DOOR)
                GetDoorFrame();
            else if (type == CompareFrameType.WINDOW)
                GetWindowFrame();
            else if (type == CompareFrameType.FIRECOMPONENT)
                GetFireComponentFrame();
            else
                throw new NotImplementedException("未找到该框线类型！！！");
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
            var engine = new ThDoorOutlineRecognitionEngine();
            engine.Recognize(adb.Database, fence);
            engine.doorOutLines.ForEachDbObject(o => reference.Add(o));
        }
        private void GetWindowReference(AcadDatabase adb)
        {
            var engine = new ThWindowOutlineRecognitionEngine();
            engine.Recognize(adb.Database, fence);
            engine.windowsOutLines.ForEachDbObject(o => reference.Add(o));
        }
        private void GetFireComponentReference(AcadDatabase adb)
        {
            var engine = new ThFireCompartmentOutlineRecognitionEngine();
            engine.Recognize(adb.Database, fence);
            var rooms = engine.Elements.Cast<ThIfcRoom>().ToList();
            rooms.Select(o => o.Boundary)
                 .OfType<Polyline>()
                 .ForEachDbObject(o => reference.Add(o));
        }
        private void GetRoomReference(AcadDatabase adb)
        {
            var engine = new ThRoomOutlineRecognitionEngine();
            engine.Recognize(adb.Database, fence);
            var rooms = engine.Elements.Cast<ThIfcRoom>().ToList();
            rooms.Select(o => o.Boundary)
                 .OfType<Polyline>()
                 .ForEachDbObject(o => reference.Add(o));
        }
        private void GetCurGraph(AcadDatabase adb, string layer)
        {
            var layerFilter = new List<string>() { layer };
            var graphs = adb.ModelSpace
                .OfType<Polyline>()
                .Where(o => layerFilter.Contains(o.Layer))
                .ToCollection();
            if (fence.Count > 0)
            {
                // 有框选 fence 是大范围，用polyline求交就可以了，不用MPolygon
                var index = new ThCADCoreNTSSpatialIndex(graphs);
                var curves = index.SelectCrossingPolygon(fence);
                foreach (Polyline p in curves)
                    curGraph.Add(p);
            }
            else
            {
                // 无框选
                foreach (Polyline pl in graphs)
                    curGraph.Add(pl);
            }
        }
    }
}