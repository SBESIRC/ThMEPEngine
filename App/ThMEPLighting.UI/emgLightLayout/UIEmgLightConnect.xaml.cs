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

using AcHelper;
using AcHelper.Commands;
using Linq2Acad;

using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPLighting.EmgLightConnect.Service;

namespace ThMEPLighting.UI.emgLightLayout
{
    /// <summary>
    /// UIEmgLightConnect.xaml 的交互逻辑
    /// </summary>
    public partial class UIEmgLightConnect : ThCustomWindow
    {
        private static emgLightConnectViewModel emgLightConnectVM = null;

        public UIEmgLightConnect()
        {
            InitializeComponent();
            if (emgLightConnectVM == null)
            {
                emgLightConnectVM = new emgLightConnectViewModel();
            }

            var value = ConnectUISettingService.Instance.groupMin;

            this.DataContext = emgLightConnectVM;
        }

        private void btnConnectEmg_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;

            ConnectUISettingService.Instance.groupMin = emgLightConnectVM.groupMin;
            ConnectUISettingService.Instance.groupMax = emgLightConnectVM.groupMax;

            var errorList = CheckGroupMinMax();
            if (null != errorList && errorList.Count > 0)
            {
                string showMsg = "";
                errorList.ForEach(c => showMsg += c + "\n");
                MessageBox.Show(showMsg, "天华-警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            CommandHandlerBase.ExecuteFromCommandLine(false, "THYJZMLX");

            FocusToCAD();

        }

        private List<string> CheckGroupMinMax()
        {
            var errorMsgs = new List<string>();

            var groupMin = emgLightConnectVM.groupMin;
            var groupMax = emgLightConnectVM.groupMax;

            if (groupMin == 0)
            {
                errorMsgs.Add(string.Format("灯组最小数不能等于0"));
            }
            if (groupMax == 0)
            {
                errorMsgs.Add(string.Format("灯组最大数不能等于0"));
            }

            if (groupMin >= groupMax)
            {
                errorMsgs.Add(string.Format("灯组最小数不能大于等于灯组最大数"));
            }
            return errorMsgs;
        }



        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://thlearning.thape.com.cn/kng/view/video/9c0675925da740d8a5f4b386a980bff1.html");
        }
    }

}
