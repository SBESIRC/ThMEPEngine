using AcHelper;
using AcHelper.Commands;
using System.Windows;
using System.Windows.Controls;
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
            InitPropertyViewModel();
        }

        private void InitMainViewModel() 
        {
            mainViewModel = new MainFunctionViewModel();
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("楼层", new StoreyElevationSetUI()));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("轴网", null));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("设计", null));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("组装", null));
            tabTopFunction.DataContext = mainViewModel;
            tabTopFunction.SelectedIndex = 2;
        }

        private void InitPropertyViewModel() 
        {
            propGrid.DataContext = PropertiesViewModel.Instacne;
        }
        #region 页面相应事件
        private void btnPushToSU_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("THSUPush");
        }

        private void btnPushToViewer_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("THDB2Push");
        }
        private void SendCommand(string cmdName) 
        {
            if (Active.Document == null)
                return;
            CommandHandlerBase.ExecuteFromCommandLine(false, cmdName);
        }
        #endregion
    }
}
