using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSCurrentTransformerModel : ThPDSMeterBaseModel
    {
        private CurrentTransformer Meter => _meter as CurrentTransformer;

        public ThPDSCurrentTransformerModel(Meter meter) : base(meter)
        {

        }

        [DisplayName("电能表规格")]
        [Editor(typeof(ThPDSMTSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string MTSpecification
        {
            get => Meter.MeterSwitchType;
            set
            {
                Meter.MeterSwitchType = value;
                OnPropertyChanged(nameof(MTSpecification));
            }
        }

        [DisplayName("互感器规格")]
        [Editor(typeof(ThPDSCTSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string CTSpecification
        {
            get => Meter.CurrentTransformerSwitchType;
            set
            {
                Meter.SetParameters(value);
                OnPropertyChanged(nameof(CTSpecification));
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
        public string ContentCT
        {
            get
            {
                if (PolesNum == "1P")
                {
                    return CTSpecification + "A";
                }
                else
                {
                    return "3×" + CTSpecification + "A";
                }
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> MTSpecifications => new() { MTSpecification };


        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> CTSpecifications => Meter.GetParameters();
    }
}
