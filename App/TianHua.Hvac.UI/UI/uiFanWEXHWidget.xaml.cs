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
/// 壁式排气扇
/// </summary>
namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanWEXHWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanWEXHWidget : UserControl
    {
        private static ThFanWEXHViewModel ViewModel = null;
        public uiFanWEXHWidget()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new ThFanWEXHViewModel();
            }
            WEXHGrid.DataContext = ViewModel;
        }
        public void SetFanConfigInfoList(ObservableCollection<ThFanConfigInfo> fanInfoList)
        {
            ViewModel.fanWEXHConfigInfo.FanSideConfigInfo.FanInfoList = fanInfoList;
        }
        public ThFanWEXHConfigInfo GetFanWEXHConfigInfo()
        {
            return ViewModel.fanWEXHConfigInfo;
        }
        private void btnAddFan_Click(object sender, RoutedEventArgs e)
        {
            uiFanInfoWidget fanWidget = new uiFanInfoWidget();
            if (fanWidget.ShowDialog() == true)
            {
                ViewModel.FanInfoConfigs.Add(fanWidget.GetFanConfigInfo());
            }
        }
    }
}
