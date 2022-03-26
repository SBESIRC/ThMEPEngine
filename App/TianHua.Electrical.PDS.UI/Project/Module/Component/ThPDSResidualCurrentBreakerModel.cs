using HandyControl.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 剩余电流断路器（带漏电保护功能的断路器）
    /// </summary>
    public class ThPDSResidualCurrentBreakerModel : ThPDSBreakerBaseModel
    {
        public ThPDSResidualCurrentBreakerModel(BreakerBaseComponent component) : base(component)
        {

        }

        private ResidualCurrentBreaker RCBreaker
        {
            get
            {
                return _breaker as ResidualCurrentBreaker;
            }
        }

        [DisplayName("RCD类型")]
        [Editor(typeof(ThPDSBreakerRCDTypePropertyEditor), typeof(PropertyEditorBase))]
        public RCDType RCDType
        {
            get => RCBreaker.RCDType;
            set
            {
                RCBreaker.SetRCDType(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("剩余电流动作")]
        [Editor(typeof(ThPDSBreakerRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public ResidualCurrentSpecification ResidualCurrent
        {
            get => RCBreaker.ResidualCurrent;
            set
            {
                RCBreaker.ResidualCurrent = value;
                OnPropertyChanged();
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<RCDType> AlternativeRCDTypes
        {
            get => RCBreaker.GetRCDTypes();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<ResidualCurrentSpecification> AlternativeResidualCurrents
        {
            get => RCBreaker.GetResidualCurrents();
        }

        protected override void OnPropertyChanged()
        {
            base.OnPropertyChanged();
            OnPropertyChanged(nameof(RCDType));
            OnPropertyChanged(nameof(ResidualCurrent));
        }
    }
}
