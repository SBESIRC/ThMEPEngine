using ThMEPHVAC.EQPMFanSelect;

namespace ThMEPHVAC.ParameterService
{
    public class FanSelectTypeParameter
    {
        FanSelectTypeParameter()
        {
        }
        public static FanSelectTypeParameter Instance = new FanSelectTypeParameter();
        public FanDataModel FanData { get; set; }
        public FanDataModel ChildFanData { get; set; }
    }
}
