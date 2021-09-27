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
/// 壁式轴流风机
/// </summary>
namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanWAFWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanWAFWidget : UserControl
    {
        public ThFanWAFConfigInfo fanWAFConfigInfo = new ThFanWAFConfigInfo();
        public uiFanWAFWidget()
        {
            InitializeComponent();
        }

        private void btnAddFan_Click(object sender, RoutedEventArgs e)
        {
            uiFanInfoWidget fanWidget = new uiFanInfoWidget();
            if (fanWidget.ShowDialog() == true)
            {
                fanWAFConfigInfo.FanInfoList.Add(fanWidget.GetFanConfigInfo());
            }
        }
    }
}
