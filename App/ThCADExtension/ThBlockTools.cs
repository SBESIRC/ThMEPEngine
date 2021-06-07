using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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

        // https://adndevblog.typepad.com/autocad/2012/05/redefining-a-block.html
        public static void RedefineBlockTableRecord(this BlockTableRecord btr, List<Entity> entities)
        {
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
    }
}
