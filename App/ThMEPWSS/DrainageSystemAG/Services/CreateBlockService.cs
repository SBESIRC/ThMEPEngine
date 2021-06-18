using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.DrainageSystemAG.Services
{
    public static class CreateBlockService
    {
        public static List<ObjectId> CreateBlocks(this Database database, List<CreateBlockInfo> createBlockInfos)
        {
            List<ObjectId> objIds = new List<ObjectId>();

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var item in createBlockInfos)
                {
                    var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                        item.layoutName,
                        item.blockName,
                        item.createPoint,
                        new Scale3d(item.scaleNum),
                        item.rotateAngle,
                        item.attNameValues);
                    if (null == id)
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
                    objIds.Add(id);
                }
            }
            return objIds;
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
    }
    
}
