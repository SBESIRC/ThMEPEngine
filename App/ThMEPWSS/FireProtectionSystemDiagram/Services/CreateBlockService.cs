using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPWSS.Assistant;
using ThMEPWSS.FireProtectionSystemDiagram.Models;

namespace ThMEPWSS.FireProtectionSystemDiagram.Services
{
    public static class CreateBlockService
    {
        static List<string> blockNames = new List<string>()
        {
            ThWSSCommon.Layout_FireHydrantBlockName,
            ThWSSCommon.Layout_ButterflyValveBlcokName,
            ThWSSCommon.Layout_LevelBlockName,
            ThWSSCommon.Layout_CheckValveBlockName,
            ThWSSCommon.Layout_ShutOffValve,
            ThWSSCommon.Layout_SafetyValve,
            ThWSSCommon.Layout_ExhaustValveSystemBlockName,
            ThWSSCommon.Layout_ConnectionReserveBlcokName,
            FireProtectionSysCommon.LayoutPipeInterruptedBlcokName,
        };
        static List<string> layerNames = new List<string>()
        {
            ThWSSCommon.Layout_LevelLayerName,//标高文字样式
            ThWSSCommon.Layout_FireHydrantTextLayerName,//"W-FRPT-HYDT-DIMS",//标高线图层
             ThWSSCommon.Layout_FireHydrantPipeLineLayerName, //"W-FRPT-HYDT-PIPE",//立管线图层
            "W-FRPT-NOTE",//立管线图层样式X3L-A
            "W-WSUP-NOTE",//标高文字图层
            "W-FRPT-HYDT-EQPM",
        };
        static List<string> textStyleNames = new List<string>
        {
            ThWSSCommon.Layout_TextStyle
        };
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
                    currentDb.Layers.Import(layer, false);
                    DbHelper.EnsureLayerOn(item);
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
        public static List<ObjectId> CreateBlocks(this Database database, List<CreateBlockInfo> createBlockInfos)
        {
            var createRes = new List<ObjectId>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in createBlockInfos)
                {
                    try
                    {
                        var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        item.layerName,
                        item.blockName,
                        item.createPoint,
                        new Scale3d(item.scaleNum),
                        item.rotateAngle,
                        item.attNameValues);
                        if (null == id || !id.IsValid)
                            continue;
                        if (null != item.dymBlockAttr && item.dymBlockAttr.Count > 0)
                        {
                            foreach (var dyAttr in item.dymBlockAttr)
                            {
                                if (dyAttr.Key == null || dyAttr.Value == null)
                                    continue;
                                id.SetDynBlockValue(dyAttr.Key, dyAttr.Value);
                            }
                        }
                        createRes.Add(id);
                    }
                    catch (Exception ex) 
                    { }
                }
            }
            return createRes;
        }
        public static ObjectId CreateBlock(this Database database, Point3d pt, double scaleNum, double rotateAngle, string layerName, string blockName, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layerName,
                    blockName,
                    pt,
                    new Scale3d(scaleNum),
                    rotateAngle,
                    attNameValues);
                return id;
            }
        }

        public static List<ObjectId> CreateBasicElement(this Database database,List<CreateBasicElement> basicElements) 
        {
            var createResults = new List<ObjectId>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in basicElements)
                {
                    try
                    {
                        var path = item.baseCurce;
                        path.Layer = item.layerName;
                        if (null != item.lineColor)
                        {
                            path.Color = item.lineColor;
                            path.LineWeight = LineWeight.LineWeight050;
                            path.CastShadows = true;
                        }
                        var id = acadDatabase.ModelSpace.Add(path);
                        if (null == id || !id.IsValid)
                            continue;
                        createResults.Add(id);
                    }
                    catch (Exception ex) 
                    { 
                    }
                }
            }
            return createResults;
        }

        public static List<ObjectId> CreateTextElement(this Database database, List<CreateDBTextElement> basicElements) 
        {
            var createResults = new List<ObjectId>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in basicElements)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(item.layerName))
                            item.dbText.Layer = item.layerName;
                        var id = acadDatabase.ModelSpace.Add(item.dbText);
                        if (null == id || !id.IsValid)
                            continue;
                        if (!string.IsNullOrEmpty(item.textStyle))
                        {
                            var dbText = acadDatabase.Element<DBText>(id);
                            DrawUtils.SetTextStyle(dbText, item.textStyle);
                        }
                        createResults.Add(id);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return createResults;
        }
    }
    
}
