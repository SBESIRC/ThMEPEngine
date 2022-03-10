using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class IsolatingSwitchModel : NotifyPropertyChangedBase
    {
        readonly IsolatingSwitch isolatingSwitch;
        public IsolatingSwitchModel(IsolatingSwitch isolatingSwitch)
        {
            this.isolatingSwitch = isolatingSwitch;
        }
        [DisplayName("元器件类型")]
        public string Type => "隔离开关";
        [DisplayName(null)]
        public string Content => isolatingSwitch.Content;
        [DisplayName("隔离开关类型")]
        public string IsolatingSwitchType
        {
            get => isolatingSwitch.IsolatingSwitchType;
            set
            {
                isolatingSwitch.IsolatingSwitchType = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("极数")]
        public string PolesNum
        {
            get => isolatingSwitch.PolesNum;
            set
            {
                isolatingSwitch.PolesNum = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => isolatingSwitch.RatedCurrent;
            set
            {
                isolatingSwitch.RatedCurrent = value;
                OnPropertyChanged(nameof(Content));
            }
        }
    }
}
