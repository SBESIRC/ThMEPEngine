using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.CAD
{
    public static class ThMEPDbExtension
    {
        public static DBObjectCollection VisibleEntites(this Database database, ObjectId bkref)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                // 考虑以下情况：
                //  图层的可见性
                //  动态块的可见性
                //  XClip的影响

                // 考虑动态性
                //  动态块：当前可见性下的所有图元
                //  非动态块：块内的所有图元
                var objs = new DBObjectCollection();
                var blkref = acadDatabase.Element<BlockReference>(bkref);
                blkref.ExplodeWithVisible(objs);

                // 考虑图层可见性
                var results = objs.Cast<Entity>().Where(o =>
                {
                    var layer = acadDatabase.Element<LayerTableRecord>(o.LayerId);
                    return !layer.IsFrozen && !layer.IsHidden && !layer.IsOff;
                });

                // 考虑XClip的影响
                var xclip = blkref.XClipInfo();
                if (xclip.IsValid)
                {
                    return results.Where(o => IsContain(xclip, o)).ToCollection();
                }
                else
                {
                    return results.ToCollection();
                }
            }
        }

        private static bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            return xclip.Contains(ent.GeometricExtents.ToRectangle());
        }
    }
}
