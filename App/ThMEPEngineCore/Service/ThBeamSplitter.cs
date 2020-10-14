using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Segment;

namespace ThMEPEngineCore.Service
{
    public abstract class ThBeamSplitter
    {
        public List<ThIfcBeam> SplitBeams { get; protected set; }
        protected List<ThSegment> Segments { get; set; }
        protected ThBeamSplitter(List<ThSegment> segments)
        {
            Segments = segments;
            SplitBeams = new List<ThIfcBeam>();
        }
        public abstract void Split();
        public abstract void SplitTType();
    }
}
