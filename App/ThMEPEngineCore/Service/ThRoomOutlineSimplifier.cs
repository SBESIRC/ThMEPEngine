using NFox.Cad;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Service
{
    public class ThRoomOutlineSimplifier
    {
        private const double MINIMUM_ROOM_AREA = 100.0;
        private const double MINIMUM_ROOM_CLOSED_TOLERANCE = 1000.0;
        public static DBObjectCollection MakeValid(DBObjectCollection curves)
        {
            return curves.Cast<Polyline>().Select(o => ThMEPFrameService.NormalizeEx(o, MINIMUM_ROOM_CLOSED_TOLERANCE)).ToCollection();
        }

        public static DBObjectCollection Simplify(DBObjectCollection curves)
        {
            return curves.Cast<Polyline>().Where(o => o.Area > MINIMUM_ROOM_AREA).ToCollection();
        }
    }
}
