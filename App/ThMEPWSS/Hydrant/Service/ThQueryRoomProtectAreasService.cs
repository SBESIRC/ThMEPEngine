using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThQueryRoomProtectAreasService
    {
        /// <summary>
        /// 获取每个房间被保护区域分割的
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="protectAreas"></param>
        /// <returns></returns>
        public Dictionary<ThIfcRoom, Dictionary<Polyline, List<Polyline>>> Query(
            List<ThIfcRoom> rooms, List<Polyline> protectAreas)
        {
            var results = new Dictionary<ThIfcRoom, Dictionary<Polyline, List<Polyline>>>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(protectAreas.ToCollection());
            rooms.ForEach(o =>
            {
                var objs = spatialIndex.SelectCrossingPolygon(o.Boundary);
                var divideService = new ThDivideRoomService(o.Boundary, objs.Cast<Polyline>().ToList());
                divideService.Divide();
                results.Add(o,divideService.Results);
            });
            return results;
        }
    }
}
