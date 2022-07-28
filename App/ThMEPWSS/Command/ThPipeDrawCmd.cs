using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.Command
{
    public class PipeBlockNames
    {
        public const string RoomWaterPipe = "屋面雨水立管-AI";
        public const string btnBalconyPipe = "阳台立管-AI";
        public const string CondensatePipe = "冷凝水立管-AI";
        public const string FloorDrain = "地漏-AI";
        public const string SewageWastePipe = "污废合流立管-AI";
        public const string WasteWaterPipe = "废水立管-AI";
        public const string SewageWaterPipe = "污水立管-AI";
        public const string VentilatePipe = "通气立管-AI";
        public const string CaissonPipe = "沉箱立管-AI";
        public const string RoomCondensateFloorDrain = "屋面+冷凝+地漏-AI";
        public const string CondensateFloorDrain = "冷凝+地漏-AI";
        public const string BalconyCondensateFloorDrain = "阳台+冷凝+地漏-AI";
        public const string RoomBalconyFloorDrain = "屋面+阳台+地漏-AI";
        public const string BalconyFloorDrain = "阳台+地漏-AI";
        public const string SewageWasteFloorDrain = "污废+通气-AI";
        public const string WasteVentilateSewageWaste = "废水+通气+污废合流-AI";
        public const string WasteVentilateSewage = "废水+通气+污水-AI";
    }
    public class ThPipeDrawCmd : IAcadCommand, IDisposable
    {
        public string BlockName { private get; set; }
        public string PipeDN { private get; set; }
        public void Dispose()
        {
        }
        public void ImportBlockFile()
        {
            //导入一个块
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (!acadDb.Blocks.Contains(PipeBlockNames.RoomWaterPipe) && blockDb.Blocks.Contains(PipeBlockNames.RoomWaterPipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.RoomWaterPipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.btnBalconyPipe) && blockDb.Blocks.Contains(PipeBlockNames.btnBalconyPipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.btnBalconyPipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.CondensatePipe) && blockDb.Blocks.Contains(PipeBlockNames.CondensatePipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.CondensatePipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.FloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.FloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.FloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.SewageWastePipe) && blockDb.Blocks.Contains(PipeBlockNames.SewageWastePipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.SewageWastePipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.WasteWaterPipe) && blockDb.Blocks.Contains(PipeBlockNames.WasteWaterPipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.WasteWaterPipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.SewageWaterPipe) && blockDb.Blocks.Contains(PipeBlockNames.SewageWaterPipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.SewageWaterPipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.VentilatePipe) && blockDb.Blocks.Contains(PipeBlockNames.VentilatePipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.VentilatePipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.CaissonPipe) && blockDb.Blocks.Contains(PipeBlockNames.CaissonPipe))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.CaissonPipe));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.RoomCondensateFloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.RoomCondensateFloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.RoomCondensateFloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.CondensateFloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.CondensateFloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.CondensateFloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.BalconyCondensateFloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.BalconyCondensateFloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.BalconyCondensateFloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.RoomBalconyFloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.RoomBalconyFloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.RoomBalconyFloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.BalconyFloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.BalconyFloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.BalconyFloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.SewageWasteFloorDrain) && blockDb.Blocks.Contains(PipeBlockNames.SewageWasteFloorDrain))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.SewageWasteFloorDrain));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.WasteVentilateSewageWaste) && blockDb.Blocks.Contains(PipeBlockNames.WasteVentilateSewageWaste))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.WasteVentilateSewageWaste));
                }
                if (!acadDb.Blocks.Contains(PipeBlockNames.WasteVentilateSewage) && blockDb.Blocks.Contains(PipeBlockNames.WasteVentilateSewage))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(PipeBlockNames.WasteVentilateSewage));
                }
            }
        }
        public void Execute()
        {
            try
            {
                string layerName = "W-辅助";
                ThMEPWSS.Common.Utils.FocusMainWindow();
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    ImportBlockFile();
                    CheckCreateLayer(layerName, Color.FromColorIndex(ColorMethod.ByLayer,253));
                    while (true)
                    {

                        using (var acadDb = Linq2Acad.AcadDatabase.Active())
                        {
                            var insertPtRst = Active.Editor.GetPoint("Specify Next Point (Press ESC to quit)\n");
                            if (insertPtRst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                                break;

                            var pt = insertPtRst.Value;
                            var blk = InsertBlock(layerName, BlockName, pt);
                            if (BlockName.Contains("+"))
                            {
                                blk.Erase();
                                var ents = blk.ExplodeToDBObjectCollection().Cast<BlockReference>().ToList();
                                var blockTableRecord = acadDb.Blocks.Element(blk.BlockTableRecord);
                                foreach (var objId in blockTableRecord)
                                {
                                    var dbObj = acadDb.Element<BlockReference>(objId);
                                    try
                                    {
                                        var name = dbObj.GetEffectiveName();
                                        var position = ents.Where(e => e.Name.Equals(dbObj.Name)).First().Position;
                                        var br = InsertBlock(layerName, name, position);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        BlockReference InsertBlock(string layerName, string name, Point3d pt)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(layerName, name, pt, new Scale3d(1, 1, 1), 0);
                var blk = acadDb.Element<BlockReference>(blkId);
                blk.TransformBy(Active.Editor.UCS2WCS());
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性1")
                        {
                            property.Value = "DN100";
                            break;
                        }
                    }
                }
                return blk;
            }
        }
        bool CheckCreateLayer(string aimLayer, Color color)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }
                // 创建新的图层
                if (layerRecord == null)
                    layerRecord = db.Layers.Create(aimLayer);
                if (layerRecord != null)
                {
                    layerRecord.UpgradeOpen();
                    layerRecord.Color = color;
                    layerRecord.IsOff = false;
                    layerRecord.IsPlottable = false;
                    layerRecord.IsHidden = false;
                    layerRecord.IsLocked = false;
                    if (!layerRecord.IsUsed)
                    {
                        //当前图层正在使用，无法进行冻结,也不能设置冻结，会报无效图层问题
                        layerRecord.IsFrozen = false;
                    }
                    layerRecord.DowngradeOpen();
                }
            }
            return layerRecord != null;
        }
    }
}
