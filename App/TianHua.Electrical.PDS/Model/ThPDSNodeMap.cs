using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSNodeMap
    {
        public Dictionary<ThPDSCircuitGraphNode, List<ObjectId>> NodeMap;
        public string ReferenceDWG;

        public ThPDSNodeMap()
        {
            NodeMap = new Dictionary<ThPDSCircuitGraphNode, List<ObjectId>>();
            ReferenceDWG = "";
        }
    }
}
