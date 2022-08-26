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
using Tianhua.Platform3D.UI.ViewModels;
using hControl = HandyControl.Controls;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// PlatformMainUI.xaml 的交互逻辑
    /// </summary>
    public partial class PlatformMainUI : UserControl
    {
        MainFunctionViewModel mainViewModel;
        public PlatformMainUI()
        {
            InitializeComponent();
            TabControl test = new hControl.TabControl();
            
            this.Loaded += PlatformMainUI_Loaded;
        }

        private void PlatformMainUI_Loaded(object sender, RoutedEventArgs e)
        {
            InitMainViewModel();
        }

        private void InitMainViewModel() 
        {
            mainViewModel = new MainFunctionViewModel();
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("楼层", null));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("轴网", null));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("设计", null));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("组装", null));
            tabTopFunction.DataContext = mainViewModel;
            tabTopFunction.SelectedIndex = 2;
        }
    }
}
