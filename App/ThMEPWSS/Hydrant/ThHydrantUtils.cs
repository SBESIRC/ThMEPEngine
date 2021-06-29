using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant
{
    public class ThHydrantUtils
    {
        public static Dictionary<Entity, Entity> BufferPolygon(List<Entity> polygons, double length)
        {
            var result = new Dictionary<Entity, Entity>();
            var bufferService = new ThNTSBufferService();
            polygons.ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    var bufferEnt = bufferService.Buffer(o, length);
                    if (bufferEnt != null)
                    {
                        result.Add(o, bufferEnt);
                    }
                }
                else if (o is MPolygon mPolygon)
                {
                    var bufferEnt = bufferService.Buffer(o, length);
                    if (bufferEnt != null)
                    {
                        result.Add(o, bufferEnt);
                    }
                }
            });
            return result;
        }
        public static List<Entity> MakeValid(Polygon polygon, double areaTolerance = 1.0)
        {
            var objs = polygon.Buffer(0).ToDbCollection();
            return objs.Cast<Entity>().Where(o =>
             {
                 if (o is Polyline polyline)
                 {
                     return polyline.Area > areaTolerance;
                 }
                 else if (o is MPolygon mPolygon)
                 {
                     return mPolygon.Area > areaTolerance;
                 }
                 else
                 {
                     return false;
                 }
             }).ToList();
        }
    }
}
