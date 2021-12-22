using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.DSFEL.Model;

namespace ThMEPLighting.DSFEL.Service
{
    public static class InsertBlockService
    {
        public static ObjectId InsertBlock(string layerName, string blockName, Point3d point, double angle, double scale, Dictionary<string, string> dic)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(blockName, layerName);
                var id = db.Database.InsertModel(point, angle, layerName, blockName, scale, dic);
                return id;
            }
        }

        public static ObjectId InsertBlock(string layerName, string blockName, Point3d point, double angle, double scale)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(blockName, layerName);
                var id = db.Database.InsertModel(point, angle, layerName, blockName, scale, null);
                return id;
            }
        }

        public static ObjectId InsertModel(this Database database, Point3d pt, double rotateAngle, string layerName, string blockName, double scale, Dictionary<string, string> dic)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (dic == null)
                {
                    return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                        layerName,
                                        blockName,
                                        pt,
                                        new Scale3d(scale),
                                        rotateAngle);
                }
                else
                {
                    return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                        layerName,
                                        blockName,
                                        pt,
                                        new Scale3d(scale),
                                        rotateAngle,
                                        dic);
                }
            }
        }

        private static void ImportModel(this Database database, string blockName, string layerName)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), false);
            }
        }
    }
}
