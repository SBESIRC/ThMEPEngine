using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngine : ThModelRecognitionEngine, IDisposable
    {
        public override List<ThIfcElement> Elements { get; set ; }
        public ThBeamRecognitionEngine()
        {
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var beamDbExtension = new ThStructureBeamDbExtension(Active.Database))
            {
                beamDbExtension.BuildElementCurves();
                DBObjectCollection beamCollection = new DBObjectCollection();
                beamDbExtension.BeamCurves.ForEach(o => beamCollection.Add(o));
                ThDistinguishBeamInfo thDisBeamInfo = new ThDistinguishBeamInfo();
                var beams = thDisBeamInfo.CalBeamStruc(beamCollection);
                beams.ForEach(o => acadDatabase.ModelSpace.Add(o.BeamBoundary));
            }
        }
    }
}
