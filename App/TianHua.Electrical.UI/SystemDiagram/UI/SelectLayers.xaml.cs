using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.SystemDiagram.Service;

namespace TianHua.Electrical.UI.SystemDiagram.UI
{
    /// <summary>
    /// SelectLayers.xaml 的交互逻辑
    /// </summary>
    public partial class SelectLayers : ThCustomWindow
    {
        static DrainageLayerViewModel viewModel;
        /// <summary>
        /// 生成系统图方式:1.所有已打开图纸 2.手动选择楼层范围 3.手动选择防火分区
        /// </summary>
        public int commondType = 1;

        /// <summary>
        /// 底部固定部分:1.包含消防室 2.不含消防室 3.仅绘制计数模块
        /// </summary>
        private int PublicSectionType = 1;

        /// <summary>
        /// 系统图生成方式： 
        /// V1.0 按防火分区区分
        /// V2.0 按回路区分
        /// </summary>
        private int DiagramGenerationType = 1;

        /// <summary>
        /// 系统图展示效果： 
        /// 1. 完全展开
        /// 2. 按楼层/防火分区合并
        /// </summary>
        private int DisplayEffect = 1;

        /// <summary>
        /// 是否为每个楼层分组： 
        /// 1. 是
        /// 2. 否
        /// </summary>
        private int CreateGroup = 1;

        public SelectLayers()
        {
            InitializeComponent();
            viewModel = new DrainageLayerViewModel()
            {
                ShortCircuitIsolatorTxt = FireCompartmentParameter.ShortCircuitIsolatorCount,
                FireBroadcastingTxt = FireCompartmentParameter.FireBroadcastingCount,
                ControlBusCountTXT = FireCompartmentParameter.ControlBusCount
            };
            this.DataContext = viewModel;
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ShortCircuitIsolatorTxt.Text)|| string.IsNullOrWhiteSpace(FireBroadcastingTxt.Text) || string.IsNullOrWhiteSpace(ControlBusCountTXT.Text))
            {
                MessageBox.Show("数据错误：计数模块不能为空！", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (null == viewModel || viewModel.SelectCheckBox == null|| viewModel.SelectCheckBox.Count==0)
            {
                MessageBox.Show("数据错误：未获取到至少一个防火分区图层，无法进行后续操作,请重新选择", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                //保存用户的输入信息
                List<string> SelectLayers = new List<string>();
                foreach (var item in viewModel.DynamicCheckBoxs)
                {
                    if (item == null || !item.IsChecked)
                        continue;
                    SelectLayers.Add(item.Content);
                }
                List<string> SelectFileNames = new List<string>();
                foreach (var item in viewModel.DynamicOpenFiles)
                {
                    if (item == null || !item.IsChecked)
                        continue;
                    SelectFileNames.Add(item.Content);
                }
                if (SelectA.IsChecked.Value)
                    commondType = 1;
                else if (SelectF.IsChecked.Value)
                    commondType = 2;
                else
                    commondType = 3;
                if (IncludingFireRoom.IsChecked.Value)
                    PublicSectionType = 1;
                else if (ExcludingFireRoom.IsChecked.Value)
                    PublicSectionType = 2;
                else
                    PublicSectionType = 3;


                if (commondType==1 && viewModel.SelectCheckFiles == null || viewModel.SelectCheckFiles.Count == 0)
                {
                    MessageBox.Show("数据错误：未获取到至少一张要统计的图纸，无法进行后续操作,请重新选择", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DiagramGenerationType = DistinguishByFireCompartment.IsChecked.Value ? 1 : 2;
                DisplayEffect = ShowAllDiagram.IsChecked.Value ? 1 : 2;
                CreateGroup = ConfirmGroup.IsChecked.Value ? 1 : 2;

                FireCompartmentParameter.LayerNames = SelectLayers;
                FireCompartmentParameter.ChoiseFileNames = SelectFileNames;
                FireCompartmentParameter.ControlBusCount = int.Parse(ControlBusCountTXT.Text);
                FireCompartmentParameter.FireBroadcastingCount = int.Parse(FireBroadcastingTxt.Text);
                FireCompartmentParameter.ShortCircuitIsolatorCount = int.Parse(ShortCircuitIsolatorTxt.Text);
                FireCompartmentParameter.FixedPartType = PublicSectionType;
                FireCompartmentParameter.SystemDiagramGenerationType = DiagramGenerationType;
                FireCompartmentParameter.DiagramDisplayEffect = DisplayEffect;
                FireCompartmentParameter.DiagramCreateGroup = CreateGroup;

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("错误：数据转换出错,请注意输入格式", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            string layerName = AddLayerTxt.Text;
            if (viewModel.AddLayer(layerName))
            {
                MessageBox.Show("添加成功！");
            }
            else
            {
                MessageBox.Show("添加失败！请检查输入是否正确！");
            }
            AddLayerTxt.Text = "";
        }

        private void CreationChecked(object sender, RoutedEventArgs e)
        {
            if (DrawingList.IsNull())
                return;
            if (SelectA.IsChecked.Value)
            {
                DrawingList.Visibility = Visibility.Visible;
                DistinguishByCircuit.Visibility = Visibility.Visible;
            }
            else if (SelectF.IsChecked.Value)
            {
                DrawingList.Visibility = Visibility.Collapsed;
                DistinguishByCircuit.Visibility = Visibility.Visible;
            }
            else
            {
                DrawingList.Visibility = Visibility.Collapsed;
                DistinguishByCircuit.Visibility = Visibility.Collapsed;
                DistinguishByFireCompartment.IsChecked = true;
            }
        }

        private void PickLayerButton_Click(object sender, RoutedEventArgs e)
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择Polyline");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Autodesk.AutoCAD.DatabaseServices.Entity entity = acad.Element<Autodesk.AutoCAD.DatabaseServices.Entity>(result.ObjectId);
                if (entity is Autodesk.AutoCAD.DatabaseServices.Polyline polyline)
                {
                    string layerName = polyline.Layer;
                    if (viewModel.AddLayer(layerName))
                    {
                        MessageBox.Show("添加成功！");
                    }
                    else
                    {
                        MessageBox.Show("添加失败！请检查是否重复添加！");
                    }
                    AddLayerTxt.Text = "";
                }
            }
        }

        private void ThCustomWindow_Closed(object sender, EventArgs e)
        {
            FireCompartmentParameter.CacheDynamicCheckBoxs = new List<ThMEPElectrical.SystemDiagram.Model.DynamicCheckBox>();
            foreach (var item in viewModel.DynamicCheckBoxs)
            {
                FireCompartmentParameter.CacheDynamicCheckBoxs.Add(item);
            }
        }

        private void VideoBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var web = "http://thlearning.thape.com.cn/kng/view/video/40d6880863a94ad8b4c6ecf81566acfc.html?m=1&view=1";
                System.Diagnostics.Process.Start(web);
            }
            catch (Exception ex)
            {
                MessageBox.Show("抱歉，出现未知错误\r\n" + ex.Message);
            }
        }
    }
}
