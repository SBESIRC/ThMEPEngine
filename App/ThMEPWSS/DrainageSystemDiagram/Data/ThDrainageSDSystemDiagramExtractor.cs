using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDSystemDiagramExtractor
    {
        public static List<Line> GetSD(Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer transformer)
        {
            var layerContainser = new List<string>() { "W-WSUP-COOL-PIPE-AI" , "W-WSUP-COOL-PIPE" };
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Line>()
                .Where(o => layerContainser.Contains ( o.Layer));

            List<Line> laneList = laneLines.Select(x => x.WashClone() as Line).ToList();

            laneList = laneList.Where(x => x != null).ToList();
            if (transformer != null)
            {
                laneList.ForEach(x => transformer.Transform(x));
            }

            laneList.ForEach(x => objs.Add(x));

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Line>().ToList();

            return sprayLines;
        }
    }
}
