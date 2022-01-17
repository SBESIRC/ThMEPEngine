using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Service
{
    public class ThElectricalCommonService
    {
        public static Polyline GetFrameBlkPolyline(BlockReference blockReference)
        {
            var objs = new DBObjectCollection();
            blockReference.Explode(objs);
            return objs.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
        }
    }
}
