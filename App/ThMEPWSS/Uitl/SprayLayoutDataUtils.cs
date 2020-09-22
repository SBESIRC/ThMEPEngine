using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;

namespace ThMEPWSS.Utils
{
    public static class SprayLayoutDataUtils
    {
        /// <summary>
        /// 生成喷淋保护区域
        /// </summary>
        /// <param name="sprays"></param>
        /// <returns></returns>
        public static DBObjectCollection Radii(List<SprayLayoutData> sprays)
        {
            var objs = new DBObjectCollection();
            foreach (var curve in sprays.Select(o => o.Radii))
            {
                objs.Add(curve);
            }

            return objs.UnionPolygons();
        }
    }
}
