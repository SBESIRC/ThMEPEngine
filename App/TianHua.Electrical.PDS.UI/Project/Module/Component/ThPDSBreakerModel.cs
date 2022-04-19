using System;
using ThCADExtension;
using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSBreakerModel : NotifyPropertyChangedBase
    {
        protected readonly Breaker _breaker;

        public ThPDSBreakerModel(Breaker breaker)
        {
            _breaker = breaker;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content
        {
            get
            {
                switch (_breaker.ComponentType)
                {
                    case ComponentType.CB:
                    case ComponentType.一体式RCD:
                        {
                            if (_breaker.Appendix == AppendixType.ST)
                            {
                                return $"{Model}{FrameSpecification}-{TripUnitType}{RatedCurrent}/{PolesNum}/{(Appendix == AppendixType.无 ? "" : Appendix)}";
                            }
                            else
                            {
                                return $"{Model}{FrameSpecification}-{TripUnitType}{RatedCurrent}/{PolesNum}";
                            }
                        }
                    case ComponentType.组合式RCD:
                        {
                            return $"{Model}{FrameSpecification}-{TripUnitType}{RatedCurrent}/{PolesNum}/{Appendix} {RCDType}{ResidualCurrent.GetDescription()}";
                        }
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _breaker.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public BreakerModel Model
        {
            get => _breaker.Model;
            set
            {
                _breaker.SetModel(value);
                OnPropertyChanged();
            }
        }

        [Category("元器件参数")]
        [DisplayName("壳架规格")]
        [Editor(typeof(ThPDSFrameSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string FrameSpecification
        {
            get => _breaker.FrameSpecification;
            set
            {
                _breaker.SetFrameSpecification(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _breaker.PolesNum;
            set
            {
                _breaker.SetPolesNum(value);
                OnPropertyChanged();
            }
        }

        [Category("元器件参数")]
        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _breaker.RatedCurrent;
            set
            {
                _breaker.SetRatedCurrent(value);
                OnPropertyChanged();
            }
        }

        [Category("元器件参数")]
        [DisplayName("脱扣器类型")]
        [Editor(typeof(ThPDSBreakerTripDevicePropertyEditor), typeof(PropertyEditorBase))]
        public string TripUnitType
        {
            get => _breaker.TripUnitType;
            set
            {
                _breaker.SetTripDevice(value);
                OnPropertyChanged();
            }
        }

        [ReadOnly(true)]
        [DisplayName("附件")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<AppendixType>), typeof(PropertyEditorBase))]
        public AppendixType Appendix
        {
            get => _breaker.Appendix;
            set
            {
                _breaker.Appendix = value;
                OnPropertyChanged(nameof(Appendix));
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("RCD类型")]
        [Editor(typeof(ThPDSBreakerRCDTypePropertyEditor), typeof(PropertyEditorBase))]
        public RCDType RCDType
        {
            get => _breaker.RCDType;
            set
            {
                _breaker.SetRCDType(value);
                OnPropertyChanged();
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("剩余电流动作")]
        [Editor(typeof(ThPDSResidualCurrentPropertyEditor<ResidualCurrentSpecification>), typeof(PropertyEditorBase))]
        public ResidualCurrentSpecification ResidualCurrent
        {
            get => _breaker.ResidualCurrent;
            set
            {
                _breaker.SetResidualCurrent(value);
                OnPropertyChanged();
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<RCDType> AlternativeRCDTypes
        {
            get => _breaker.GetRCDTypes();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<ResidualCurrentSpecification> AlternativeResidualCurrents
        {
            get => _breaker.GetResidualCurrents();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<BreakerModel> AlternativeModels
        {
            get => _breaker.GetModels();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativePolesNums
        {
            get => _breaker.GetPolesNums();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeRatedCurrents
        {
            get => _breaker.GetRatedCurrents();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeTripDevices
        {
            get => _breaker.GetTripDevices();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeFrameSpecifications
        {
            get => _breaker.GetFrameSpecifications();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsAppendixEnabled
        {
            get => _breaker.ComponentType != ComponentType.组合式RCD;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public ComponentType ComponentType => _breaker.ComponentType;

        protected virtual void OnPropertyChanged()
        {
            OnPropertyChanged(nameof(Model));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(RCDType));
            OnPropertyChanged(nameof(PolesNum));
            OnPropertyChanged(nameof(RatedCurrent));
            OnPropertyChanged(nameof(TripUnitType));
            OnPropertyChanged(nameof(ResidualCurrent));
            OnPropertyChanged(nameof(FrameSpecification));
        }
    }
}
