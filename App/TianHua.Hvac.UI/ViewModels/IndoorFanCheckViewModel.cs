using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.IndoorFanModels;

namespace TianHua.Hvac.UI.ViewModels
{
    class IndoorFanCheckViewModel : NotifyPropertyChangedBase
    {
        public IndoorFanCheckModel indoorFanCheck { get; }
        public IndoorFanCheckViewModel()
        {
            indoorFanCheck = new IndoorFanCheckModel();
            indoorFanCheck.HotColdType = EnumHotColdType.Cold;
            indoorFanCheck.MarkOverRoom = true;
            indoorFanCheck.MarkNotEnoughRoom = true;
            indoorFanCheck.MarkOverPercentage = 30;
        }
        public EnumHotColdType HotColdType
        {
            get { return indoorFanCheck.HotColdType; }
            set
            {
                indoorFanCheck.HotColdType = value;
                this.RaisePropertyChanged();
            }
        }
        public bool MarkNotEnoughRoom
        {
            get { return indoorFanCheck.MarkNotEnoughRoom; }
            set 
            {
                indoorFanCheck.MarkNotEnoughRoom = value;
                this.RaisePropertyChanged();
            }
        }
        public bool MarkOverRoom 
        {
            get { return indoorFanCheck.MarkOverRoom; }
            set 
            {
                indoorFanCheck.MarkOverRoom = value;
                this.RaisePropertyChanged();
            }
        }
        public double MarkOverPercentage 
        {
            get { return indoorFanCheck.MarkOverPercentage; }
            set 
            {
                indoorFanCheck.MarkOverPercentage = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
