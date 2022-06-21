using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.UndergroundSpraySystem.ViewModel;
using ThMEPWSS.UndergroundSpraySystem.Command;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class UiUNDSpraySystem : ThCustomWindow
    {
        static SprayVM viewModel;
        public UiUNDSpraySystem()
        {
            InitializeComponent();
            if (null == viewModel)
                viewModel = new SprayVM();
            DataContext = viewModel;
        }

        private void BtnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateFloorFraming();
        }

        private void BtnReadStoreys_Click(object sender, RoutedEventArgs e)
        {
            viewModel.InitListDatas();
        }

        private void NodeMark_Click(object sender, RoutedEventArgs e)
        {
            SprayViewModel.InsertNodeMark();
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThSpraySystemCmd(viewModel))
            {
                cmd.Execute();
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone();
            var systemSet = new UiUNDSpraySystemSet(viewModel.SetViewModel);
            systemSet.Owner = this;
            var ret = systemSet.ShowDialog();
            if (ret == false)
            {
                //用户取消了操作
                viewModel.SetViewModel = oldViewModel;
                return;
            }
        }
    }
}
