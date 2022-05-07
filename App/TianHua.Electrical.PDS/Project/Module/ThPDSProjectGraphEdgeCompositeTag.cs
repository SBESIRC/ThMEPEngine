using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public class ThPDSProjectGraphEdgeCompositeTag : ThPDSProjectGraphEdgeTag
    {
        public ThPDSProjectGraphEdgeDataTag DataTag { get; set; }
        public ThPDSProjectGraphEdgeCompareTag CompareTag { get; set; }
        public ThPDSProjectGraphEdgeSingleTag SingleTag { get; set; }
        public ThPDSProjectGraphEdgeDuplicateTag DupTag { get; set; }
    }
}
