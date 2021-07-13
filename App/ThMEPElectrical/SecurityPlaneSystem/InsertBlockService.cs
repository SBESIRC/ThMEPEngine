﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPElectrical.SecurityPlaneSystem
{
    public static class InsertBlockService
    {
        public static List<BlockReference> InsertBlock(string layerName, string blockName, Point3d point, double angle, double scale)
        {
            List<BlockReference> broadcasts = new List<BlockReference>();
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(blockName, layerName);
                var id = db.Database.InsertModel(point, angle, layerName, blockName, scale);
            }

            return broadcasts;
        }

        public static ObjectId InsertModel(this Database database, Point3d pt, double rotateAngle, string layerName, string blockName, double scale)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layerName,
                    blockName,
                    pt,
                    new Scale3d(scale),
                    rotateAngle);
            }
        }

        public static void ImportModel(this Database database, string blockName, string layerName)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalSecurityPlaneDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), false);
            }
        }
    }
}
 