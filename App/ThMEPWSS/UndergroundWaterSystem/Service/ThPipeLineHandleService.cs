using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.LaneLine;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThPipeLineHandleService
    {
        public ThPipeLineHandleService()
        {

        }
        /// <summary>
        /// points:如果横管末端具有立管（points），则不考虑
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="points"></param>
        /// <param name="tol"></param>
        public List<Line> ConnectLinesWithSpacing(List<Line> lines, List<Point3d> points, double tol = 500)
        {
            List<Line> mergedLines = new();
            lines.ForEach(o => mergedLines.Add(o));
            //ConnectBrokenLine(lines, points).Where(o => o.Length > 0).ForEach(o => mergedLines.Add(o));
            ConnectBrokenLine(lines, new List<Point3d>() { }, points).Where(o => o.Length > 0).ForEach(o => mergedLines.Add(o));
            var objs = new DBObjectCollection();
            mergedLines.ForEach(o => objs.Add(o));
            //var processedLines = ThLaneLineMergeExtension.Merge(objs).Cast<Line>().ToList();
            return mergedLines;
        }
    }
}
