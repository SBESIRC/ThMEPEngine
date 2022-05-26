using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.Service
{
    public static class CreateLayoutAreaService
    {
        /// <summary>
        /// 计算可布置区域
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="allBeams"></param>
        /// <returns></returns>
        public static List<MPolygon> GetLayoutArea(Polyline polyline, List<Entity> allBeams, List<Polyline> columnPoly, List<Polyline> wallPolys, double spacing = 300)
        {
            DBObjectCollection dBObjects = new DBObjectCollection();
            foreach (var beam in allBeams)
            {
                dBObjects.Add(beam);
            }

            foreach (var cPoly in columnPoly)
            {
                dBObjects.Add(cPoly);
            }

            foreach (var wPoly in wallPolys)
            {
                dBObjects.Add(wPoly);
            }

            var layoutAreas = polyline.DifferenceMP(dBObjects).Cast<MPolygon>().SelectMany(x => x.Buffer(-spacing, true).Cast<MPolygon>()).Where(x => x.Area > 0).ToList();
            return layoutAreas;
        }
    }
}
