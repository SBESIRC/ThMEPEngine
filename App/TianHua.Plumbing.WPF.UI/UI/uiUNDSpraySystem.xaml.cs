using Autodesk.AutoCAD.EditorInput;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.UndergroundSpraySystem.ViewModel;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using AcHelper;
using ThMEPWSS.UndergroundSpraySystem.Command;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiUNDSpraySystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiUNDSpraySystem : ThCustomWindow
    {
        static SprayVM viewModel;
        public uiUNDSpraySystem()
        {
            InitializeComponent();
            if (null == viewModel)
                viewModel = new SprayVM();
            this.DataContext = viewModel;
        }

        private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateFloorFraming();
        }

        private void btnReadStoreys_Click(object sender, RoutedEventArgs e)
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

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone();
            var systemSet = new uiUNDSpraySystemSet(viewModel.SetViewModel);
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
