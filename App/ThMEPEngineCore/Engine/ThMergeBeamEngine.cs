using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThMergeBeamEngine : ThBuildingElementPreprocessEngine
    {
        public List<ThIfcBuildingElement> BeamElements { get; set; }

        private IEnumerable<ThIfcLineBeam> LineBeams
        {
            get
            {
                return BeamEngine.Elements.Where(o => o is ThIfcLineBeam).Cast<ThIfcLineBeam>();
            }
        }

        private ThBeamConnectRecogitionEngine BeamConnectRecogitionEngine { get; set; }

        private ThCADCoreNTSSpatialIndex SpatialIndex
        {
            get
            {
                return BeamConnectRecogitionEngine.SpatialIndexManager.BeamSpatialIndex;
            }
        }

        private ThBeamRecognitionEngine BeamEngine
        {
            get
            {
                return BeamConnectRecogitionEngine.BeamEngine;
            }
        }

        public ThMergeBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamElements = new List<ThIfcBuildingElement>();
            BeamConnectRecogitionEngine = thBeamConnectRecogitionEngine;
        }

        public void Merge()
        {
            foreach (var beam in LineBeams)
            {
                var poly = beam.Outline as Polyline;
                var objs = SpatialIndex.SelectCrossingPolygon(poly);
                if (objs.Count == 1)
                {
                    continue;
                }
                var beams = BeamEngine.FilterByOutline(objs)
                    .Cast<ThIfcLineBeam>()
                    .Where(o => IsParallel(beam, o))
                    .Where(o => IsOverlap(beam, o));
                if (beams.Count() == 1)
                {
                    continue;
                }
                var tagBeams = beams.Where(o => SpatialIndex.Tag(o.Outline) != null);
                if (tagBeams.Count() == 0)
                {
                    var tag = Guid.NewGuid().ToString();
                    beams.ForEach(o => SpatialIndex.AddTag(o.Outline, tag));
                }
                else
                {
                    var tag = SpatialIndex.Tag(tagBeams.First().Outline);
                    beams.ForEach(o => SpatialIndex.AddTag(o.Outline, tag));
                }
            }
            var groups = LineBeams.GroupBy(o => SpatialIndex.Tag(o.Outline));
            foreach (var group in groups)
            {
                if (group.Key == null)
                {
                    group.ForEach(o => BeamElements.Add(o));
                }
                else
                {
                    using (var ov = new ThCADCoreNTSFixedPrecision())
                    {
                        var outlines = group.Select(o => o.Outline).ToCollection().Boundaries();
                        outlines.Cast<Polyline>().ForEachDbObject(o =>
                        {
                            var rectangle = o.GetMinimumRectangle();
                            BeamElements.Add(ThIfcLineBeam.Create(rectangle));
                        });
                    }
                }
            }
            BeamEngine.Elements = BeamElements;
        }
    }
}
