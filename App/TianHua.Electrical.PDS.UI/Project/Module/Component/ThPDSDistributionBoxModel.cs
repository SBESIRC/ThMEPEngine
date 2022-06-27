using System.ComponentModel;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 配电箱
    /// </summary>
    public class ThPDSDistributionBoxModel : NotifyPropertyChangedBase
    {
        private readonly ThPDSProjectGraphNode _node;
        public ThPDSDistributionBoxModel(ThPDSProjectGraphNode graphNode)
        {
            _node = graphNode;
        }

        [ReadOnly(true)]
        [Category("配电箱参数")]
        [DisplayName("配电箱编号")]
        public string ID
        {
            get => _node.Load.ID.LoadID;
        }

        [Browsable(true)]
        [DisplayName("功率")]
        [Category("配电箱参数")]
        [Editor(typeof(ThPDSNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double Power
        {
            get => _node.Details.LoadCalculationInfo.HighPower;
            set
            {
                _node.SetNodeHighPower(value);
                OnPropertyChanged(nameof(Power));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [Browsable(true)]
        [DisplayName("平时功率")]
        [Category("配电箱参数")]
        [Editor(typeof(ThPDSNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double LowPower
        {
            get => _node.Details.LoadCalculationInfo.LowPower;
            set
            {
                _node.SetNodeLowPower(value);
                OnPropertyChanged(nameof(LowPower));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [Browsable(true)]
        [DisplayName("消防功率")]
        [Category("配电箱参数")]
        [Editor(typeof(ThPDSNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double HighPower
        {
            get => _node.Details.LoadCalculationInfo.HighPower;
            set
            {
                _node.SetNodeHighPower(value);
                OnPropertyChanged(nameof(HighPower));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [ReadOnly(true)]
        [DisplayName("相数")]
        [Category("配电箱参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<ThPDSPhase>), typeof(PropertyEditorBase))]
        public ThPDSPhase Phase
        {
            get => _node.Load.Phase;
        }

        [Category("配电箱参数")]
        [DisplayName("需要系数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double DemandFactor
        {
            get => _node.Details.LoadCalculationInfo.HighDemandFactor;
            set
            {
                _node.SetDemandFactor(value);
                OnPropertyChanged(nameof(DemandFactor));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [Category("配电箱参数")]
        [DisplayName("功率因数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double PowerFactor
        {
            get => _node.Details.LoadCalculationInfo.PowerFactor;
            set
            {
                _node.SetPowerFactor(value);
                OnPropertyChanged(nameof(PowerFactor));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [ReadOnly(true)]
        [Category("配电箱参数")]
        [DisplayName("计算电流")]
        public string CalculateCurrent
        {
            get => string.Format("{0}", _node.Details.LoadCalculationInfo.HighCalculateCurrent);
        }

        [Category("配电箱参数")]
        [DisplayName("用途描述")]
        public string Description
        {
            get => _node.Load.ID.Description;
            set
            {
                _node.Load.ID.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        [Category("配电箱参数")]
        [DisplayName("箱体尺寸")]
        [Editor(typeof(ThPDSEnumPropertyEditor<BoxSize>), typeof(PropertyEditorBase))]
        public BoxSize BoxSize
        {
            get => _node.Details.BoxSize;
            set
            {
                _node.Details.BoxSize = value;
                OnPropertyChanged(nameof(BoxSize));
            }
        }

        [Category("配电箱参数")]
        [DisplayName("安装方式")]
        [Editor(typeof(ThPDSEnumPropertyEditor<BoxInstallationType>), typeof(PropertyEditorBase))]
        public BoxInstallationType BoxInstallationType
        {
            get => _node.Details.BoxInstallationType;
            set
            {
                _node.Details.BoxInstallationType = value;
                OnPropertyChanged(nameof(BoxInstallationType));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsDualPower => _node.Details.LoadCalculationInfo.IsDualPower;
    }
}
