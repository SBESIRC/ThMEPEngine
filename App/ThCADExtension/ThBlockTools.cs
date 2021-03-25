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
    }
}
