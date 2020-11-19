using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical
{
    public class ThLaneLineCmds
    {
        [CommandMethod("TIANHUACAD", "THTCD", CommandFlags.Modal)]
        public void ThLaneLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneRecognitionEngine laneLineEngine = new ThLaneRecognitionEngine())
            {
                laneLineEngine.Recognize(Active.Database, new Point3dCollection());
                var lines = laneLineEngine.Spaces.Select(o => o.Boundary).ToList();
                ThLaneLineSimplifier.Simplify(lines.ToCollection(), 1500).ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
    }
}
