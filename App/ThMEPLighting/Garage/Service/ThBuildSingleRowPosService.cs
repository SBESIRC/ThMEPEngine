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
            List<List<Point3d>> segments,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
            :base(edges, segments, arrangeParameter, queryLightBlockService)
        {
        }

        public override void Build()
        {
            Segments.ForEach(o =>
            {
                var splitParameter = new ThLineSplitParameter
                {
                    Segment=o,
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
