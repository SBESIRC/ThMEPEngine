using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace ThMEPLighting.IlluminationLighting.Service
{
    public class ThHandleContainsService
    {
        public List<Entity> Handle(List<Entity> polygons)
        {
            var results = new List<Entity>();
            polygons = polygons.OrderByDescending(o => GetArea(o)).ToList();
            var objs = new DBObjectCollection();
            polygons.ForEach(p => objs.Add(p));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var garbage = new DBObjectCollection();
            foreach (Entity polygon in polygons)
            {
                if (garbage.Contains(polygon))
                {
                    continue;
                }
                var querys = spatialIndex.SelectWindowPolygon(polygon);
                querys.Remove(polygon);
                results.Add(polygon);
                querys.Cast<Entity>().ForEach(o => garbage.Add(o));
            }
            return results;
        }
        private double GetArea(Entity polygon)
        {
            return polygon.ToNTSPolygon().Area;
        }
    }

}
