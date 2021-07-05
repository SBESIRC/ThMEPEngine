using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

namespace ThCADExtension
{
    public static class ThBlockTools
    {
        public static Extents3d GeometricExtents(this BlockTableRecord btr)
        {
            var extents = new Extents3d();
            extents.AddBlockExtents(btr);
            return extents;
        }

        public static void RedefineBlockTableRecord(this BlockTableRecord btr, List<Entity> entities)
        {
            // https://adndevblog.typepad.com/autocad/2012/05/redefining-a-block.html
            using (var acadDatabase = AcadDatabase.Use(btr.Database))
            {
                foreach (var objId in btr)
                {
                    acadDatabase.Element<Entity>(objId, true).Erase();
                }
                entities.ForEach(o => btr.AppendEntity(o));
                entities.ForEach(o => acadDatabase.AddNewlyCreatedDBObject(o));
            }
        }

        public static void FixWipeOutDrawOrder(this BlockTableRecord btr)
        {
            // https://www.keanw.com/2013/05/fixing-autocad-drawings-exported-by-smartsketch-using-net.html
            using (var acadDatabase = AcadDatabase.Use(btr.Database))
            {
                var wipeouts = new ObjectIdCollection();
                foreach (ObjectId objId in btr)
                {
                    var entity = acadDatabase.Element<Entity>(objId);
                    if (entity is Wipeout)
                    {
                        wipeouts.Add(entity.ObjectId);
                    }
                }
                if (wipeouts.Count > 0)
                {
                    var drawOrder = acadDatabase.Element<DrawOrderTable>(btr.DrawOrderTableId, true);
                    drawOrder.MoveToBottom(wipeouts);
                }
            }
        }

        /// <summary>
        /// 获取块引用的属性值（支持重复属性名）
        /// </summary>
        /// <param name="blockReferenceId"></param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> GetAttributesInBlockReferenceEx(this ObjectId blockReferenceId)
        {
            List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
            Database db = blockReferenceId.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 获取块参照
                BlockReference bref = (BlockReference)trans.GetObject(blockReferenceId, OpenMode.ForRead);
                // 遍历块参照的属性，并将其属性名和属性值添加到字典中
                foreach (ObjectId attId in bref.AttributeCollection)
                {
                    AttributeReference attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForRead);
                    attributes.Add(new KeyValuePair<string, string>(attRef.Tag, attRef.TextString));
                }
                trans.Commit();
            }
            return attributes; // 返回块参照的属性名和属性值
        }

        public static ObjectId AddBlockTableRecordDBEntity(Database database, string addBlockName, Point3d blockBasePoint, ObjectId[] entityIds, bool delEntity = false)
        {
            ObjectId blockId = ObjectId.Null;
            if (string.IsNullOrEmpty(addBlockName) || null == entityIds || entityIds.Length < 1)
                return blockId;
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)transaction.GetObject(database.BlockTableId,OpenMode.ForRead);
                if (!bt.Has(addBlockName))
                {
                    //项目中没有相应的块，进行创建，如果已经有相应的块不进行创建
                    bt.UpgradeOpen();
                    //create new
                    BlockTableRecord record = new BlockTableRecord();
                    record.Name = addBlockName;
                    record.Origin = blockBasePoint;
                    bt.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                }
                blockId = bt[addBlockName];
                transaction.Commit();
            }
            //copy the select entities to block by using deepclone.
            ObjectIdCollection collection = new ObjectIdCollection(entityIds);
            IdMapping mapping = new IdMapping();
            database.DeepCloneObjects(collection, blockId, mapping, false);
            if (!delEntity)
                return blockId;
            using (AcadDatabase acdb = AcadDatabase.Use(database))
            {
                foreach (var blId in entityIds)
                {
                    blId.Erase();
                }
            }
            return blockId;
        }
    }
}
