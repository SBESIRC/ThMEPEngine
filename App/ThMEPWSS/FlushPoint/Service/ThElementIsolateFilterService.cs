using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThElementIsolateFilterService
    {
        private static bool IsIsolate(Entity o,List<ThIfcRoom> rooms)
        {
            foreach (var room in rooms)
            {
                if (room.Boundary.IsFullContains(o))
                {
                    return true;
                }
            }
            return false;
        }
        public static List<Entity> Filter(List<Entity> entities, List<ThIfcRoom> rooms)
        {
            return entities.Where(o => IsIsolate(o,rooms)).ToList();
        }
    }
}
