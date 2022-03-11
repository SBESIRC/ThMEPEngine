using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSContactorModel : NotifyPropertyChangedBase
    {
        readonly Contactor contactor;

        public ThPDSContactorModel(Contactor contactor)
        {
            this.contactor = contactor;
        }
        [DisplayName(null)]
        public string Content => contactor.Content;

        [DisplayName("元器件类型")]
        public string Type => "接触器";
        [DisplayName("接触器类型")]
        public string ContactorType
        {
            get => contactor.ContactorType;
            set
            {
                contactor.ContactorType = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("极数")]
        public string PolesNum
        {
            get => contactor.PolesNum;
            set
            {
                contactor.PolesNum = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => contactor.RatedCurrent;
            set
            {
                contactor.RatedCurrent = value;
                OnPropertyChanged(nameof(Content));
            }
        }
    }
}
