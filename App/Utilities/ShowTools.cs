using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class ShowTools
    {
        /// <summary>
        /// 实体集体高亮
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ents"></param>
        public static void Highlight<T>(this IEnumerable<T> ents) where T : Entity
        {
            foreach (var item in ents)
            {
                item.Highlight();
            }
        }

        public static void Highlight(this IEnumerable<ObjectId> ids, OpenCloseTransaction tr)
        {
            foreach (var id in ids)
            {
                (tr.GetObject(id, OpenMode.ForRead) as Entity).Highlight();
            }
        }

        public static void Highlight(this ObjectId id, OpenCloseTransaction tr)
        {
            (tr.GetObject(id, OpenMode.ForRead) as Entity).Highlight();
        }

        public static void UnHighlight(this ObjectId id, OpenCloseTransaction tr)
        {
            (tr.GetObject(id, OpenMode.ForRead) as Entity).Unhighlight();
        }
    }
}
