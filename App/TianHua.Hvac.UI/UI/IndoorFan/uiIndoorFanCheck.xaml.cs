using AcHelper;
using AcHelper.Commands;
using System;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.ParameterService;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    /// <summary>
    /// uiIndoorFanCheck.xaml 的交互逻辑
    /// </summary>
    public partial class uiIndoorFanCheck : ThCustomWindow
    {
        static IndoorFanCheckViewModel checkViewModel;
        public uiIndoorFanCheck()
        {
            InitializeComponent();
            this.MutexName = "THSNJJH";
            if (null == checkViewModel)
            {
                checkViewModel = new IndoorFanCheckViewModel();
            }
            this.DataContext = checkViewModel;
        }

        private void CheckLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FormUtil.DisableForm(gridForm);
                //设置参数，发送命令
                IndoorFanParameter.Instance.CheckModel = checkViewModel.indoorFanCheck;
                CommandHandlerBase.ExecuteFromCommandLine(false, "THSNJJH");
                FocusToCAD();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FormUtil.EnableForm(gridForm);
            }
        }
        private void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
