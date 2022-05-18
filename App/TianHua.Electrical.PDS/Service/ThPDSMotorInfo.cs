using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSMotorInfo
    {
        public ThPDSLoadTypeCat_3 TypeCat_3 { get; set; }
        public bool FireLoad { get; set; }
        public string OperationMode { get; set; }
        public string FaultProtection { get; set; }
        public string Signal { get; set; }

        public ThPDSMotorInfo()
        {
            TypeCat_3 = ThPDSLoadTypeCat_3.None;
            FireLoad = false;
            OperationMode = "";
            FaultProtection = "";
            Signal = "";
        }
    }
}
