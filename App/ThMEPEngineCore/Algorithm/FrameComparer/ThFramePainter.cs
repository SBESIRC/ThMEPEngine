using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public static class AppendLayerInfo
    {
        public static string AppendDoorLayer = "AI-门-新增";
        public static string AppendWindowLayer = "AI-窗-新增";
        public static string AppendRoomFrameLayer = "AI-房间框线-新增";
        public static string AppendFireComponentLayer = "AI-防火分区-新增";
    }
    public static class LineTypeInfo
    {
        public static string Hidden = "Hidden";
        public static string ByLayer = "ByLayer";
        public static string Continuous = "Continuous";
    }
    public class ThFramePainter
    {
        public ThFramePainter()
        {
            PreProcLayer();
            ImportLayerBlock();
        }
        private void ImportLayerBlock()
        {
            using (var db = AcadDatabase.Active())
            {
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    using (db.Database.GetDocument().LockDocument())
                    {
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.DOOR))
                            db.Database.CreateAIDoorLayer();
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.WINDOW))
                            db.Database.CreateAIWindowLayer();
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.ROOMOUTLINE))
                            db.Database.CreateAIRoomOutlineLayer();
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT))
                            db.Database.CreateAIFireCompartmentLayer();
                        if (!db.Layers.Contains(AppendLayerInfo.AppendDoorLayer))
                            db.Database.CreateAILayer(AppendLayerInfo.AppendDoorLayer, 30);
                        if (!db.Layers.Contains(AppendLayerInfo.AppendWindowLayer))
                            db.Database.CreateAILayer(AppendLayerInfo.AppendWindowLayer, 30);
                        if (!db.Layers.Contains(AppendLayerInfo.AppendRoomFrameLayer))
                            db.Database.CreateAILayer(AppendLayerInfo.AppendRoomFrameLayer, 30);
                        if (!db.Layers.Contains(AppendLayerInfo.AppendFireComponentLayer))
                            db.Database.CreateAILayer(AppendLayerInfo.AppendFireComponentLayer, 30);
                        db.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineTypeInfo.Hidden), true);
                        db.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineTypeInfo.Continuous), true);
                    }
                }
            }
        }
        private void PreProcLayer()
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    GetCurLayer(db, "0");
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.DOOR);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.WINDOW);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT);
                }
            }
        }
        private void GetCurLayer(AcadDatabase db, string layerName)
        {
            db.Database.UnFrozenLayer(layerName);
            db.Database.UnLockLayer(layerName);
            db.Database.UnOffLayer(layerName);
        }
        public void Draw(ThMEPFrameComparer comp, Dictionary<int, ObjectId> dicCode2Id, CompareFrameType type)
        {
            DrawDeleteLine(comp.ErasedFrame, dicCode2Id);
            DrawChangedLine(comp, dicCode2Id);
            DrawAppendLine(comp, type, dicCode2Id);
            //DrawUnchangedLine(comp, type, dicCode2Id);
        }

        private void DrawUnchangedLine(ThMEPFrameComparer comp, CompareFrameType type, Dictionary<int, ObjectId> dicCode2Id)
        {
            var unchangedFrames = comp.unChangedFrame.Keys.ToCollection();
            DrawLines(ref unchangedFrames, ColorIndex.Cyan, LineTypeInfo.Continuous, ThMEPEngineCoreLayerUtils.ROOMOUTLINE, dicCode2Id);
        }

        public void DrawDeleteLine(DBObjectCollection lines, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            using (db.Database.GetDocument().LockDocument())
            {
                lines.OfType<Polyline>().ForEach(p =>
                {
                    var poly = db.Element<Polyline>(dicCode2Id[p.GetHashCode()], true);
                    poly.ColorIndex = (int)ColorIndex.Red;
                    poly.Linetype = LineTypeInfo.Hidden;
                });
            }
        }
        public void DrawAppendLine(ThMEPFrameComparer comp, CompareFrameType type, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    var appFrames = comp.AppendedFrame.ToCollection();
                    string layer;
                    switch (type)
                    {
                        case CompareFrameType.DOOR: layer = AppendLayerInfo.AppendDoorLayer; break;
                        case CompareFrameType.ROOM: layer = AppendLayerInfo.AppendRoomFrameLayer; break;
                        case CompareFrameType.WINDOW: layer = AppendLayerInfo.AppendWindowLayer; break;
                        case CompareFrameType.FIRECOMPONENT: layer = AppendLayerInfo.AppendFireComponentLayer; break;
                        default: throw new NotImplementedException("不支持该类型框线");
                    }
                    DrawLines(ref appFrames, ColorIndex.Green, LineTypeInfo.Continuous, layer, dicCode2Id);
                    comp.AppendedFrame.Clear();
                    foreach (Polyline frame in appFrames)
                        comp.AppendedFrame.Add(frame);
                }
            }
        }
        public void DrawChangedLine(ThMEPFrameComparer comp, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    var ChangedFrame = comp.ChangedFrame;
                    var mapLines = new DBObjectCollection();
                    foreach (var ppp in ChangedFrame.Values)
                        mapLines.Add(ppp.Item1);
                    DrawChangeMapLine(mapLines, dicCode2Id);
                    var frames = ChangedFrame.Keys.ToCollection();
                    DrawLines(ref frames, ColorIndex.Magenta, LineTypeInfo.Continuous, ThMEPEngineCoreLayerUtils.ROOMOUTLINE, dicCode2Id);
                    var tChangedFrame = new Dictionary<Polyline, Tuple<Polyline, double>>();
                    int i = 0;
                    foreach (var frame in ChangedFrame.Values)
                    {
                        tChangedFrame.Add(frames[i++] as Polyline, frame);
                    }
                    comp.ChangedFrame = tChangedFrame;
                }
            }
        }
        private void DrawChangeMapLine(DBObjectCollection lines, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    lines.OfType<Polyline>().ForEach(p =>
                    {
                        var poly = db.Element<Polyline>(dicCode2Id[p.GetHashCode()], true);
                        poly.ColorIndex = (int)ColorIndex.Yellow;
                        poly.Linetype = LineTypeInfo.Continuous;
                    });
                }
            }
        }
        public void DrawLines(ref DBObjectCollection lines, ColorIndex color, string lineType, string layer, Dictionary<int, ObjectId> dicCode2Id)
        {
            // dicCode2Id主要用于将新画的多段线加到字典中
            using (var db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    if (lines != null)
                    {
                        var newLines = new DBObjectCollection();
                        foreach (Curve obj in lines)
                        {
                            var shadow = obj.Clone() as Curve;
                            newLines.Add(shadow);
                            var id = db.ModelSpace.Add(shadow);
                            dicCode2Id.Add(shadow.GetHashCode(), id);
                            shadow.SetDatabaseDefaults();
                            shadow.Layer = layer;
                            shadow.ColorIndex = (int)color;
                            shadow.Linetype = lineType;
                        }
                        lines.Clear();
                        lines = newLines;
                    }
                }
            }
        }
    }
}
