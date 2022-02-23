namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSBlockInfo
    {
        public string BlockName { get; set; }
        public ThPDSLoadTypeCat_1 Cat_1 { get; set; }
        public ThPDSLoadTypeCat_2 Cat_2 { get; set; }
        public string Properties { get; set; }
        public ThPDSCircuitType DefaultCircuitType { get; set; }
    }
}
