using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThDrainageFacilityCleanService
    {
        public double TesslateLength { get; set; }
        public ThDrainageFacilityCleanService()
        {
            TesslateLength = 100.0;
        }
        public DBObjectCollection Clean(DBObjectCollection curves)
        {
            curves = curves.Cast<Curve>().Where(o => o is Line || o is Polyline).ToCollection();
            var objs = curves.ExplodeLines(TesslateLength).ToCollection();
            objs = objs.LineMerge();
            objs = objs.ExplodeLines().ToCollection();
            return objs.Cast<Line>().Where(o => o.Length >= 1e-4).ToCollection();
        }
    }
}
