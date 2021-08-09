using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;

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
        }
        /// <summary>
        /// 楼层线间距参数设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetParameter_Click(object sender, RoutedEventArgs e)
        {
            var systemSet = new uiUNDPDrainageSystemSet(viewModel);
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
                    MessageBox.Show("数据错误：获取选中住户分区失败，无法进行后续操作");
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
                }
                viewModel.PreGenerateDiagram(cmd);
            }
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}