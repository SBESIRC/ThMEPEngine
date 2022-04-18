using System.Collections.Generic;
using System.Windows;
using ThControlLibraryWPF;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    /// <summary>
    /// UIFanModel.xaml 的交互逻辑
    /// </summary>
    public partial class UIFanModel : ThCustomWindow
    {
        FanModelSelectViewModel viewModel;
        public UIFanModel(FanDataModel pModel, FanDataModel cModel,FanModelPicker mainPicker, List<FanModelPicker> canUseFanModelPickers)
        {
            InitializeComponent();
            viewModel = new FanModelSelectViewModel(pModel, cModel, mainPicker, canUseFanModelPickers);
            if (null != cModel) 
            {
                //双速，有子风机
            }
            this.DataContext = viewModel;
        }
        public CalcFanModel GetNewFanModel(out CalcFanModel childFan) 
        {
            childFan = viewModel.ChildCalcFan;
            return viewModel.MainCalcFan;
        }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
