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

                if(commondType==1 && viewModel.SelectCheckFiles == null || viewModel.SelectCheckFiles.Count == 0)
                {
                    MessageBox.Show("数据错误：未获取到至少一张要统计的图纸，无法进行后续操作,请重新选择", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DiagramGenerationType = DistinguishByFireCompartment.IsChecked.Value ? 1 : 2;

                FireCompartmentParameter.LayerNames = SelectLayers;
                FireCompartmentParameter.ChoiseFileNames = SelectFileNames;
                FireCompartmentParameter.ControlBusCount = int.Parse(ControlBusCountTXT.Text);
                FireCompartmentParameter.FireBroadcastingCount = int.Parse(FireBroadcastingTxt.Text);
                FireCompartmentParameter.ShortCircuitIsolatorCount = int.Parse(ShortCircuitIsolatorTxt.Text);
                FireCompartmentParameter.FixedPartType = PublicSectionType;
                FireCompartmentParameter.SystemDiagramGenerationType = DiagramGenerationType;

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
            if(viewModel.AddLayer(layerName))
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
            else if(SelectF.IsChecked.Value)
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
    }
}
