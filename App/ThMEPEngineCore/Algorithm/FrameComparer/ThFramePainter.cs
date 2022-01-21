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
            ImportLayerBlock();
            PreProcLayer();
        }
        private void ImportLayerBlock()
        {
            using (var adb = AcadDatabase.Active())
            {
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    adb.Database.CreateAIDoorLayer();
                    adb.Database.CreateAIWindowLayer();
                    adb.Database.CreateAIRoomOutlineLayer();
                    adb.Database.CreateAIFireCompartmentLayer();
                    adb.Database.CreateAILayer(AppendLayerInfo.AppendDoorLayer, 30);
                    adb.Database.CreateAILayer(AppendLayerInfo.AppendWindowLayer, 30);
                    adb.Database.CreateAILayer(AppendLayerInfo.AppendRoomFrameLayer, 30);
                    adb.Database.CreateAILayer(AppendLayerInfo.AppendFireComponentLayer, 30);
                    adb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineTypeInfo.Hidden), true);
                    adb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineTypeInfo.Continuous), true);
                }
            }
        }
        private void PreProcLayer()
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                GetCurLayer(db, "0");
                GetCurLayer(db, ThMEPEngineCoreLayerUtils.DOOR);
                GetCurLayer(db, ThMEPEngineCoreLayerUtils.WINDOW);
                GetCurLayer(db, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                GetCurLayer(db, ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT);
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
            DrawChangedLine(comp.ChangedFrame, dicCode2Id);
            DrawAppendLine(comp.AppendedFrame.ToCollection(), type);
        }
        public void DrawDeleteLine(DBObjectCollection lines, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            {
                lines.OfType<Polyline>().ForEach(p =>
                {
                    var poly = db.Element<Polyline>(dicCode2Id[p.GetHashCode()], true);
                    poly.ColorIndex = (int)ColorIndex.Red;
                    poly.Linetype = LineTypeInfo.Hidden;
                });
            }
        }
        public void DrawAppendLine(DBObjectCollection lines, CompareFrameType type)
        {
            using (var db = AcadDatabase.Active())
            {
                string layer;
                switch (type)
                {
                    case CompareFrameType.DOOR: layer = AppendLayerInfo.AppendDoorLayer; break;
                    case CompareFrameType.ROOM: layer = AppendLayerInfo.AppendRoomFrameLayer; break;
                    case CompareFrameType.WINDOW: layer = AppendLayerInfo.AppendWindowLayer; break;
                    case CompareFrameType.FIRECOMPONENT: layer = AppendLayerInfo.AppendFireComponentLayer; break;
                    default:throw new NotImplementedException("不支持该类型框线");
                }
                DrawLines(lines, ColorIndex.Green, LineTypeInfo.Continuous, layer);
            }
        }
        public void DrawChangedLine(Dictionary<Polyline, Tuple<Polyline, double>> ChangedFrame, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            {
                var mapLines = new DBObjectCollection();
                foreach (var ppp in ChangedFrame.Values)
                    mapLines.Add(ppp.Item1);
                DrawChangeMapLine(mapLines, dicCode2Id);
                DrawLines(ChangedFrame.Keys.ToCollection(), ColorIndex.Magenta, LineTypeInfo.Continuous, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
            }
        }
        private void DrawChangeMapLine(DBObjectCollection lines, Dictionary<int, ObjectId> dicCode2Id)
        {
            using (var db = AcadDatabase.Active())
            {
                lines.OfType<Polyline>().ForEach(p =>
                {
                    var poly = db.Element<Polyline>(dicCode2Id[p.GetHashCode()], true);
                    poly.ColorIndex = (int)ColorIndex.Yellow;
                    poly.Linetype = LineTypeInfo.Continuous;
                });
            }
        }
        public void DrawLines(DBObjectCollection lines, ColorIndex color, string lineType, string layer)
        {
            using (var db = AcadDatabase.Active())
            {
                if (lines != null)
                {
                    foreach (Curve obj in lines)
                    {
                        var shadow = obj.Clone() as Curve;
                        db.ModelSpace.Add(shadow);
                        shadow.SetDatabaseDefaults();
                        shadow.Layer = layer;
                        shadow.ColorIndex = (int)color;
                        shadow.Linetype = lineType;
                    }
                }
            }
        }
    }
}
