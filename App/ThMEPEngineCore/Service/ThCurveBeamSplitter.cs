using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Segment;

namespace ThMEPEngineCore.Service
{
    public class ThCurveBeamSplitter : ThBeamSplitter, IDisposable
    {
        private ThIfcArcBeam arcBeam { get; set; }
        private Arc CenterLine { get; set; }
        public ThCurveBeamSplitter(ThIfcArcBeam thIfcArcBeam,List<ThSegment> segments):base(segments)
        {
            arcBeam = thIfcArcBeam;
        }
        public void Dispose()
        {            
        }

        public override void Split()
        {
            throw new NotImplementedException();
        }
    }
}
