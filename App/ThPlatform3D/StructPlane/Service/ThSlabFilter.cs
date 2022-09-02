using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using System;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThSlabFilter
    {
        /// <summary>
        /// 过滤空调板
        /// </summary>
        /// <param name="geos"></param>
        /// <returns></returns>
        public static List<ThGeometry> FilterCantiSlabs(List<ThGeometry> geos)
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

        /// <summary>
        /// 过滤指定厚度的楼板标注
        /// </summary>
        /// <param name="geos"></param>
        /// <param name="slabThick"></param>
        /// <returns></returns>
        public static List<ThGeometry> FilterSpecifiedThickSlabs(List<ThGeometry> geos,double slabThick)
        {
            var slabMarks = geos.GetSlabMarks();
            var filters = slabMarks.Where(o =>
            {
                if(o.Boundary is DBText mark)
                {
                    var content = mark.TextString;
                    var values = content.GetDoubles();
                    return values.Count == 1 && Math.Abs(values[0] - slabThick) <= ThStructurePlaneCommon.DoubleEqualTolerance;
                }
                else
                {
                    return false;
                }
            });
            return geos.Except(filters).ToList();
        }
    }
}
