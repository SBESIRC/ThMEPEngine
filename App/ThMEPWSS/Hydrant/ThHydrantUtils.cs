using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

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
        public static Entity MakeValid(Polygon polygon)
        {
            var objs = polygon.Buffer(0).ToDbCollection(true);
            var areaDic = new Dictionary<Entity, double>();
            objs.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    areaDic.Add(polyline, polyline.Area);
                }
                else if (o is MPolygon mPolygon)
                {
                    areaDic.Add(mPolygon, mPolygon.Area);
                }
                else
                {
                    areaDic.Add(o, 0.0);
                }
            });
            return areaDic.OrderByDescending(o => o.Value).First().Key;
        }
        public static MPolygon MakeValid(MultiPolygon multiPolygon)
        {
            var loops = new List<DBObjectCollection>();
            var polygons = multiPolygon.Geometries.Cast<Polygon>();
            polygons.ForEach(o =>
            {
                var result = new DBObjectCollection();
                var polys = o.ToDbCollection().Cast<Polyline>();
                polys.ForEach(p =>
                {
                    result.Add(p.MakeValid().Cast<Polyline>().OrderByDescending(e => e.Area).First());
                });
                loops.Add(result);
            });
            return ThMPolygonTool.CreateMPolygon(loops);
        }
    }
}
