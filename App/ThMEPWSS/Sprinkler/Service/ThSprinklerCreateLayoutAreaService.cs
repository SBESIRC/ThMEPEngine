using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerCreateLayoutAreaService
    {
        /// <summary>
        /// 计算可布置区域
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="allBeams"></param>
        /// <returns></returns>
        public static List<Polyline> GetLayoutArea(Polyline polyline, List<Polyline> holes, double spacing = 300)
        {
            DBObjectCollection dBObjects = new DBObjectCollection();
            foreach (var structure in holes)
            {
                dBObjects.Add(structure);
            }

            var layoutAreas = polyline.Difference(dBObjects).Cast<Polyline>().SelectMany(x => x.Buffer(-spacing).Cast<Polyline>()).Where(x => x.Area > 0).ToList();
            return layoutAreas;
        }
    }
}
