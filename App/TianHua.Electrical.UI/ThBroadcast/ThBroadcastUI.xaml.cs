using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.Service;

namespace TianHua.Electrical.UI.ThBroadcast
{
    /// <summary>
    /// ThBroadcastUI.xaml 的交互逻辑
    /// </summary>
    public partial class ThBroadcastUI : ThCustomWindow
    {
        public ThBroadcastUI()
        {
            InitializeComponent();

            this.MutexName = "Mutext_ThBroadcastUI";

            //设置默认值
            SetDefaultValue();
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        private void SetDefaultValue()
        {
            var scaleLst = new List<string>() { "150", "100" };
            BlockScale.ItemsSource = scaleLst;
            BlockScale.SelectedValue = scaleLst[0];
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            ThElectricalUIService.Instance.thGBParameter.Scale = double.Parse(BlockScale.SelectedItem.ToString());
            ThElectricalUIService.Instance.thGBParameter.BlindRadius = double.Parse(blindArea.Text.ToString());
            CommandHandlerBase.ExecuteFromCommandLine(false, "THGBBZ");
        }

        private void btnWiringConnect_Click(object sender, RoutedEventArgs e)
        {
            CommandHandlerBase.ExecuteFromCommandLine(false, "THGBLX");
        }

        private void btnGetBlindArea_Click(object sender, RoutedEventArgs e)
        {
            ThElectricalUIService.Instance.thGBParameter.BlindRadius = double.Parse(blindArea.Text.ToString());
            CommandHandlerBase.ExecuteFromCommandLine(false, "THGBMQ");
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/075be5d104964a9fab1b3f079c51d755.html");
        }
    }
}
