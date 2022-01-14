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
        public const double AREATOLERANCE = 1.0;
        public const double BUFFERLENGTH = 50.0;
        public const double TESSELLATE_ARC_LENGTH = 100.0;

        public static DBObjectCollection Clean(this DBObjectCollection objs)
        {
            var simplifer = new ThPolygonalElementSimplifier()
            {
                OFFSETDISTANCE = BUFFERLENGTH,
                TESSELLATEARCLENGTH = TESSELLATE_ARC_LENGTH,
            };
            var results = simplifer.Normalize(objs);
            results = simplifer.Simplify(results);
            results = simplifer.MakeValid(results);
            results = ClearZeroPolygon(results);
            return results;
        }

        public static DBObjectCollection ClearZeroPolygon(this DBObjectCollection polygons)
        {
            return polygons
                .OfType<Entity>()
                .Where(o => o.EntityArea() > AREATOLERANCE)
                .ToCollection();
        }

        public static DBObjectCollection MakeValid(this DBObjectCollection polygons)
        {
            var simplifer = new ThPolygonalElementSimplifier()
            {
                OFFSETDISTANCE = BUFFERLENGTH,
                TESSELLATEARCLENGTH = TESSELLATE_ARC_LENGTH,
            };
            var results = simplifer.MakeValid(polygons);
            return ClearZeroPolygon(results);
        }
    }
}
