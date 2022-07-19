using System;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThFullOverlapBeamMarkGrouper
    {
        private double ClosestDistanceTolerance = 50.0; // 文字中心到文字中心的距离范围
        private DBObjectCollection BeamMarks { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThFullOverlapBeamMarkGrouper(DBObjectCollection beamMarks)
        {
            BeamMarks = beamMarks;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(beamMarks);
        }
        public void Group()
        {
            // 按文字中心靠近
            throw new NotImplementedException();
        }
    }
}
