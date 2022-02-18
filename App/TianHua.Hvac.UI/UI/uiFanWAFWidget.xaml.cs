using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThMEPHVAC.FanLayout.ViewModel;
/// <summary>
/// 壁式轴流风机
/// </summary>
namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanWAFWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanWAFWidget : UserControl
    {
        public uiFanWAFWidget()
        {
            InitializeComponent();
            WAFGrid.DataContext = new ThFanWAFViewModel();
        }
        private ThFanWAFViewModel ViewModel
        {
            get 
            { 
                return WAFGrid.DataContext as ThFanWAFViewModel; 
            }
        }
        public ThFanWAFConfigInfo GetFanWAFConfigInfo()
        {
            return ViewModel.fanWAFConfigInfo;
        }
        public void SetFanConfigInfoList(ObservableCollection<ThFanConfigInfo> fanInfoList)
        {
            ViewModel.fanWAFConfigInfo.FanSideConfigInfo.FanInfoList = fanInfoList;
            ViewModel.fanWAFConfigInfo.FanSideConfigInfo.FanConfigInfo = fanInfoList.FirstOrDefault();
        }
        private void btnAddFan_Click(object sender, RoutedEventArgs e)
        {
            uiFanInfoWidget fanWidget = new uiFanInfoWidget("DZ");
            if (fanWidget.ShowDialog() == true)
            {
                ViewModel.FanInfoConfigs.Add(fanWidget.GetFanConfigInfo());
                //写入到图纸
            }
        }
    }
}
