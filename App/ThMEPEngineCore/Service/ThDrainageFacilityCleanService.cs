using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

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
            var objs1 = curves.ExplodeToLines(TesslateLength);
            var objs2 = objs1.LineMerge();
            var objs3 = objs2.ExplodeToLines();
            var results = objs3.Cast<Line>().Where(o => o.Length >= 1e-4).ToCollection();

            // 释放
            var garbages = new DBObjectCollection();
            garbages = garbages.Union(objs1);
            garbages = garbages.Union(objs2);
            garbages = garbages.Union(objs3);
            garbages.Difference(results);
            garbages.MDispose();

            return results;
        }
    }
}
