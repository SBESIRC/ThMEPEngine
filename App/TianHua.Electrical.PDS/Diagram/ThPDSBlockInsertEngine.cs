using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Diagram
{
    public class ThPDSBlockInsertEngine
    {
        public BlockReference Insert(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale)
        {
            activeDb.Blocks.Import(configDb.Blocks.ElementOrDefault(blockName), false);
            var tableTitleId = activeDb.ModelSpace.ObjectId.InsertBlockReference(
                "0",
                blockName,
                basePoint,
                scale,
                0.0);
            return activeDb.Element<BlockReference>(tableTitleId, true);
        }

        public BlockReference InsertHeader(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale)
        {
            var header = Insert(activeDb, configDb, blockName, basePoint, scale);
            var objs = ThPDSExplodeService.BlockExplode(activeDb, header);
            var title = objs.OfType<BlockReference>()
                .Where(o => o.Name == ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TITLE)
                .FirstOrDefault();
            return title;
        }
    }
}
