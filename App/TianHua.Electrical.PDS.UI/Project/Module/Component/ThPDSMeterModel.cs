using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public abstract class ThPDSMeterModel : NotifyPropertyChangedBase
    {
    }
    public class ThPDSMMeterTransformerModel : ThPDSMeterModel
    {
        readonly MeterTransformer meterTransformer;
        public ThPDSMMeterTransformerModel(MeterTransformer meterTransformer)
        {
            this.meterTransformer = meterTransformer;
        }
        [DisplayName("内容")]
        public string Content { get => meterTransformer.Content; }
        [DisplayName("电能表类型")]
        public string MeterSwitchType { get => meterTransformer.MeterSwitchType; set => meterTransformer.MeterSwitchType = value; }
        [DisplayName("极数")]
        public string PolesNum { get => meterTransformer.PolesNum; set => meterTransformer.PolesNum = value; }
        [DisplayName("额定电流")]
        public string RatedCurrent { get => meterTransformer.RatedCurrent; set => meterTransformer.RatedCurrent = value; }
    }
    public class ThPDSCurrentTransformerModel : ThPDSMeterModel
    {
        readonly CurrentTransformer currentTransformer;
        public ThPDSCurrentTransformerModel(CurrentTransformer currentTransformer)
        {
            this.currentTransformer = currentTransformer;
        }
        public string ContentMT { get => currentTransformer.ContentMT; }
        public string ContentCT { get => currentTransformer.ContentCT; }
        [DisplayName("电能表类型")]
        public string MTSwitchType { get => currentTransformer.MTSwitchType; set => currentTransformer.MTSwitchType = value; }
        [DisplayName("间接表类型")]
        public string CTSwitchType { get => currentTransformer.CTSwitchType; set => currentTransformer.CTSwitchType = value; }
        [DisplayName("极数")]
        public string PolesNum { get => currentTransformer.PolesNum; set => currentTransformer.PolesNum = value; }
        [DisplayName("额定电流")]
        public string RatedCurrent { get => currentTransformer.RatedCurrent; set => currentTransformer.RatedCurrent = value; }
    }
}
