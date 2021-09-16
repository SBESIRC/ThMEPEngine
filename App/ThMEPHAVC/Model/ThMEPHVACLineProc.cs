using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;

namespace ThMEPHVAC.Model
{
    public class ThMEPHVACLineProc
    {
        public static DBObjectCollection Explode(DBObjectCollection lines)
        {
            return ThLaneLineEngine.Explode(lines);
        }
        public static DBObjectCollection Pre_proc(DBObjectCollection lines)
        {
            //var service = new ThLaneLineCleanService();
            //var res = ThLaneLineEngine.Explode(service.Clean(lines));
            //var extendLines = res.OfType<Line>().Select(o => o.ExtendLine(1.0)).ToCollection();
            //lines = ThLaneLineEngine.Noding(extendLines);
            //lines = ThLaneLineEngine.CleanZeroCurves(lines);
            //lines = lines.LineMerge();
            //lines = ThLaneLineEngine.Explode(lines);
            var service = new ThLaneLineCleanService();
            lines = service.CleanNoding(lines);
            return lines;
        }
    }
}
