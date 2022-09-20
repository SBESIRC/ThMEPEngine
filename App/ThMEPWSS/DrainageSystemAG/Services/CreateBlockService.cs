using AcHelper;
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
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                foreach (var item in createBlockInfos)
                {
                    try
                    {
                        var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        item.layerName,
                        item.blockName,
                        item.createPoint,
                        new Scale3d(item.scaleNum),
                        item.rotateAngle+angle,
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
                        var createResult = new CreateResult(id, item.createPoint, item.equipmentType, item.floorId, item.tag, item.layerName,item.belongBlockId);
                        createRes.Add(createResult);
                    }
                    catch (Exception ex) 
                    {                    
                    }                    
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
                        var createResult = new CreateResult(id, item.baseCurce.StartPoint, EnumEquipmentType.other, item.floorId, item.curveTag, item.layerName,item.belongBlockId);
                        createResults.Add(createResult);
                    }
                    catch (Exception ex) 
                    { 
                    
                    }
                    
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
                    try
                    {
                        var id = acadDatabase.ModelSpace.Add(item.dbText);
                        if (null == id || !id.IsValid)
                            continue;
                        if (!string.IsNullOrEmpty(item.textStyle))
                        {
                            var dbText = acadDatabase.Element<DBText>(id);
                            DrawUtils.SetTextStyle(dbText, item.textStyle);
                        }
                        createResults.Add(new CreateResult(id, item.dbText.Position, EnumEquipmentType.other, item.floorUid, item.Tag, item.layerName,item.belongBlockId));
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
