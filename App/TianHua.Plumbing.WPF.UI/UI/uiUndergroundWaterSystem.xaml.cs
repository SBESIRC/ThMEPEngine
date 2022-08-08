using AcHelper;
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
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.UndergroundWaterSystem.Command;
using ThMEPWSS.UndergroundWaterSystem.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiUndergroundWaterSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiUndergroundWaterSystem : ThCustomWindow
    {
        public ThWaterSystemInfoModel InfoModel { set; get; }
        private static ThWaterSystemInfoViewModel ViewModel = null;
        public uiUndergroundWaterSystem()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new ThWaterSystemInfoViewModel();
            }
            DataContext = ViewModel;
            InfoModel = new ThWaterSystemInfoModel();
        }

        private void btnParaSet_Click(object sender, RoutedEventArgs e)
        {
            var paramUi = new uiUndergroundWaterSystemSet();
            paramUi.GetViewModel().strFloorLineSpace = InfoModel.FloorLineSpace.ToString();
            if (paramUi.ShowDialog() == true)
            {
                //取出其中的参数信息
                var floorSpace = double.Parse(paramUi.GetViewModel().strFloorLineSpace);
                InfoModel.FloorLineSpace = floorSpace;
            }
        }
        private void btnFloorLimit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThMEPWSS.Common.Utils.FocusMainWindow();
                ThMEPWSS.Common.Utils.CreateFloorFraming(false);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnFloorRead_Click(object sender, RoutedEventArgs e)
        {
            var readFloorFrameCmd = new ThReadFloorFrameCmd();
            readFloorFrameCmd.Execute();
            //添加到ViewModel中间去
            var floorList = new List<String>();
            foreach(var floor in readFloorFrameCmd.FloorList)
            {
                floorList.Add(floor.FloorName);
            }
            ViewModel.FloorListDatas = floorList;
            InfoModel.FloorList = readFloorFrameCmd.FloorList;
        }

        private void btnCreateSystemMap_Click(object sender, RoutedEventArgs e)
        {
            var cmd = new ThUndergroundWaterSystemCmd();
            cmd.InfoModel = InfoModel;
            cmd.Execute();
        }

        private void btn_Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var web = "http://thlearning.thape.com.cn/kng/view/video/d4676fb948e3432fa46ec5677b2123b4.html";
                System.Diagnostics.Process.Start(web);
            }
            catch (Exception ex)
            {
                MessageBox.Show("抱歉，出现未知错误\r\n" + ex.Message);
            }
        }
    }
}
