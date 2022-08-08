using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;
using TianHua.Plumbing.WPF.UI.UI;

namespace ThMEPWSS.UndergroundFireHydrantSystem.UI
{
    public partial class UiFireHydrantSystem : ThCustomWindow
    {
        public static FireHydrantSystemViewModel viewModel;
        public UiFireHydrantSystem()
        {
            InitializeComponent();
            if (null == viewModel)
            {
                viewModel = new FireHydrantSystemViewModel();
            }
            DataContext = viewModel;
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThFireHydrantCmd(viewModel))
            {
                cmd.Execute();
            }
        }

        private void LoopMark_Click(object sender, RoutedEventArgs e)
        {
            FireHydrantSystemViewModel.InsertLoopMark();
        }

        private void NodeMark_Click(object sender, RoutedEventArgs e)
        {
            FireHydrantSystemViewModel.InsertSubLoopMark();
        }

        private void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone();
            UiFireHydrantSystemSet systemSet = new UiFireHydrantSystemSet(viewModel.SetViewModel);
            systemSet.Owner = this;
            var ret = systemSet.ShowDialog();
            if (ret == false)//用户取消了操作
            {
                viewModel.SetViewModel = oldViewModel;
                return;
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @" http://thlearning.thape.com.cn/kng/view/video/87edbfd8f0294c5f8bf884ceb04ed474.html");
        }
    }
}
