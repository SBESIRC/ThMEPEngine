using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.DrainageSystemAG.Services
{
    static class ClearLoadBlockServices
    {
        static List<string> blockNames = new List<string>()
        {
            ThWSSCommon.Layout_FloorDrainBlockName,
            ThWSSCommon.Layout_PositionRiserBlockName,
            ThWSSCommon.Layout_PositionRiser150BlockName,
            ThWSSCommon.Layout_PipeCasingBlockName,
            ThWSSCommon.Layout_CleanoutBlockName,
        };
        static List<string> layerNames = new List<string>()
        {
            ThWSSCommon.Layout_PipeRainDrainConnectLayerName,
            ThWSSCommon.Layout_PipeWastDrainConnectLayerName,
            ThWSSCommon.Layout_FloorDrainBlockRainLayerName,
            ThWSSCommon.Layout_PipeWastDrainTextLayerName,
            ThWSSCommon.Layout_PipeRainTextLayerName,
            ThWSSCommon.Layout_PipeCasingLayerName,
            ThWSSCommon.Layout_PipeCasingTextLayerName,
        };
        static List<string> textStyleNames = new List<string>
        {
            ThWSSCommon.Layout_TextStyle
        };
        public static void ClearHisFloorBlock(this Database database,List<Polyline> selectFloors) 
        {
            if (null == selectFloors || selectFloors.Count < 1)
                return;
            //这里是删除该框线的块，直接删除块定义
            
            //获取生成的可能是要删除的块
            var blockReferences = new List<BlockReference>();
            using (AcadDatabase acdb = AcadDatabase.Use(database))
            {
                var allBlockReference = acdb.ModelSpace.OfType<BlockReference>();

                foreach (var block in allBlockReference)
                {
                    if (block == null || block.BlockTableRecord == null || !block.BlockTableRecord.IsValid)
                        continue;
                    if (!block.GetEffectiveName().ToUpper().Contains(DrainSysAGCommon.BLOCKNAMEPREFIX))
                        continue;
                    blockReferences.Add(block);
                }
                var delBlocks = new List<BlockReference>();
                foreach (var item in selectFloors)
                {
                    foreach (var block in blockReferences)
                    {
                        if (delBlocks.Any(c => c.BlockTableRecord == block.BlockTableRecord))
                            continue;
                        var blockPoint = new Point3d(block.Position.X, block.Position.Y, item.StartPoint.Z);
                        if (item.Contains(blockPoint))
                        {
                            delBlocks.Add(block);
                        }
                    }
                }
                if (delBlocks.Count < 1)
                    return;
                var delBlIds = new List<ObjectId>();
                foreach (var block in delBlocks)
                {
                    if (!delBlIds.Any(c => c.Equals(block.BlockTableRecord)))
                        delBlIds.Add(block.BlockTableRecord);
                    block.UpgradeOpen();
                    block.Erase();
                }
                foreach (var blId in delBlIds)
                {
                    var blockRecord = acdb.Blocks.Element(blId);
                    blockRecord.UpgradeOpen();
                    blockRecord.Erase();
                }
            }
            
        }
        public static void LoadBlockLayerToDocument(this Database database) 
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, false);
                }
                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, true);
                }
                foreach (var item in textStyleNames) 
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var currentStyle = currentDb.TextStyles.ElementOrDefault(item);
                    if (null != currentStyle)
                        continue;
                    var style = blockDb.TextStyles.ElementOrDefault(item);
                    if (style == null)
                        continue;
                    currentDb.TextStyles.Import(style);
                }
            }
        }
    }
}
