using ThMEPEngineCore.Service;
using System.Collections.Generic;
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
    }
}
