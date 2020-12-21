using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Lane
{
    public class ThLaneLineMerger
    {
        public static DBObjectCollection LineMerge(DBObjectCollection lines)
        {
            return lines.LineMerge();
        }
    }
}
