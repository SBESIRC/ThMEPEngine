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
        private void PDSSet_Click(object sender, RoutedEventArgs e)
        {
            if (null == viewModel)
            {
                MessageBox.Show("数据错误：获取选中住户分区失败，无法进行后续操作");
                return;
            }
            var systemSet = new uiUNDPDrainageSystemSet(viewModel);
            var ret = systemSet.ShowDialog();
            if (ret == false)
            {
                //用户取消了操作
                return;
            }
            //用户确认，进行后续的业务逻辑
            //step1 保存用户的输入信息
        }
        private void PDSSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateFloorFraming();
        }
        private void PDSReadStoreys_Click(object sender, RoutedEventArgs e)
        {
            viewModel.InitListDatas();
        }
        private void ImageButton_Click(object sender, RoutedEventArgs e)
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
                    //用户取消了操作
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

                cmd.Execute();
            }
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
