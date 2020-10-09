using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThJoinBeamEngine
    {
        private ThBeamRecognitionEngine BeamEngine { get; set; }
        private ThSpatialIndexManager SpatialIndexManager { get; set; }

        public List<ThIfcBuildingElement> BeamElements { get; set; }

        private IEnumerable<ThIfcLineBeam> LineBeams
        {
            get
            {
                return BeamElements.Where(o => o is ThIfcLineBeam).Cast<ThIfcLineBeam>();
            }
        }

        private ThCADCoreNTSSpatialIndex SpatialIndex
        {
            get
            {
                return SpatialIndexManager.BeamSpatialIndex;
            }
        }

        public ThJoinBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            BeamEngine = thBeamRecognitionEngine;
            SpatialIndexManager = thSpatialIndexManager;
            BeamElements = thBeamRecognitionEngine.Elements;
        }

        /// <summary>
        /// 合并由绘制误差造成的“断”梁
        /// </summary>
        public void Join()
        {
            var results = new List<ThIfcBuildingElement>();
            foreach (var lineBeam in LineBeams)
            {
                // 是否延长起始端?
                var outline = lineBeam.ExtendBoth(
                    ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance,
                    0.0);
                var objs = SpatialIndexManager.BeamSpatialIndex.SelectCrossingPolygon(outline);
                bool bExtendStart = (objs.Count > 0);

                // 是否延长结尾端?
                outline = lineBeam.ExtendBoth(
                    0.0,
                    ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
                objs = SpatialIndexManager.BeamSpatialIndex.SelectCrossingPolygon(outline);
                bool bExtendEnd = (objs.Count > 0);

                if (bExtendStart & bExtendEnd)
                {
                    // 延长两端
                    var newBeam = lineBeam.Clone() as ThIfcLineBeam;
                    newBeam.Outline = lineBeam.ExtendBoth(
                        ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance,
                        ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
                    results.Add(newBeam);
                }
                else if (bExtendStart)
                {
                    // 延长起始端
                    var newBeam = lineBeam.Clone() as ThIfcLineBeam;
                    newBeam.Outline = lineBeam.ExtendBoth(
                        ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance,
                        0.0);
                    results.Add(newBeam);
                }
                else if (bExtendEnd)
                {
                    // 延长结尾端
                    var newBeam = lineBeam.Clone() as ThIfcLineBeam;
                    newBeam.Outline = lineBeam.ExtendBoth(
                        0.0,
                        ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
                    results.Add(newBeam);
                }
                else
                {
                    // 不延长
                    results.Add(lineBeam);
                }
            }
            BeamElements = results;
        }
    }
}
