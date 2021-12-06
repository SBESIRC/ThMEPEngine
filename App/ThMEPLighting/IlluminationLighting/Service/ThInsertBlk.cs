using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.IlluminationLighting.Model;

namespace ThMEPLighting.IlluminationLighting.Service
{
    class ThInsertBlk
    {
        public static void prepareInsert(List<string> blkName, List<string> layerName)
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
}
