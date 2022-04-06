using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSEdgeMap
    {
        public Dictionary<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>, List<ObjectId>> EdgeMap;
        public string ReferenceDWG;

        public ThPDSEdgeMap()
        {
            EdgeMap = new Dictionary<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>, List<ObjectId>>();
            ReferenceDWG = "";
        }
    }
}
