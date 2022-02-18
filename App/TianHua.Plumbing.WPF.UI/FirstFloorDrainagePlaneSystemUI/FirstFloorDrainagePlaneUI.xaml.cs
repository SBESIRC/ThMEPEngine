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
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using TianHua.Plumbing.WPF.UI.UI;

namespace TianHua.Plumbing.WPF.UI.FirstFloorDrainagePlaneSystemUI
{
    /// <summary>
    /// FirstFloorDrainagePlaneUI.xaml 的交互逻辑
    /// </summary>
    public partial class FirstFloorDrainagePlaneUI : ThCustomWindow
    {
        public FirstFloorDrainagePlaneUI()
        {
            InitializeComponent();
        }

        private void btnPipeLine_Click(object sender, RoutedEventArgs e)
        {
            //if (null == viewModel || viewModel.FloorFrameds == null || viewModel.FloorFrameds.Count < 1)
            //{
            //    MessageBox.Show("没有任何楼层信息，在读取楼层信息后在进行相应的操作，如果图纸中也没有楼层信息，请放置楼层信息后再进行后续操作",
            //        "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            ////放置用户重复点击按钮，先将按钮置为不可用，业务完成后再将按钮置为可用
            ////直接设置后，后续的页面逻辑会卡UI线程，需要刷新一下界面
            //try
            //{
            //    var config = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
            //    FormUtil.DisableForm(gridForm);
            //    ThDrainSystemAboveGroundCmd thDrainSystem = new ThDrainSystemAboveGroundCmd(viewModel.FloorFrameds.ToList(), setViewModel, config);
            //    thDrainSystem.Execute();
            //    //执行完成后窗口焦点不在CAD上，CAD界面不会及时更新，触发焦点到CAD
            //    ThMEPWSS.Common.Utils.FocusToCAD();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
            //finally
            //{
            //    FormUtil.EnableForm(gridForm);
            //}
        }

        private void btnSltFloor_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
