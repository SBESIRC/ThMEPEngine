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
        [Browsable(false)]
        public double LowPower => _node.Details.LowPower;
        [Browsable(false)]
        public double HighPower => _node.Details.HighPower;

        [ReadOnly(true)]
        [Category("配电箱参数")]
        [DisplayName("配电箱编号")]
        public string ID
        {
            get => _node.Load.ID.LoadID;
        }

        [DisplayName("功率")]
        [Category("配电箱参数")]
        public double InstallCapacity
        {
            get => _node.Details.HighPower;
            set
            {
                _node.Details.HighPower = value;
                OnPropertyChanged(nameof(InstallCapacity));
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
            get => _node.Load.DemandFactor;
            set
            {
                _node.SetDemandFactor(value);
                OnPropertyChanged(nameof(DemandFactor));
            }
        }

        [Category("配电箱参数")]
        [DisplayName("功率因数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double PowerFactor
        {
            get => _node.Load.PowerFactor;
            set
            {
                _node.SetPowerFactor(value);
                OnPropertyChanged(nameof(PowerFactor));
            }
        }

        [ReadOnly(true)]
        [Category("配电箱参数")]
        [DisplayName("计算电流")]
        public double CalculateCurrent
        {
            get => _node.Load.CalculateCurrent;
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
    }
}
