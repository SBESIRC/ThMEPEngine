using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public class ThPDSProjectGraphNodeCompositeTag : ThPDSProjectGraphNodeTag
    {
        public ThPDSProjectGraphNodeDataTag DataTag { get; set; }
        public ThPDSProjectGraphNodeCompareTag CompareTag { get; set; }
        public ThPDSProjectGraphNodeDuplicateTag DupTag { get; set; }
        public ThPDSProjectGraphNodeValidateTag ValidateTag { get; set; }
    }
}
