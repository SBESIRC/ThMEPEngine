using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThCantiSlabFilter
    {
        public static List<ThGeometry> Filter(List<ThGeometry> geos)
        {            
            var cantiSlabGeos = geos.GetCantiSlabGeos();
            if(cantiSlabGeos.Count == 0)
            {
                return geos;
            }
            var slabMarks = geos.GetSlabMarks();

            var cantiBoundaries = cantiSlabGeos.Select(o => o.Boundary).ToCollection();
            var slabTexts = slabMarks.Select(o => o.Boundary).ToCollection();

            var slabTextSpatialIndex = new ThCADCoreNTSSpatialIndex(slabTexts);

            var cantiSlabTexts = new DBObjectCollection();
            cantiBoundaries.OfType<Entity>().ForEach(e =>
            {
                var texts = slabTextSpatialIndex.SelectWindowPolygon(e);
                cantiSlabTexts = cantiSlabTexts.Union(texts);
            });

            var cantiSlabMarks = slabMarks.Where(o => cantiSlabTexts.Contains(o.Boundary)).ToList();

            geos = geos.Except(cantiSlabGeos).ToList();
            geos = geos.Except(cantiSlabMarks).ToList();

            cantiBoundaries.MDispose();
            cantiSlabTexts.MDispose();
            return geos;
        }
    }
}
