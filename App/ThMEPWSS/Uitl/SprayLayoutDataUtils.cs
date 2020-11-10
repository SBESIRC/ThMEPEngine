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
        public static DBObjectCollection Radii(List<Polyline> sprays)
        {
            var objs = new DBObjectCollection();
            foreach (var curve in sprays)
            {
                objs.Add(curve);
            }

            return objs.UnionPolygons();
        }
    }
}
