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
/// 吊顶式排气扇
/// </summary>
namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanCEXHWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanCEXHWidget : UserControl
    {
        public uiFanCEXHWidget()
        {
            InitializeComponent();
            CEXHGrid.DataContext = new ThFanCEXHViewModel();
        }
        private ThFanCEXHViewModel ViewModel
        {
            get
            {
                return CEXHGrid.DataContext as ThFanCEXHViewModel;
            }
        }
        public void SetFanConfigInfoList(ObservableCollection<ThFanConfigInfo> fanInfoList)
        {
            ViewModel.FanCEXHConfigInfo.FanSideConfigInfo.FanInfoList = fanInfoList;
            ViewModel.FanCEXHConfigInfo.FanSideConfigInfo.FanConfigInfo = fanInfoList.FirstOrDefault();
        }
        public ThFanCEXHConfigInfo GetFanCEXHConfigInfo()
        {
            return ViewModel.FanCEXHConfigInfo;
        }
        private void btnAddFan_Click(object sender, RoutedEventArgs e)
        {
            uiFanInfoWidget fanWidget = new uiFanInfoWidget("BLD");
            if (fanWidget.ShowDialog() == true)
            {
                ViewModel.FanInfoConfigs.Add(fanWidget.GetFanConfigInfo());
            }
        }
    }
}
