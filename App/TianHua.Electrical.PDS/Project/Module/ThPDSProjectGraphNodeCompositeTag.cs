namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphNodeCompositeTag : ThPDSProjectGraphNodeTag
    {
        public ThPDSProjectGraphNodeDataTag DataTag { get; set; }
        public ThPDSProjectGraphNodeCompareTag CompareTag { get; set; }
        public ThPDSProjectGraphNodeValidateTag ValidateTag { get; set; }
    }
}
