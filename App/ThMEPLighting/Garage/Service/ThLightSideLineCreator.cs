using AcHelper;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using GeometryExtensions;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThLightSideLineCreator
    {
        private const double CutLineLength = 2.0; // 切割线长度
        /// <summary>
        /// 传入的中心线已经处理过了
        /// </summary>
        /// <param name="centers"></param>
        /// <param name="width"></param>
        public static List<Line> Create(List<Line> centerLines,double width)
        {
            // 按当前坐标系排序
            centerLines = centerLines.Sort(Active.Editor.WCS2UCS());

            // 合并
            var mergeLines = ThPriorityMergeLightLineService.Merge(centerLines);

            // Buffer
            var segments = mergeLines.Select(o => o.ToPolyline(ThGarageLightCommon.RepeatedPointDistance)).ToCollection();
            var bufferObjs = segments.Buffer(width / 2.0);

            // 切割
            var sideLines = bufferObjs.GetLines().Where(o => o.Length > 1.0).ToList();

            return SplitSideLines(sideLines, centerLines, width);
        }

        private static List<Line> SplitSideLines(List<Line> sideLines,List<Line> centerLines,
            double width)
        {
            // 获取要切割线槽的线
            var cutLines = ThCollectCutLinesService.Collect(
                centerLines, width / 2.0, CutLineLength);
            var cableObjs = new DBObjectCollection();
            sideLines.ForEach(o => cableObjs.Add(o));
            cutLines.ForEach(o => cableObjs.Add(o));
            var handleObjs = ThLaneLineEngine.Noding(cableObjs);
            return handleObjs.Cast<Line>().Where(o => o.Length > (CutLineLength + 1.0)).ToList();
        }
    }
}
