using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.ViewModel
{
    public partial class DrainageSystemSupplyAxonoViewModel : NotifyPropertyChangedBase
    {
        private ObservableCollection<UListItemData> _scenarioList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> scenarioList
        {
            get { return _scenarioList; }
            set
            {
                _scenarioList = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _scenario { get; set; }

        public UListItemData scenario
        {
            get { return _scenario; }
            set
            {
                _scenario = value;
                this.RaisePropertyChanged();
            }
        }

        private double _alpha = 1.5;

        public double alpha
        {
            get { return _alpha; }
            set
            {
                _alpha = value;
                this.RaisePropertyChanged();
            }
        }

        public Dictionary <int,double> scenarioValue = new Dictionary<int, double> ();
        public DrainageSystemSupplyAxonoViewModel ()
        {
            scenarioList.Add(new UListItemData("幼儿园、托儿所、养老院", 0, null));
            scenarioList.Add(new UListItemData("门诊部、诊疗所", 1, null));
            scenarioList.Add(new UListItemData("办公楼、商场", 2, null));
            scenarioList.Add(new UListItemData("图书馆", 3, null));
            scenarioList.Add(new UListItemData("书店", 4, null));
            scenarioList.Add(new UListItemData("教学楼", 5, null));
            scenarioList.Add(new UListItemData("医院、疗养院、休养所", 6, null));
            scenarioList.Add(new UListItemData("酒店式公寓", 7, null));
            scenarioList.Add(new UListItemData("宿舍、旅馆、招待所、宾馆", 8, null));
            scenarioList.Add(new UListItemData("客运站、航站楼、会展中心、公共厕所", 9, null));
            scenario = scenarioList.FirstOrDefault();

            scenarioValue.Add(0, 1.2);
            scenarioValue.Add(1, 1.4);
            scenarioValue.Add(2, 1.5);
            scenarioValue.Add(3, 1.6);
            scenarioValue.Add(4, 1.7);
            scenarioValue.Add(5, 1.8);
            scenarioValue.Add(6, 2.0);
            scenarioValue.Add(7, 2.2);
            scenarioValue.Add(8, 2.5);
            scenarioValue.Add(9, 3.0);

        }

    }
}
