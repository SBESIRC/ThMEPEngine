using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThApplicationPipesEngine
    {
        public static void Application(string sourceFloor,List<Tuple<string,bool>> targetFloors)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var targetFloor in targetFloors)
                {//check value=false
                    var sourceBlock= BlockTools.GetAllDynBlockReferences(acadDatabase.Database, sourceFloor);
                    var targetBlock= BlockTools.GetAllDynBlockReferences(acadDatabase.Database, targetFloor.Item1);
                    if (!targetFloor.Item2)
                    {
                        CopyEntities(targetBlock, sourceBlock, acadDatabase);
                    }
                    else
                    {//仅更新标注
                        CopyTags(targetBlock, sourceBlock, acadDatabase);
                    }
                }
            }
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
            Circle origin = objs[0] as Circle;
            Circle proposed = target_objs[0] as Circle;
            var offset = Matrix3d.Displacement(origin.Center.GetVectorTo(proposed.Center));
            foreach (object et in objs)
            {
                Entity ent = et as Entity;
                entities.Add(ent.GetTransformedCopy(offset));
            }
            acadDatabase.ModelSpace.Add(CreateBlock(acadDatabase.Database, entities, target_objs, targetBlock, name));
        }
        private static void CopyTags(List<BlockReference> targetBlock, List<BlockReference> sourceBlock, AcadDatabase acadDatabase)
        {
            string name = targetBlock[0].GetBlockName();
            var objs = new DBObjectCollection();
            var target_objs = new DBObjectCollection();
            var entities = new List<Entity>();
            sourceBlock[0].Explode(objs);
            targetBlock[0].Explode(target_objs);
            Circle origin = objs[0] as Circle;
            Circle proposed = target_objs[0] as Circle;
            var offset = Matrix3d.Displacement(origin.Center.GetVectorTo(proposed.Center));
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
                            if (proposeTx.Position.DistanceTo((originTx.Position) + origin.Center.GetVectorTo(proposed.Center)) < 1)
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
