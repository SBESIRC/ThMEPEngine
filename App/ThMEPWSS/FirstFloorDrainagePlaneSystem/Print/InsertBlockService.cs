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

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Print
{
    public static class InsertBlockService
    {
        public static double scaleNum = 100;
        public static void InsertBlock(List<KeyValuePair<Point3d, Vector3d>> insertPts, string layerName, string blockName)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(layerName, blockName);
                foreach (var col in insertPts)
                {
                    db.Database.InsertModel(col.Key, col.Value, layerName, blockName);
                }
            }
        }

        public static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, string layerName, string blockName)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir);
            //控制旋转角度
            if (layoutDir.DotProduct(-Vector3d.XAxis) < 0)
            {
                rotateAngle = -rotateAngle;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layerName,
                    blockName,
                    pt,
                    new Scale3d(scaleNum),
                    rotateAngle);
            }
        }

        public static void ImportModel(this Database database, string layerName, string blockName)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), false);
            }
        }
    }
}