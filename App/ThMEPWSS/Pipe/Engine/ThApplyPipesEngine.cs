using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThApplyPipesEngine
    {
        public static void Apply(string sourceFloor,List<Tuple<string,bool>> targetFloors)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var sourceBlock = GetBlockReferences1(acadDatabase.Database, sourceFloor);
                if (sourceBlock.Count == 0)
                {
                    return;
                }
                foreach (var targetFloor in targetFloors)
                {
                    var targetBlock= GetBlockReferences(acadDatabase.Database, targetFloor.Item1);
                    if (targetBlock.Count == 0)
                    {
                        continue;
                    }
                    if (targetFloor.Item2)
                    {
                        CopyTags(targetBlock, sourceBlock, acadDatabase);
                    }
                    else
                    {
                        CopyEntities(targetBlock, sourceBlock, acadDatabase);
                    }
                }
            }
        }

        private static List<BlockReference> GetBlockReferences1(Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            var trans = db.TransactionManager;
            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            blocks = (from b in db.GetEntsInDatabase<BlockReference>()
                      where (b.GetBlockName().Contains(blockName.Substring(0, blockName.Length - 3)) && b.GetBlockName().Contains("标准层"))
                      select b).ToList();
            return blocks;
        }

        private static List<BlockReference> GetBlockReferences(Database db, string blockName)
        {
            List<BlockReference> blocks = new List<BlockReference>();
            var trans = db.TransactionManager;
            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
            blocks = (from b in db.GetEntsInDatabase<BlockReference>()
                      where (b.GetBlockName().Contains(blockName))
                      select b).ToList();
            return blocks;
        }
        private static BlockReference CreateBlock(Database db,List<Entity> entities,DBObjectCollection target_objs,List<BlockReference> targetBlock,string name)
        {           
            targetBlock[0].UpgradeOpen();
            targetBlock[0].Erase();
            targetBlock[0].DowngradeOpen();
            BlockTools.AddBlockTableRecord(db, name, entities).Erase();
            var record = BlockTools.AddBlockTableRecord(db, name, entities);
            BlockReference nsourceBlock = new BlockReference(new Point3d(0, 0, 0), record);
            return nsourceBlock;
        }
        private static void CopyEntities(List<BlockReference> targetBlock, List<BlockReference> sourceBlock, AcadDatabase acadDatabase)
        {
            string name = targetBlock[0].GetBlockName();
            var objs = new DBObjectCollection();
            var target_objs = new DBObjectCollection();
            var entities = new List<Entity>();
            sourceBlock[0].Explode(objs);
            targetBlock[0].Explode(target_objs);                   
            var offset = Matrix3d.Displacement(GetPoint(sourceBlock[0]).GetVectorTo(GetPoint(targetBlock[0])));
            foreach (object et in objs)
            {
                Entity ent = et as Entity;
                entities.Add(ent.GetTransformedCopy(offset));
            }
            acadDatabase.ModelSpace.Add(CreateBlock(acadDatabase.Database, entities, target_objs, targetBlock, name));
        }
        private static Point3d GetPoint(BlockReference blockSp)
        {
            if (blockSp.GetEffectiveName().Length == 3)
            {
                foreach (var block in ThTagParametersService.blockCollection)
                {
                    if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Equals(blockSp.GetEffectiveName()))
                    {
                        return block.GeometricExtents.MinPoint;
                    }
                }
            }
            else
            {
                var name = blockSp.GetEffectiveName().Substring(3, blockSp.GetEffectiveName().Length - 3);
                foreach (var block in ThTagParametersService.blockCollection)
                {            
                    if (BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号").Equals(name))
                    {
                        return block.GeometricExtents.MinPoint;
                    }
                }
            }
            return new Point3d();
        }
        private static void CopyTags(List<BlockReference> targetBlock, List<BlockReference> sourceBlock, AcadDatabase acadDatabase)
        {
            string name = targetBlock[0].GetBlockName();
            var objs = new DBObjectCollection();
            var target_objs = new DBObjectCollection();
            var entities = new List<Entity>();
            sourceBlock[0].Explode(objs);
            targetBlock[0].Explode(target_objs);
          
            var offset = Matrix3d.Displacement(GetPoint(sourceBlock[0]).GetVectorTo(GetPoint(targetBlock[0])));
            for (int i = 0; i < objs.Count; i++)
            {
                int n = 0;
                if ((objs[i].GetType().Name == "DBText"))
                {
                    DBText originTx = objs[i] as DBText;
                    for (int j = 0; j < target_objs.Count; j++)
                    {
                        if (target_objs[j].GetType().Name == "DBText")
                        {
                            DBText proposeTx = target_objs[j] as DBText;
                            if (proposeTx.Position.DistanceTo((originTx.Position) + GetPoint(sourceBlock[0]).GetVectorTo(GetPoint(targetBlock[0]))) < 1)
                            {
                                Entity ent1 = objs[i] as Entity;
                                target_objs[j] = ent1.GetTransformedCopy(offset);
                                n++;
                            }
                        }
                    }
                    if (n == 0)
                    {
                        Entity ent = objs[i] as Entity;
                        entities.Add(ent.GetTransformedCopy(offset));
                    }
                }
            }
            foreach (object et in target_objs)
            {
                Entity ent = et as Entity;
                entities.Add(ent);
            }
            acadDatabase.ModelSpace.Add(CreateBlock(acadDatabase.Database, entities, target_objs, targetBlock, name));
        }
    }
}
