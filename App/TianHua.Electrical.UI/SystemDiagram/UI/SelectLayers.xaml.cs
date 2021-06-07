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
        /// 生成系统图方式:1.全部图纸 2.手动选择
        /// </summary>
        public int commondType = 1;
        public SelectLayers()
        {
            InitializeComponent();
            if (null == viewModel)
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
                FireCompartmentParameter.LayerNames = SelectLayers;
                FireCompartmentParameter.ControlBusCount = int.Parse(ControlBusCountTXT.Text);
                FireCompartmentParameter.FireBroadcastingCount = int.Parse(FireBroadcastingTxt.Text);
                FireCompartmentParameter.ShortCircuitIsolatorCount = int.Parse(ShortCircuitIsolatorTxt.Text);
                commondType = SelectAll.IsChecked.Value ? 1 : 2;
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
    }
}
