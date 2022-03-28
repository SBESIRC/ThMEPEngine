﻿using ThCADExtension;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.UI.Editors;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 隔离开关
    /// </summary>
    public class ThPDSIsolatingSwitchModel : NotifyPropertyChangedBase
    {
        private readonly IsolatingSwitch _isolatingSwitch;
        public ThPDSIsolatingSwitchModel(IsolatingSwitch isolatingSwitch)
        {
            _isolatingSwitch = isolatingSwitch;
        }

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type
        {
            get => _isolatingSwitch.ComponentType.GetDescription();
        }

        [DisplayName("型号")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _isolatingSwitch.Model;
            set
            {
                _isolatingSwitch.Model = value;
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("极数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _isolatingSwitch.PolesNum;
            set
            {
                _isolatingSwitch.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => _isolatingSwitch.RatedCurrent;
            set
            {
                _isolatingSwitch.RatedCurrent = value;
                OnPropertyChanged(nameof(RatedCurrent));
                OnPropertyChanged(nameof(Content));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("额定电压")]
        public string MaxKV => _isolatingSwitch.MaxKV;

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("内容")]
        public string Content => $"{Model} {RatedCurrent}/{PolesNum}";
    }
}
