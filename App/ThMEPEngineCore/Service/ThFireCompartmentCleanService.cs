using ThMEPEngineCore.LaneLine;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using NFox.Cad;

namespace ThMEPEngineCore.Service
{
    public class ThFireCompartmentCleanService
    {
        private double CollinearGap { get; set; }
        private double ExtendDistance { get; set; }

        public ThFireCompartmentCleanService()
        {
            CollinearGap = 5.0;
            ExtendDistance = 100.0;
        }

        public DBObjectCollection Clean(DBObjectCollection curves)
        {
            // 配置参数
            ThLaneLineEngine.extend_distance = ExtendDistance;
            ThLaneLineEngine.collinear_gap_distance = CollinearGap;

            // 合并处理
            var mergedCurves = ThLaneLineEngine.Explode(curves);
            var mergedArcs = mergedCurves.Cast<Curve>().Where(o => o is Arc).ToCollection();
            var mergedLines = mergedCurves.Cast<Curve>().Where(o => o is Line).ToCollection();
            mergedLines = ThLaneLineMergeExtension.Merge(mergedLines);
            //mergedLines = ThLaneLineEngine.Noding(mergedLines);
            mergedLines = ThLaneLineEngine.CleanZeroCurves(mergedLines);

            // 延伸处理
            var extendedLines = ThFireCompartmentExtendEngine.Extend(mergedLines);
            mergedArcs = ThFireCompartmentExtendEngine.Extend(mergedArcs);

            // 合并处理
            return extendedLines.Cast<Curve>().Union(mergedArcs.Cast<Curve>()).ToCollection();
        }
    }
}
