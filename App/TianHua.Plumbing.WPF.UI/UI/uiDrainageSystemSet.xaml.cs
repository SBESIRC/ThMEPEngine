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
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiDrainageSystemSet.xaml 的交互逻辑
    /// </summary>
    public partial class uiDrainageSystemSet : ThCustomWindow
    {
        public DrainageSetViewModel setViewModel;
        public uiDrainageSystemSet(string attrTitle, DrainageSetViewModel viewModel =null)
        {
            InitializeComponent();
            this.Title = "参数设置-" + attrTitle;
            setViewModel = viewModel;
            if (null == viewModel)
                setViewModel = new DrainageSetViewModel();
            this.DataContext = setViewModel;
        }

        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            //输入框数据校验
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
