using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.EQPMFanModelEnums;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    /// <summary>
    /// UIFanVolumeCalc.xaml 的交互逻辑
    /// </summary>
    public partial class UIFanVolumeCalc : ThCustomWindow
    {
        FanVolumeViewModel viewModel;
        public UIFanVolumeCalc(FanVolumeCalcModel model)
        {
            InitializeComponent();
            viewModel = new FanVolumeViewModel(model);
            this.DataContext = viewModel;
        }
        public FanVolumeCalcModel GetNewModel() 
        {
            return viewModel.VolumeCalcModel;
        }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
        }
    }
}
