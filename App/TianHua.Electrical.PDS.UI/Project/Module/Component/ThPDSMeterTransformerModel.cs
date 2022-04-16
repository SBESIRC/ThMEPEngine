using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSMeterTransformerModel : ThPDSMeterBaseModel
    {
        private MeterTransformer Meter => _meter as MeterTransformer;
        public ThPDSMeterTransformerModel(Meter meter) : base(meter)
        {

        }

        [Category("元器件参数")]
        [DisplayName("电能表规格")]
        [Editor(typeof(ThPDSMTSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string MTSpecification
        {
            get => Meter.MeterParameter;
            set
            {
                Meter.SetParameters(value);
                OnPropertyChanged(nameof(MTSpecification));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string ContentMT
        {
            get
            {
                if (PolesNum == "1P")
                {
                    return MTSpecification;
                }
                else
                {
                    return "3×" + MTSpecification;
                }
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> MTSpecifications => Meter.GetParameters();
    }
}