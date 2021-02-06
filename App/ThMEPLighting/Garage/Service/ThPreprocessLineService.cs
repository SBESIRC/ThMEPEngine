using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThPreprocessLineService
    {
        public static List<Line> Preprocess(List<Line> curves)
        {
            var lines = curves.ToCollection();
            var cleanInstance = new ThLaneLineCleanService();
            lines = cleanInstance.Clean(lines);
            var extendLines = new DBObjectCollection();
            foreach (Line line in lines)
            {
                extendLines.Add(line.ExtendLine(1.0));
            }
            lines = ThLaneLineEngine.Noding(extendLines);
            lines = ThLaneLineEngine.CleanZeroCurves(lines);
            return lines.Cast<Line>().ToList();
        }
    }
}
