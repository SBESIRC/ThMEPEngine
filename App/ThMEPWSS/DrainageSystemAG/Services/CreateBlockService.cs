using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Assistant;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.DrainageSystemAG.Services
{
    public static class CreateBlockService
    {
        public static List<CreateResult> CreateBlocks(this Database database, List<CreateBlockInfo> createBlockInfos)
        {
            List<CreateResult> createRes = new List<CreateResult>();

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in createBlockInfos)
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
                    createRes.Add(new CreateResult(id, item.createPoint, item.equipmentType, item.floorId, item.tag));
                }
            }
            return createRes;
        }
        public static ObjectId CreateBlock(this Database database, Point3d pt, Vector3d layoutDir, double scaleNum, double rotateAngle, string layerName, string blockName, Dictionary<string, string> attNameValues)
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

        public static List<CreateResult> CreateBasicElement(this Database database,List<CreateBasicElement> basicElements) 
        {
            List<CreateResult> createResults = new List<CreateResult>();

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in basicElements)
                {
                    var path = item.baseCurce;
                    path.Layer = item.layerName;
                    if (null != item.lineColor)
                    {
                        path.Color = item.lineColor;
                        path.LineWeight = LineWeight.LineWeight050;
                        path.CastShadows = true;
                    }
                    var id= acadDatabase.ModelSpace.Add(path);
                    if (null == id || !id.IsValid)
                        continue;
                    createResults.Add(new CreateResult(id, item.baseCurce.StartPoint, EnumEquipmentType.other, item.floorId, ""));
                }
            }
            return createResults;
        }

        public static List<CreateResult> CreateTextElement(this Database database, List<CreateDBTextElement> basicElements) 
        {
            var createResults = new List<CreateResult>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in basicElements)
                {
                    var id = acadDatabase.ModelSpace.Add(item.dbText);
                    if (null == id || !id.IsValid)
                        continue;
                    if (!string.IsNullOrEmpty(item.textStyle)) 
                    {
                        try 
                        {
                            var dbText = acadDatabase.Element<DBText>(id);
                            DrawUtils.SetTextStyle(dbText, item.textStyle);
                        }
                        catch (Exception ex) 
                        {
                        
                        }
                        
                    }
                    createResults.Add(new CreateResult(id, item.dbText.Position, EnumEquipmentType.other, item.floorUid, ""));
                }
            }
            return createResults;
        }
    }
    
}
