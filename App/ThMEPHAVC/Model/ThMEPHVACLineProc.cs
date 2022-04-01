using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
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
        public static DBObjectCollection PreProc(DBObjectCollection lines)
        {
            if (lines.Count == 0)
                return new DBObjectCollection();
            lines = CleanNoding(lines);
            return lines;
        }
        public static DBObjectCollection CleanNoding(DBObjectCollection curves)
        {
            // 配置参数
            ThLaneLineEngine.extend_distance = 2.0;
            ThLaneLineEngine.collinear_gap_distance = 20.0;

            // 合并处理
            var mergedLines = ThLaneLineEngine.Explode(curves);
            mergedLines = ThLaneLineMergeExtension.Merge(mergedLines);
            mergedLines = ThLaneLineEngine.Noding(mergedLines);
            mergedLines = ThLaneLineEngine.CleanZeroCurves(mergedLines);

            // 延伸处理
            var extendedLines = ThLaneLineExtendEngine.ExtendBufferEx(mergedLines);
            extendedLines = ThLaneLineMergeExtension.Merge(extendedLines);
            extendedLines = ThLaneLineEngine.Noding(extendedLines);
            extendedLines = ThLaneLineEngine.CleanZeroCurves(extendedLines);

            return extendedLines;
        }
    }
}
