using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public static class ThInsertStoreyFrameService
    {
        public static void ImportBlock()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportBlock(ThWPipeCommon.STOREY_BLOCK_NAME, ThWPipeCommon.STOREY_LAYER_NAME);
            }
        }

        public static void ImportHouseTypeSplitLineLayer()
        {
            using (var currentDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.StoreyFrameDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThWPipeCommon.HouseTypeSplitLineLayer), true);
            }
        }

        public static void ImportCellSplitLineLayer()
        {
            using (var currentDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.StoreyFrameDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThWPipeCommon.CellSplitLineLayer), true);
            }
        }

        public static void Insert(Point3d position)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var obj = acadDatabase.Database.InsertBlock(
                    ThWPipeCommon.STOREY_LAYER_NAME,
                    ThWPipeCommon.STOREY_BLOCK_NAME,
                    position,
                    new Scale3d(1),
                    0);
                var frame = acadDatabase.Element<BlockReference>(obj, true);
                frame.TransformBy(Active.Editor.UCS2WCS());
            }
        }

        private static ObjectId InsertBlock(this Database database, string layer, string name, Point3d position, Scale3d scale, double angle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var attNameValues = new Dictionary<string, string>();
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layer,
                    name,
                    position,
                    scale,
                    angle,
                    attNameValues);
            }
        }

        private static void ImportBlock(this Database database, string name, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StoreyFrameDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), true);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true);               
            }
        }
    }
}
