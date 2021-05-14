using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Plumbing.WPF.UI.ViewModels
{
    public class DrainageViewModel: NotifyPropertyChangedBase
    {
        public DrainageViewModel() 
        {
            //测试数据
            FloorListDatas = new List<string>();
            FloorListDatas.Add("测试楼层1");
            FloorListDatas.Add("测试楼层2");
            FloorListDatas.Add("测试楼层3");
            FloorListDatas.Add("测试楼层4");
            FloorListDatas.Add("测试楼层5");
            FloorListDatas.Add("测试楼层6");

            DynamicRadioButtons = new ObservableCollection<DynamicRadioButtonViewModel>();
            DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "分组1",GroupName="group",IsChecked=true, SetViewModel =new DrainageSetViewModel()});
            DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "分组2", GroupName = "group", IsChecked = false, SetViewModel = new DrainageSetViewModel() });
            DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "分组3", GroupName = "group", IsChecked = false, SetViewModel = new DrainageSetViewModel() });
            DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "分组4", GroupName = "group", IsChecked = false, SetViewModel = new DrainageSetViewModel() });

        }
        private List<string> floorListDatas { get; set; }
        public List<string> FloorListDatas 
        {
            get { return floorListDatas; }
            set 
            {
                floorListDatas = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<DynamicRadioButtonViewModel> dynamicRadioButtons { get; set; }
        public ObservableCollection<DynamicRadioButtonViewModel> DynamicRadioButtons 
        {
            get { return dynamicRadioButtons; }
            set 
            {
                dynamicRadioButtons = value;
                this.RaisePropertyChanged();
            }
        }
        public DynamicRadioButtonViewModel SelectRadionButton 
        {
            get 
            {
                if (null == dynamicRadioButtons || dynamicRadioButtons.Count < 1)
                    return null;
                return dynamicRadioButtons.Where(c => c.IsChecked).FirstOrDefault();
            }
        }
    }
    public class DynamicRadioButton 
    {
        public string Content { get; set; }
        public string GroupName { get; set; }
        public bool IsChecked { get; set; }
    }
    public class DynamicRadioButtonViewModel: DynamicRadioButton
    {
        public DrainageSetViewModel SetViewModel { get; set; }
    }
}
