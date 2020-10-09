using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThMergeOverlapBeamEngine
    {
        private ThBeamRecognitionEngine BeamEngine { get; set; }

        public List<ThIfcBuildingElement> BeamElements { get; set; }

        public ThMergeOverlapBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            BeamEngine = thBeamRecognitionEngine;
            BeamElements = thBeamRecognitionEngine.Elements;
        }

        public void MergeOverlap()
        {
            var results = new List<ThIfcBuildingElement>();
            foreach (Polyline outline in BeamEngine.Geometries.Boundaries())
            {
                results.Add(ThIfcLineBeam.Create(outline.GetMinimumRectangle()));
            }
            BeamElements = results;
        }
    }
}
