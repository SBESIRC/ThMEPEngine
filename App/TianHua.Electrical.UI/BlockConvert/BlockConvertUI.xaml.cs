using AcHelper;
using System.Linq;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.Model;
using ThMEPElectrical.ViewModel;

namespace TianHua.Electrical.UI.BlockConvert
{
    public partial class BlockConvertUI : ThCustomWindow
    {
        public ThBlockConvertVM BlockConvertVM { get; set; }

        public BlockConvertUI()
        {
            BlockConvertVM = new ThBlockConvertVM();
            InitializeComponent();
            this.DataContext = BlockConvertVM;
        }

        private void wssConvertStrongEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = false;
            convertManualActuator.IsChecked = false;
        }

        private void wssConvertWeakEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = true;
        }

        private void wssConvertAllEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = true;
        }

        /// <summary>
        /// 提资转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBlockConvert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
            BlockConvertVM.BlockConvert();
        }

        /// <summary>
        /// 更新比对
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateCompare_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Hide();
            FocusToCAD();
            BlockConvertVM.UpdateCompare();
            Refresh();
            this.Show();
        }

        /// <summary>
        /// 提资更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBlockUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BlockConvertVM.BlockUpdate();
            Refresh();
        }

        /// <summary>
        /// 更新标注角度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRefreshLabel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Hide();
            BlockConvertVM.BlockRefresh();
            this.Show();
        }

        /// <summary>
        /// 视频教程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"http://thlearning.thape.com.cn/kng/view/video/30b3c88329674aac91d26d29f30f4ee8.html");
        }

        /// <summary>
        /// 忽略变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignoreChange_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var ids = table1.SelectedItems.OfType<BlockConvertInfo>().Select(o => o.Guid).ToList();
            if (ids.Count > 0)
            {
                BlockConvertVM.IgnoreChange(ids);
                Refresh();
            }
        }

        /// <summary>
        /// 局部更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void localUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var ids = table1.SelectedItems.OfType<BlockConvertInfo>().Select(o => o.Guid).ToList();
            if (ids.Count > 0)
            {
                BlockConvertVM.LocalUpdate(ids);
                Refresh();
            }
        }

        private void table1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FocusToCAD();
            BlockConvertVM.Zoom(table1.SelectedItem as BlockConvertInfo);
        }

        private void Refresh()
        {
            table1.ItemsSource = null;
            table1.ItemsSource = BlockConvertVM.BlockConvertInfos;
        }

        private static void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
