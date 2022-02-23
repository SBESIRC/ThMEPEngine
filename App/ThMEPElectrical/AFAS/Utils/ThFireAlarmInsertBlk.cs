using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

using DotNetARX;
using Linq2Acad;

using ThCADExtension;

using ThMEPElectrical.AFAS.Model;

namespace ThMEPElectrical.AFAS.Utils
{
    public static class ThFireAlarmInsertBlk
    {
        public static void InsertBlock(List<KeyValuePair<Point3d, Vector3d>> insertPtInfo, double scale, string blkName, string layserName, bool needMove)
        {
            using (var db = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                db.Blocks.Import(blockDb.Blocks.ElementOrDefault(blkName), true);
                db.Layers.Import(blockDb.Layers.ElementOrDefault(layserName), true);

                foreach (var ptInfo in insertPtInfo)
                {
                    double size = 0;
                    if (needMove == true)
                    {
                        size = ThFaCommon.blk_size[blkName].Item2 / 2;
                    }
                    var pt = ptInfo.Key + ptInfo.Value * scale * size;
                    double rotateAngle = Vector3d.YAxis.GetAngleTo(ptInfo.Value, Vector3d.ZAxis);
                    var attNameValues = new Dictionary<string, string>() { };

                    db.ModelSpace.ObjectId.InsertBlockReference(
                                            layserName,
                                            blkName,
                                            pt,
                                            new Scale3d(scale),
                                            rotateAngle,
                                            attNameValues);
                }
            }
        }

        public static void InsertBlock(Dictionary<Point3d, double> insertPtInfo, double scale, string blkName, string layserName)
        {
            using (var db = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                db.Blocks.Import(blockDb.Blocks.ElementOrDefault(blkName), true);
                db.Layers.Import(blockDb.Layers.ElementOrDefault(layserName), true);

                foreach (var ptInfo in insertPtInfo)
                {
                    var pt = ptInfo.Key;
                    double rotateAngle = ptInfo.Value;
                    var attNameValues = new Dictionary<string, string>() { };

                    db.ModelSpace.ObjectId.InsertBlockReference(
                                            layserName,
                                            blkName,
                                            pt,
                                            new Scale3d(scale),
                                            rotateAngle,
                                            attNameValues);
                }
            }
        }

        public static void PrepareInsert(List<string> blkName, List<string> layerName)
        {
            using (var db = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                blkName.ForEach(x =>
                {
                    db.Blocks.Import(blockDb.Blocks.ElementOrDefault(x), true);
                });
                layerName.ForEach(x =>
                {
                    db.Layers.Import(blockDb.Layers.ElementOrDefault(x), true);
                });
            }
        }

        public static void InsertBlock(List<ThLayoutPt> insertPtInfo, double scale)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var ptInfo in insertPtInfo)
                {
                    var blkName = ptInfo.BlkName;
                    var pt = ptInfo.Pt;
                    double rotateAngle = Vector3d.YAxis.GetAngleTo(ptInfo.Dir, Vector3d.ZAxis);
                    var attNameValues = new Dictionary<string, string>() { };
                    var layerName = ThFaCommon.Blk_Layer[blkName];
                    db.ModelSpace.ObjectId.InsertBlockReference(
                                            layerName,
                                            blkName,
                                            pt,
                                            new Scale3d(scale),
                                            rotateAngle,
                                            attNameValues);
                }
            }
        }

        public static void InsertBlockAngle(List<ThLayoutPt> insertPtInfo, double scale)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var ptInfo in insertPtInfo)
                {
                    var pt = ptInfo.Pt;
                    double rotateAngle = ptInfo.Angle;
                    var blkName = ptInfo.BlkName;
                    var attNameValues = new Dictionary<string, string>() { };
                    var layerName = ThFaCommon.Blk_Layer[blkName];
                    db.ModelSpace.ObjectId.InsertBlockReference(
                                            layerName,
                                            blkName,
                                            pt,
                                            new Scale3d(scale),
                                            rotateAngle,
                                            attNameValues);
                }
            }
        }

        public static void InsertPolyline(List<Polyline> plList, string layerName, int colorIndex)
        {
            if (plList == null || plList.Count == 0)
                return;

            using (var db = AcadDatabase.Active())
            {
                var color = Color.FromColorIndex(ColorMethod.ByColor, (short)colorIndex);

                ThMEPEngineCore.Diagnostics.DrawUtils.CreateLayer(layerName, color, ReplaceLayerSetting: true);

                foreach (var pl in plList)
                {
                    if (pl != null)
                    {
                        var clone = pl.Clone() as Entity;
                        clone.Layer = layerName;
                        db.ModelSpace.Add(clone);
                    }
                }
            }
        }
    }
}
