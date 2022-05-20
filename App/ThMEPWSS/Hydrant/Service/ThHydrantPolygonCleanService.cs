using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Hydrant.Service
{
    internal static class ThHydrantPolygonCleanService
    {
        public static DBObjectCollection Clean(
            this DBObjectCollection objs,
            double areaTolerance=1.0,
            double offsetDistance = 20.0,
            double distanceTolerance = 1.0,
            double tessellate_arc_length = 100.0)
        {
            var simplifer = new ThPolygonalElementSimplifier()
            {
                AREATOLERANCE = areaTolerance,
                OFFSETDISTANCE = offsetDistance,
                DISTANCETOLERANCE = distanceTolerance,
                TESSELLATEARCLENGTH = tessellate_arc_length,
            };
            var results = simplifer.Normalize(objs);
            results = simplifer.Simplify(results);
            results = simplifer.MakeValid(results);
            results = ClearZeroPolygon(results, areaTolerance);
            return results;
        }

        public static DBObjectCollection ClearZeroPolygon(this DBObjectCollection polygons,
            double areaTolerance= 1.0)
        {
            return polygons
                .OfType<Entity>()
                .Where(o => o.EntityArea() > areaTolerance)
                .ToCollection();
        }

        public static DBObjectCollection MakeValid(this DBObjectCollection polygons,
            double tessellate_arc_length = 100.0)
        {
            var simplifer = new ThPolygonalElementSimplifier()
            {
                TESSELLATEARCLENGTH = tessellate_arc_length,
            };
            var results = simplifer.MakeValid(polygons);
            return ClearZeroPolygon(results);
        }
    }
}
