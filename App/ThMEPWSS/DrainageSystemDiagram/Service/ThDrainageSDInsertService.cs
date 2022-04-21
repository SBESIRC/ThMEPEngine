using System.Collections.Generic;
using System.Linq;

using Linq2Acad;
using DotNetARX;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;

using ThMEPWSS.DrainageSystemDiagram.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDInsertService
    {
        public static void InsertConnectPipe(List<Line> lines)
        {
            if (lines==null || lines.Count() ==0)
            {
                return;
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var templateLayer = getTemplateLayerName(ThDrainageSDCommon.Layer_CoolPipe, ThDrainageSDCommon.Layer_Suffix);
                acadDatabase.Database.ImportLayer(ThDrainageSDCommon.Layer_CoolPipe, templateLayer);

                for (int i = 0; i < lines.Count(); i++)
                {
                    var linkLine = lines[i];
                    linkLine.Layer = ThDrainageSDCommon.Layer_CoolPipe;
                    linkLine.Color = Color.FromColorIndex(ColorMethod.ByLayer, (short)ColorIndex.BYLAYER);
                    acadDatabase.ModelSpace.Add(linkLine);
                }
            }
        }

        public static void InsertStackPoint(List<Point3d> pts)
        {
            if (pts == null || pts.Count() == 0)
            {
                return;
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThDrainageSDCommon.Layer_Stack);

                for (int i = 0; i < pts.Count(); i++)
                {
                    var pt = new Circle(pts[i], new Vector3d(0, 0, 1), ThDrainageSDCommon.Dia_Stack);
                    pt.Layer = ThDrainageSDCommon.Layer_Stack;
                    pt.Color = Color.FromColorIndex(ColorMethod.ByLayer, (short)ColorIndex.BYLAYER);
                    acadDatabase.ModelSpace.Add(pt);
                }
            }
        }

        public static void InsertValve(List<ThDrainageSDADBlkOutput> convertedValve)
        {
            if (convertedValve == null || convertedValve.Count() == 0)
            {
                return;
            }

            var layer = ThDrainageSDCommon.Layer_Valves;

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(layer);

                var allBlkName = convertedValve.Select(x => x.Name).Distinct().ToList();
                allBlkName.ForEach(x => acadDatabase.Database.ImportBlock(x));

                for (int i = 0; i < convertedValve.Count(); i++)
                {
                    var valve = convertedValve[i];
                    var pt = valve.Position;
                    double rotateAngle = Vector3d.XAxis.GetAngleTo(valve.Dir, Vector3d.ZAxis);
                    double scale = valve.Scale;
                
                    var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                           layer,
                           valve.Name,
                           pt,
                           new Scale3d(scale),
                           rotateAngle,
                          new Dictionary<string, string>()
                       );
                    foreach (var dynamic in valve.Visibility)
                    {
                        id.SetDynBlockValue(dynamic.Key, dynamic.Value);
                    }
                }
            }
        }

        public static void InsertDim(List<RotatedDimension> dims)
        {
            if (dims == null || dims.Count() == 0)
            {
                return;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThDrainageSDCommon.Layer_Dim);
                acadDatabase.Database.ImportDimtype(ThDrainageSDCommon.Style_Dim);

                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(ThDrainageSDCommon.Style_Dim, acadDatabase.Database);

                for (int i = 0; i < dims.Count(); i++)
                {
                    var dim = dims[i];

                    dim.Layer = ThDrainageSDCommon.Layer_Dim;
                    dim.Color = Color.FromColorIndex(ColorMethod.ByLayer, (short)ColorIndex.BYLAYER);
                    dim.DimensionStyle = id;
                    acadDatabase.ModelSpace.Add(dim);

                }
            }
        }

        private static string getTemplateLayerName(string layerName, string suffix)
        {
            var layer = layerName.Substring(0, layerName.IndexOf(suffix));
            return layer;
        }
    }

    public static class InsertService
    {
        public static void ImportBlock(this Database database, string name)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                var item = blockDb.Blocks.ElementOrDefault(name);
                if (item != null)
                {
                    currentDb.Blocks.Import(item, false);
                }
            }
        }

        public static void ImportDimtype(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                var item = blockDb.DimStyles.ElementOrDefault(name);
                if (item != null)
                {
                    currentDb.DimStyles.Import(item, replaceIfDuplicate);
                }
            }
        }

        public static void ImportLayer(this Database database, string layerName, string TemplateName = "", bool replaceIfDuplicate = true)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {

                if (TemplateName != "")
                {
                    var tempLayer = blockDb.Layers.ElementOrDefault(TemplateName);
                    Color color = Color.FromColorIndex(ColorMethod.ByLayer, (short)3);
                    if (tempLayer != null)
                    {
                        color = tempLayer.Color;
                    }
                    var newLayer = CreateLayer(layerName, color, replaceIfDuplicate);
                }
                else
                {
                    currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), replaceIfDuplicate);
                }

                LayerTableRecord layer = currentDb.Layers.Element(layerName, true);
                if (layer != null)
                {
                    layer.UpgradeOpen();
                    layer.IsOff = false;
                    layer.IsFrozen = false;
                    layer.IsLocked = false;
                    layer.DowngradeOpen();
                }
            }
        }

        private static LayerTableRecord CreateLayer(string aimLayer, Color color, bool replaceIfDuplicate)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                layerRecord = db.Layers.ElementOrDefault(aimLayer);
                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color = color;
                }
                layerRecord.UpgradeOpen();
                if (replaceIfDuplicate == true)
                {
                    layerRecord.Color = color;
                }
                layerRecord.IsPlottable = false;
                layerRecord.DowngradeOpen();
            }

            return layerRecord;
        }
    }
}
