using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;

using ThCADExtension;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Diagram
{
    public class ThPDSBlockInsertEngine
    {
        public ObjectId Insert(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale)
        {
            activeDb.Blocks.Import(configDb.Blocks.ElementOrDefault(blockName), false);
            return activeDb.ModelSpace.ObjectId.InsertBlockReference(
                "0",
                blockName,
                basePoint,
                scale,
                0.0);
        }

        public ObjectId Insert(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale, Dictionary<string, string> dictionary)
        {
            activeDb.Blocks.Import(configDb.Blocks.ElementOrDefault(blockName), false);
            return activeDb.ModelSpace.ObjectId.InsertBlockReference(
                "0",
                blockName,
                basePoint,
                scale,
                0.0,
                dictionary);
        }

        public BlockReference Insert1(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale)
        {
            var tableTitleId = Insert(activeDb, configDb, blockName, basePoint, scale);
            return activeDb.Element<BlockReference>(tableTitleId, true);
        }

        public void Insert2(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale, int frameNum)
        {
            var key = "内框名称";
            var value = "配电箱系统图（" + ((frameNum / 2) + 1).NumberToChinese() + "）";
            var dictionary = new Dictionary<string, string>
            {
                { key, value },
            };
            Insert(activeDb, configDb, blockName, basePoint, scale, dictionary);
        }

        public BlockReference Insert3(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale, Dictionary<string, string> dictionary)
        {
            var block = Insert(activeDb, configDb, blockName, basePoint, scale, dictionary);
            return activeDb.Element<BlockReference>(block, true);
        }

        public BlockReference InsertHeader(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale)
        {
            var header = Insert1(activeDb, configDb, blockName, basePoint, scale);
            var objs = ThPDSExplodeService.BlockExplode(activeDb, header);
            var title = objs.OfType<BlockReference>()
                .Where(o => o.Name == ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TITLE)
                .FirstOrDefault();
            return title;
        }

        public void InsertBlankLine(AcadDatabase activeDb, AcadDatabase configDb, Point3d basePoint, Scale3d scale, List<Entity> tableObjs)
        {
            activeDb.Blocks.Import(configDb.Blocks.ElementOrDefault(CircuitFormOutType.常规.GetDescription()), false);
            var circuitId = activeDb.ModelSpace.ObjectId.InsertBlockReference(
                "0",
                CircuitFormOutType.常规.GetDescription(),
                basePoint,
                scale,
                0.0);
            var circuit = activeDb.Element<BlockReference>(circuitId, true);
            var objs = ThPDSExplodeService.BlockExplode(activeDb, circuit);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            objs.OfType<Entity>()
                .Where(e => e.Layer != ThPDSLayerService.TableFrameLayer())
                .ForEach(e => e.Erase());
        }

        public BlockReference InsertCircuitDimension(AcadDatabase activeDb, AcadDatabase configDb, string blockName, Point3d basePoint, Scale3d scale)
        {
            var sourceDimension = Insert1(activeDb, configDb, blockName, basePoint, scale);
            var objID = ObjectId.Null;
            void handler(object s, ObjectEventArgs e)
            {
                if (e.DBObject is BlockReference block)
                {
                    objID = e.DBObject.ObjectId;
                }
            }
            activeDb.Database.ObjectAppended += handler;
            sourceDimension.ExplodeToOwnerSpace();
            activeDb.Database.ObjectAppended -= handler;
            sourceDimension.Erase();
            return activeDb.Element<BlockReference>(objID, true); ;
        }
    }
}
