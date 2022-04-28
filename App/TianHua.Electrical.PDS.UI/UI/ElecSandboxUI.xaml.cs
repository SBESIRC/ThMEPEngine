using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using QuikGraph;
using QuikGraph.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Forms;
using ThControlLibraryWPF.CustomControl;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Project;
using TianHua.Electrical.PDS.UI.UserContorls;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UI
{
    /// <summary>
    /// ElecSandboxUI.xaml 的交互逻辑
    /// </summary>
    public partial class ElecSandboxUI : ThCustomWindow
    {
        public static ElecSandboxUI singleton;
        SandBoxTableItemViewModel topTableItemViewModel;
        public ElecSandboxUI()
        {
            InitializeComponent();
            this.Loaded += ElecSandboxUI_Loaded;
        }
        public static ElecSandboxUI TryGetCurrentWindow()
        {
            return singleton;
        }
        public static ElecSandboxUI TryCreateSingleton()
        {
            if (singleton == null)
            {
                singleton = new ElecSandboxUI();
                singleton.Closed += Singleton_Closed;
                return singleton;
            }
            return null;
        }

        private static void Singleton_Closed(object sender, EventArgs e)
        {
            singleton = null;
        }

        private void ElecSandboxUI_Loaded(object sender, RoutedEventArgs e)
        {
            InitTopTableItem();
        }

        static bool hasInited;

        /// <summary>
        /// 初始化数据，只执行一次！
        /// </summary>
        public static void InitPDSProjectData()
        {
            if (hasInited) return;
            LoadProject();
            hasInited = true;
        }

        /// <summary>
        /// 加载项目文件
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private static void LoadProject(string url = null)
        {
            PDSProject.Instance.DataChanged -= PDSProjectVM.Instance.ProjectDataChanged;
            //订阅Project数据改变事件
            PDSProject.Instance.DataChanged += PDSProjectVM.Instance.ProjectDataChanged;

            //Setp 1
            //加载项目
            PDSProject.Instance.Load(url);
            //Setp 2
            //刷新所有UI的DataContext
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
            var dft = new Models.UTableItem("全局参数设置界面", new UESandboxParameter());
            topTableItemViewModel.FunctionTableItems.Add(dft);
            topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("信息匹配查看器", new ThPDSInfoCompare()));
            //topTableItemViewModel.FunctionTableItems.Add(new Models.UTableItem("成果导出界面", new ThPDSExport()));
            tabTopFunction.DataContext = topTableItemViewModel;
            tabTopFunction.SelectedItem = dft;
            if(tabTopFunction.SelectedItem is null)
            {
                tabTopFunction.SelectedIndex = topTableItemViewModel.FunctionTableItems.IndexOf(dft);
            }
        }
        #endregion

        #region 界面顶部按钮响应事件
        private void btnNewProject_Click(object sender, RoutedEventArgs e)
        {
            ThPDSProjectGraphService.ImportProject("");
        }
        private void btnOpenProject_Click(object sender, RoutedEventArgs e)
        {
            //ImportProject
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
            new ThPDSSetting() { Width = 400, Height = 200 }.ShowDialog();
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

        private void btnSaveProject_Click(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {

        }
    }
}