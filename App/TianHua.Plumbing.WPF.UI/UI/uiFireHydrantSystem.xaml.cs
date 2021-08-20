using AcHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;
using TianHua.Plumbing.WPF.UI.UI;

namespace ThMEPWSS.UndergroundFireHydrantSystem.UI
{
    /// <summary>
    /// uiFireHydrantSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiFireHydrantSystem : ThCustomWindow
    {
        public static FireHydrantSystemViewModel viewModel;
        public uiFireHydrantSystem()
        {
            InitializeComponent();
            if (null == viewModel)
                viewModel = new FireHydrantSystemViewModel();
            this.DataContext = viewModel;
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
        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone();
            uiFireHydrantSystemSet systemSet = new uiFireHydrantSystemSet(viewModel.SetViewModel);
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
