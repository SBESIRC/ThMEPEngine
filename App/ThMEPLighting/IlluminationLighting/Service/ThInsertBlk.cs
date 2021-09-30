using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using DotNetARX;
using Linq2Acad;

using ThCADExtension;
using ThMEPLighting.IlluminationLighting.Model;

namespace ThMEPLighting.IlluminationLighting.Service
{
    class ThInsertBlk
    {
        public static void prepareInsert(List<string> blkName, List<string> layerName)
        {
            using (var db = AcadDatabase.Active())
            {
                blkName.ForEach(x => db.Database.ImportBlock(x));
                layerName.ForEach(x => db.Database.ImportLayer(x));
            }
        }
        public static void InsertBlock(List<ThLayoutPt> insertPtInfo, double scale)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var ptInfo in insertPtInfo)
                {
                    var blkName = ptInfo.BlkName;
                   // var size = ThIlluminationCommon.blk_size[blkName].Item2 / 2;
                   // var pt = ptInfo.Pt + ptInfo.Dir * scale * size;
                    var pt = ptInfo.Pt;
                    double rotateAngle = Vector3d.YAxis.GetAngleTo(ptInfo.Dir, Vector3d.ZAxis);
                    var attNameValues = new Dictionary<string, string>() { };
                    var layerName = ThIlluminationCommon.blk_layer[blkName];
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
                    var layerName = ThIlluminationCommon.blk_layer[blkName];
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


    }
    public static class InsertService
    {
        public static void ImportBlock(this Database database, string name)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
            }
        }

        public static void ImportLinetype(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLayer(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(name), replaceIfDuplicate);

                LayerTableRecord layer = currentDb.Layers.Element(name, true);
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
    }


}
