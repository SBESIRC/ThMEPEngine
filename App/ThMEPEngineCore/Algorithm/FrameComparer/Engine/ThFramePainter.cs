using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

using ThMEPEngineCore.Algorithm.FrameComparer.Model;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public static class LineTypeInfo
    {
        public static string Hidden = "Hidden";
        public static string ByLayer = "ByLayer";
        public static string Continuous = "Continuous";
    }
    public static class ThFramePainter
    {
        public static void InitialPainter()
        {
            PreProcLayer();
            ImportLayerBlock();
        }

        private static void ImportLayerBlock()
        {
            using (var db = AcadDatabase.Active())
            {
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    using (db.Database.GetDocument().LockDocument())
                    {
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.DOOR))
                        {
                            db.Database.CreateAIDoorLayer();
                        }
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.WINDOW))
                        {
                            db.Database.CreateAIWindowLayer();
                        }
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.ROOMOUTLINE))
                        {
                            db.Database.CreateAIRoomOutlineLayer();
                        }
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.ROOMMARK))
                        {
                            db.Database.CreateAIRoomMarkLayer();
                        }
                        if (!db.Layers.Contains(ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT))
                        {
                            db.Database.CreateAIFireCompartmentLayer();
                        }
                        //if (!db.Layers.Contains(ThFrameChangedCommon.TempLayer))
                        //{
                        //    db.Database.CreateAILayer(ThFrameChangedCommon.TempLayer, 1);
                        //}
                        db.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineTypeInfo.Hidden), true);
                        db.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(LineTypeInfo.Continuous), true);
                    }
                }
            }
        }
        private static void PreProcLayer()
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    GetCurLayer(db, "0");
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.DOOR);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.WINDOW);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.ROOMMARK);
                    GetCurLayer(db, ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT);
                }
            }
        }
        private static void GetCurLayer(AcadDatabase db, string layerName)
        {
            db.Database.UnFrozenLayer(layerName);
            db.Database.UnLockLayer(layerName);
            db.Database.UnOffLayer(layerName);
        }

        public static void EraseEntity(ObjectId itemId)
        {

            var dbTrans = new DBTransaction();
            var obj = dbTrans.GetObject(itemId, OpenMode.ForWrite, false);
            obj.UpgradeOpen();
            obj.Erase();
            obj.DowngradeOpen();
            dbTrans.Commit();

        }

        public static void AddEntity(Entity item, string layer)
        {
            using (var db = AcadDatabase.Active())
            using (db.Database.GetDocument().LockDocument())
            {
                var print = item.Clone() as Entity;
                print.Linetype = "ByLayer";
                print.Layer = layer;
                print.ColorIndex = (int)ColorIndex.BYLAYER;
                db.ModelSpace.Add(print);
                print.SetDatabaseDefaults();
            }
        }
    }
}
