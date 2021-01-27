using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineMergeExtension : ThLaneLineEngine
    {
        public static DBObjectCollection Merge(DBObjectCollection curves)
        {
            return ExplodeCurves(Simplify(curves.LineMerge())).ToCollection();
        }
    }
}
