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

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Print
{
    public static class InsertBlockService
    {
        public static double scaleNum = 1;
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

        public static void InsertBlock(List<KeyValuePair<Point3d, Vector3d>> insertPts, string layerName, string blockName, Dictionary<string, object> attNameValues, bool isDynAttri = false)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(layerName, blockName);
                foreach (var col in insertPts)
                {
                    db.Database.InsertModel(col.Key, col.Value, layerName, blockName, attNameValues, isDynAttri);
                }
            }
        }

        public static void InsertConnectPipe(List<Polyline> polylines, string layerName, string lineType, bool needImport = true)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                if (needImport)
                {
                    acadDatabase.Layers.Import(
                       blockDb.Layers.ElementOrDefault(layerName), false);
                    if (lineType != null)
                    {
                        acadDatabase.Linetypes.Import(
                        blockDb.Linetypes.ElementOrDefault(lineType), false);
                    }
                }
                foreach (var poly in polylines)
                {
                    if (lineType != null)
                    {
                        poly.Linetype = lineType;
                    }
                    poly.Layer = layerName;
                    poly.ColorIndex = 256;
                    acadDatabase.ModelSpace.Add(poly);
                }
            }
        }

        public static void InsertText(List<DBText> txts, string layerName, string lineType, bool needImport = true)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                if (needImport)
                {
                    acadDatabase.Layers.Import(
                       blockDb.Layers.ElementOrDefault(layerName), false);
                    if (lineType != null)
                    {
                        acadDatabase.Linetypes.Import(
                        blockDb.Linetypes.ElementOrDefault(lineType), false);
                    }
                }
                foreach (var txt in txts)
                {
                    if (lineType != null)
                    {
                        txt.Linetype = lineType;
                    }
                    txt.Layer = layerName;
                    txt.ColorIndex = 256;
                    txt.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    txt.Scale(txt.Position, scaleNum);
                    acadDatabase.ModelSpace.Add(txt);
                }
            }
        }

        public static void InsertPipeCircle(Circle circle, string layerName, bool needImport = true)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                if (needImport)
                {
                    acadDatabase.Layers.Import(
                       blockDb.Layers.ElementOrDefault(layerName), false);
                }
                circle.Layer = layerName;
                circle.ColorIndex = 256;
                acadDatabase.ModelSpace.Add(circle);
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

        public static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, string layerName, string blockName, Dictionary<string, object> attNameValues, bool isDynAttri)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir);
            //控制旋转角度
            if (layoutDir.DotProduct(-Vector3d.XAxis) < 0)
            {
                rotateAngle = -rotateAngle;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var attri = attNameValues.ToDictionary(x => x.Key, y => y.Value.ToString());
                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layerName,
                    blockName,
                    pt,
                    new Scale3d(scaleNum),
                    rotateAngle,
                    attri);
                if (isDynAttri)
                {
                    foreach (var dynamic in attNameValues)
                    {
                        objId.SetDynBlockValue(dynamic.Key, dynamic.Value);
                    }
                }
                return objId;
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
