using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Forms;
using System.ComponentModel;
using ThControlLibraryWPF.CustomControl;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.UI.UserContorls;

namespace TianHua.Electrical.PDS.UI.UI
{
    /// <summary>
    /// ElecSandboxUI.xaml 的交互逻辑
    /// </summary>
    public partial class ElecSandboxUI : ThCustomWindow
    {
        private SandBoxTableItemViewModel topTableItemViewModel;
        public ElecSandboxUI()
        {
            InitializeComponent();
            this.Loaded += ElecSandboxUI_Loaded;
            this.Closing += ElecSandboxUI_Closing;
        }

        private void ElecSandboxUI_Loaded(object sender, RoutedEventArgs e)
        {
            InitTopTableItem();
        }

        private void ElecSandboxUI_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        /// <summary>
        /// 加载项目文件
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void LoadProject()
        {
            ThPDSProjectGraphService.CreateNewProject();
        }

        #region 初始化信息
        private void InitTopTableItem()
        {
            topTableItemViewModel = new SandBoxTableItemViewModel();
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("配电沙盘界面", new ThPDSDistributionSandPanel()));
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("单线图绘制界面", new ThPDSSingleLineImageDrawingPanel()));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("配电箱编辑界面", new ThPDSDistributionPanel()));
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("干线编辑界面", new ThPDSMainBusPanel()));
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("低压柜编辑界面", new ThPDSLowPressurePanel()));
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("高压压柜编辑界面", new ThPDSHighPressurePanel()));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("全局参数设置界面", new UESandboxParameter()));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("信息匹配查看器", new ThPDSInfoCompare()));
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("成果导出界面", new ThPDSExport()));
            tabTopFunction.DataContext = topTableItemViewModel;
            tabTopFunction.SelectedIndex = 1;
        }
        #endregion

        #region 界面顶部按钮响应事件
        private void btnNewProject_Click(object sender, RoutedEventArgs e)
        {
            ThPDSProjectGraphService.CreateNewProject();
        }
        private void btnOpenProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Project (.PDSProject)|*.pdsProject"; // Filter files by extension
            dlg.DefaultExt = ".PDSProject"; // Default file extension
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ThPDSProjectGraphService.ImportProject(dlg.FileName);
            }
        }
        private void btnSaveProject_Click(object sender, RoutedEventArgs e)
        {
            //选择路径
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Project"; // Default file name
            dlg.DefaultExt = ".PDSProject"; // Default file extension
            dlg.Filter = "Project (.PDSProject)|*.pdsProject"; // Filter files by extension
            bool? result = dlg.ShowDialog();
            // Process save file dialog box results
            if (result == true)
            {
                var filePathUrl = dlg.FileName.Substring(0, dlg.FileName.LastIndexOf("\\"));
                var fileName = dlg.SafeFileName;
                ThPDSProjectGraphService.ExportProject(filePathUrl, fileName);
            }
            else
            {
                return;
            }
        }
        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            new ThPDSSetting() { Width = 400, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterScreen, }.ShowDialog();
        }
        private void btnImportProject_Click(object sender, RoutedEventArgs e)
        {
        }
        private void btnExportProject_Click(object sender, RoutedEventArgs e)
        {
        }
        private void btnOpenHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/a8f09d07113b4239bde5a4d465277860.html?m=1&view=1");
        }
        private void btnSaveProject_Click(object sender, ExecutedRoutedEventArgs e)
        {
        }
        #endregion
    }
}