using System;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThBuildSingleRowPosService: ThBuildLightPosService
    {
        public ThBuildSingleRowPosService(
            List<ThLightEdge> edges,
            List<Tuple<Point3d, Point3d>> splitPts,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
            :base(edges, splitPts, arrangeParameter, queryLightBlockService)
        {
        }

        public override void Build()
        {
            SplitPts.ForEach(o =>
            {
                var splitParameter = new ThLineSplitParameter
                {
                    LineSp = o.Item1,
                    LineEp = o.Item2,
                    Margin = ArrangeParameter.Margin,
                    Interval = ArrangeParameter.Interval,
                };
                if (ArrangeParameter.AutoGenerate)
                {
                    BuildByCalculation(splitParameter);
                }
                else
                {
                    BuildByExtractFromCad(splitParameter);
                }
            });
        }
    }
}
