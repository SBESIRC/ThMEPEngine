using System;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using TianHua.Electrical.PDS.UI.UserContorls;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UI
{
    /// <summary>
    /// ElecSandboxUI.xaml 的交互逻辑
    /// </summary>
    public partial class ElecSandboxUI : ThCustomWindow
    {

        SandBoxTableItemViewModel topTableItemViewModel;
        public ElecSandboxUI()
        {
            InitializeComponent();

            MutexName = "ElecSandboxUI";

            InitTopTableItem();
        }

        #region 初始化信息
        private void InitTopTableItem() 
        {
            topTableItemViewModel = new SandBoxTableItemViewModel();
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("单线图绘制界面", new TestUControl("单线图绘制界面")));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("配电箱编辑界面", new TestUControl("配电箱编辑界面")));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("干线编辑界面", new TestUControl("干线编辑界面")));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("低压柜编辑界面", new TestUControl("低压柜编辑界面")));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("高压压柜编辑界面", new TestUControl("高压压柜编辑界面")));

            var setUControl = new UESandboxParameter();
            setUControl.TestEvevnt += new RoutedEventHandler(TestUControlEvent);
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("全局参数设置界面", setUControl));

            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("信息匹配查看器", new TestUControl("信息匹配查看器")));
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("成果导出界面", new TestUControl("成果导出界面")));
            tabTopFunction.DataContext = topTableItemViewModel;
        }
        #endregion

        #region 测试代码
        private void TestUControlEvent(object sender, RoutedEventArgs e) 
        {
        
        }
        #endregion

        #region 界面顶部按钮响应事件
        private void btnNewProject_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void btnOpenProject_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnSaveProject_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnSet_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnImportProject_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnExportProject_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnOpenHelp_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region 提醒
        private void ShowErrorMsg(string message) 
        {
            MessageBox.Show(message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void ShowWarningMsg(string message)
        {
            MessageBox.Show(message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        #endregion

        
    }
}
