using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.Lane
{
    public static class ThLaneDbExtension
    {
        public static DBObjectCollection LaneLines(this Database database, Polyline frame)
        {
            using (ThLaneRecognitionEngine laneLineEngine = new ThLaneRecognitionEngine())
            {
                // 提取车道中心线
                laneLineEngine.Recognize(database, frame.Vertices());

                // 车道中心线处理
                var curves = laneLineEngine.Spaces.Select(o => o.Boundary).ToList();
                var lines = ThLaneLineSimplifier.Simplify(curves.ToCollection(), 1500);

                // 框线相交处打断
                return ThCADCoreNTSGeometryClipper.Clip(frame, lines.ToCollection());
            }
        }
    }
}
