
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Segment;

namespace ThMEPEngineCore.Service
{
    public class ThLinealBeamSplitter: ThBeamSplitter, IDisposable
    {
        private ThIfcLineBeam LineBeam { get; set; }       
       
        public ThLinealBeamSplitter(ThIfcLineBeam thIfcLineBeam, List<ThSegment> segments) :base(segments)
        {
            LineBeam = thIfcLineBeam;
        }
        public override void Split()
        {
            List<ThIfcLineBeam> beamContainer = new List<ThIfcLineBeam>() { LineBeam };
            Segments.ForEach(m =>
            {
                Dictionary<ThIfcLineBeam, List<ThIfcLineBeam>> beamDic = new Dictionary<ThIfcLineBeam, List<ThIfcLineBeam>>();
                beamContainer.ForEach(n =>
                {                    
                    ThLinealBeamSplitterExtension splitEx = new ThLinealBeamSplitterExtension(n, m.Outline);
                    splitEx.Split();
                    beamDic.Add(n, splitEx.SplitBeams);                    
                });
                beamContainer.Clear();
                beamDic.ForEach(o =>
                {
                    if(o.Value.Count>0)
                    {
                        beamContainer.AddRange(o.Value);
                    }
                    else
                    {
                        beamContainer.Add(o.Key);
                    }
                });
            });
            beamContainer.Where(o=>o.Uuid!=LineBeam.Uuid && 
            o.Length>= ThMEPEngineCoreCommon.BeamMinimumLength).ForEach(o => SplitBeams.Add(o));
        }
        public void Dispose()
        {            
        }        
    }
}
