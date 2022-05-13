using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSBlockInfo
    {
        public string BlockName { get; set; }
        public ThPDSLoadTypeCat_1 Cat_1 { get; set; }
        public ThPDSLoadTypeCat_2 Cat_2 { get; set; }
        public string Properties { get; set; }
        public ThPDSCircuitType DefaultCircuitType { get; set; }
        public ThPDSPhase Phase { get; set; }
        public double DemandFactor { get; set; }
        public double PowerFactor { get; set; }
        public ThPDSFireLoad FireLoad { get; set; }
        public string DefaultDescription{ get; set; }
        public LayingSite CableLayingMethod1 { get; set; }
        public LayingSite CableLayingMethod2 { get; set; }

        public ThPDSBlockInfo()
        {
            CableLayingMethod1 = LayingSite.CC;
            CableLayingMethod2 = LayingSite.None;
        }
    }
}
