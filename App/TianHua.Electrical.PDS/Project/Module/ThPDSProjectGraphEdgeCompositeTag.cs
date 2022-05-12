using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public class ThPDSProjectGraphEdgeCompositeTag : ThPDSProjectGraphEdgeTag
    {
        public ThPDSProjectGraphEdgeCompareTag CompareTag { get; set; }
        public ThPDSProjectGraphEdgeSingleTag SingleTag { get; set; }
        public ThPDSProjectGraphEdgeDuplicateTag DupTag { get; set; }
        public ThPDSProjectGraphEdgeCascadingErrorTag CascadingErrorTag { get; set; }
    }
}
