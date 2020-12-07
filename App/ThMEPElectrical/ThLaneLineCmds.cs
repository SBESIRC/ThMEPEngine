using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
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
                var result = Active.Editor.GetEntity("\n请选择需要提取车道中心线的范围框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                laneLineEngine.Recognize(Active.Database, frame.Vertices());
                var lines = laneLineEngine.Spaces.Select(o => o.Boundary).ToList();
                ThLaneLineSimplifier.Simplify(lines.ToCollection(), 1500).ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
    }
}
