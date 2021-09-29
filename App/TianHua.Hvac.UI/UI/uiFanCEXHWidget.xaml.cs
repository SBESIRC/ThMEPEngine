using System;
using System.Collections.Generic;
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
        public ThFanCEXHViewModel ViewModel = null;
        public uiFanCEXHWidget()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new ThFanCEXHViewModel();
            }
            CEXHGrid.DataContext = ViewModel;
        }
        public ThFanCEXHConfigInfo GetFanCEXHConfigInfo()
        {
            return ViewModel.FanCEXHConfigInfo;
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
