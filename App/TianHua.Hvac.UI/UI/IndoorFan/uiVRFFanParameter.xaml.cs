using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ThControlLibraryWPF;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.IndoorFanModels;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    /// <summary>
    /// uiVRFFanParameter.xaml 的交互逻辑
    /// </summary>
    public partial class uiVRFFanParameter : ThCustomWindow
    {
        EnumFanType _showFanType;
        FanDataShowViewModel dataShowViewModel;
        public uiVRFFanParameter(List<IndoorFanBase> indoorFans, EnumFanType fanType, string workingName)
        {
            InitializeComponent();
            _showFanType = fanType;
            ShowFanLable(workingName);
            dataShowViewModel = new FanDataShowViewModel();
            foreach (var item in indoorFans)
            {
                dataShowViewModel.FanInfos.Add(item);
            }
            this.DataContext = dataShowViewModel;
        }
        public FanDataShowViewModel GetViewModelData()
        {
            return dataShowViewModel;
        }
        void ShowFanLable(string workingName)
        {
            string name = "";
            switch (_showFanType)
            {
                case EnumFanType.VRFConditioninFourSides:
                    name = "VRF室内机（四面出风）：";
                    dGridVRFFan.Visibility = Visibility.Collapsed;
                    dGridVRFSideFan.Visibility = Visibility.Visible;
                    break;
                case EnumFanType.VRFConditioninConduit:
                    name = "VRF室内机（管道机）：";
                    dGridVRFSideFan.Visibility = Visibility.Collapsed;
                    dGridVRFFan.Visibility = Visibility.Visible;
                    break;
            }
            name += workingName;
            lableFanType.Content = name;
        }
        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        private void addRow_Click(object sender, RoutedEventArgs e)
        {
            var copy = dataShowViewModel.SelectFanData;
            if (null == copy)
                copy = dataShowViewModel.FanInfos.First();
            var clone = ModelCloneUtil.Copy(copy);
            dataShowViewModel.FanInfos.Add(clone);
        }
        private void deleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (dataShowViewModel.SelectFanData == null)
            {
                MessageBox.Show("没有选中有效的风机信息，无法进行删除操作", "删除提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dataShowViewModel.FanInfos.Count < 2)
            {
                MessageBox.Show("当前风机信息过少，不能进行删除操作", "删除提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var rmFan = dataShowViewModel.SelectFanData;
            var showMsg = string.Format("确定删除 风机{0}吗？该过程是不可逆的，请谨慎操作。", rmFan.FanNumber);
            var res = MessageBox.Show(showMsg, "删除提醒", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes || res == MessageBoxResult.OK)
            {
                dataShowViewModel.FanInfos.Remove(rmFan);
                dataShowViewModel.SelectFanData = null;
            }
        }
    }
}
