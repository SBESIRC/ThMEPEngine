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
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// DrainageSystemUI.xaml 的交互逻辑
    /// </summary>
    public partial class uiDrainageSystem : ThCustomWindow
    {
        static DrainageViewModel viewModel;
        public uiDrainageSystem()
        {
            InitializeComponent();
            if(null == viewModel)
                viewModel = new DrainageViewModel();
            this.DataContext = viewModel;
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            if (null == viewModel || viewModel.SelectRadionButton == null)
            {
                MessageBox.Show("数据错误：获取选中住户分区失败，无法进行后续操作");
                return;
            }
            uiDrainageSystemSet systemSet = new uiDrainageSystemSet(viewModel.SelectRadionButton.Content,viewModel.SelectRadionButton.SetViewModel);
            systemSet.Owner = this;
            var ret= systemSet.ShowDialog();
            if (ret == false)
                //用户取消了操作
                return;
            //用户确认，进行后续的业务逻辑
            //step1 保存用户的输入信息
            foreach (var item in viewModel.DynamicRadioButtons) 
            {
                if (item == null || !item.IsChecked)
                    continue;
                item.SetViewModel = systemSet.setViewModel;
            }
        }

        //run
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThWaterSuplySystemDiagramCmd())
            {
                cmd.Execute();
            }
        }
    }
}
