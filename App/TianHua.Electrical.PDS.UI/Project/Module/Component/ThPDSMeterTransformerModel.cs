using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSMeterTransformerModel : ThPDSMeterBaseModel
    {
        readonly MeterTransformer meterTransformer;
        public ThPDSMeterTransformerModel(MeterTransformer meterTransformer)
        {
            this.meterTransformer = meterTransformer;
        }
        [DisplayName("内容")]
        public string Content { get => meterTransformer.Content; }
        //[DisplayName("电能表类型")]
        //public string MeterSwitchType { get => meterTransformer.MeterSwitchType; set => meterTransformer.MeterSwitchType = value; }
        //[DisplayName("极数")]
        //public string PolesNum { get => meterTransformer.PolesNum; set => meterTransformer.PolesNum = value; }
        //[DisplayName("额定电流")]
        //public string RatedCurrent { get => meterTransformer.RatedCurrent; set => meterTransformer.RatedCurrent = value; }
    }
}
