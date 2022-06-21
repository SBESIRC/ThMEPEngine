using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiUNDPressureDrainageSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiUNDPressureDrainageSystem : ThCustomWindow
    {
        static PressureDrainageSystemDiagramVieModel viewModel;
        public uiUNDPressureDrainageSystem()
        {
            InitializeComponent();
            if (null == viewModel)
                viewModel = new PressureDrainageSystemDiagramVieModel();
            this.DataContext = viewModel;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        /// <summary>
        /// 楼层线间距参数设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetParameter_Click(object sender, RoutedEventArgs e)
        {
            var systemSet = new uiUNDPDrainageSystemSet(viewModel);
            systemSet.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var ret = systemSet.ShowDialog();
            if (ret == false) return;
        }
        /// <summary>
        /// 楼层框定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PDSSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateFloorFraming();
        }
        /// <summary>
        /// 读取楼层信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PDSReadStoreys_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.InitListDatas();
            }
            catch { }
            try
            {
                ReadPumpWellKeyBlockNames();
            }
            catch { }
        }

        /// <summary>
        /// 从图块配置内存中读取集水井数据
        /// </summary>
        private void ReadPumpWellKeyBlockNames()
        {
            var config = uiBlockNameConfig.staticUIBlockName.GetBlockNameList(); 
            List<string> wellnames = new List<string>();
            foreach (var fig in config)
            {
                if (fig.Key == "集水井")
                {
                    foreach (var value in fig.Value)
                    {
                        bool quit = false;
                        for (int i = 0; i < wellnames.Count; i++)
                        {
                            if (wellnames.Equals(value))
                            {
                                quit = true;
                                break;
                            }
                        }
                        if (quit)
                            continue;
                        else
                            wellnames.Add(value);
                    }
                }
            }
            wellnames.ForEach(e => viewModel.WellBlockKeyNames.Add(e));
        }

        /// <summary>
        /// 生成系统图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateDiagramButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThUNDPDrainageSystemDiagramCmd(viewModel))
            {
                if (null == viewModel)
                {
                    System.Windows.Forms.MessageBox.Show("数据错误：获取选中住户分区失败，无法进行后续操作");
                    return;
                }
                uiUNDPDrainageSystemInfoCheck infoCheck = new uiUNDPDrainageSystemInfoCheck();
                var ret = infoCheck.ShowDialog();
                if (ret == false)
                {
                    return;
                }
                Point3d insertPt = new Point3d();
                var pt = Active.Editor.GetPoint("\n请输入插入点");
                if (pt.Status == PromptStatus.OK)
                {
                    insertPt = pt.Value;
                    cmd.InsertPt = insertPt;
                }
                else
                {
                    return;
                }
                if (uiUNDPDrainageSystemInfoCheck.viewmodel.HasInfoTablesRoRead)
                {
                    viewModel.HasInfoTablesRoRead = true;
                    viewModel.InfoRegion = uiUNDPDrainageSystemInfoCheck.viewmodel.InfoRegion;
                }
                viewModel.PreGenerateDiagram(cmd);
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}