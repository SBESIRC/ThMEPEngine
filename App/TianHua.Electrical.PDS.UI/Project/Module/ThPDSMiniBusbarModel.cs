using System.ComponentModel;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Project.Module
{
    public class ThPDSMiniBusbarModel : NotifyPropertyChangedBase
    {
        private readonly MiniBusbar _miniBusbar;
        private readonly ThPDSProjectGraphNode _node;

        public ThPDSMiniBusbarModel(ThPDSProjectGraphNode node, MiniBusbar busbar)
        {
            _node = node;
            _miniBusbar = busbar;
        }

        [DisplayName("功率")]
        [Category("小母排参数")]
        public double Power
        {
            get => _miniBusbar.Power;
            set
            {
                _miniBusbar.Power = value;
                OnPropertyChanged(nameof(Power));
            }
        }

        [ReadOnly(true)]
        [DisplayName("相数")]
        [Category("小母排参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<ThPDSPhase>), typeof(PropertyEditorBase))]
        public ThPDSPhase Phase => _miniBusbar.Phase;


        [DisplayName("需要系数")]
        [Category("小母排参数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double DemandFactor
        {
            get => _miniBusbar.DemandFactor;
            set
            {
                _miniBusbar.SetDemandFactor(_node, value);
                OnPropertyChanged(nameof(DemandFactor));
            }
        }

        [DisplayName("需要系数")]
        [Category("小母排参数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double PowerFactor
        {
            get => _miniBusbar.PowerFactor;
            set
            {
                _miniBusbar.SetPowerFactor(_node, value);
                OnPropertyChanged(nameof(PowerFactor));
            }
        }

        [ReadOnly(true)]
        [DisplayName("计算电流")]
        [Category("小母排参数")]
        public double CalculateCurrent => _miniBusbar.CalculateCurrent;
    }
}
