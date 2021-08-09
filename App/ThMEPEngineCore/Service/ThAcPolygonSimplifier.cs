using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ACPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public class ThAcPolygonSimplifier :ThElementSimplifier
    {
        public ACPolygon Clean(ACPolygon polygon)
        {
            var objs = new DBObjectCollection();
            objs.Add(polygon);
            objs = Tessellate(objs);
            objs = MakeValid(objs);
            objs = Normalize(objs);
            if (objs.Count > 0)
            {
                return objs.Cast<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
            }
            else
            {
                return new Polyline();
            }
        }
    }
}
