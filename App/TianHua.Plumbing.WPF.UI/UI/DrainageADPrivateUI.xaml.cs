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

using System.Globalization;

using AcHelper;
using ThControlLibraryWPF.CustomControl;

using ThMEPWSS.DrainageADPrivate.Cmd;
using ThMEPWSS.DrainageADPrivate.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// DrainageADPrivateUI.xaml 的交互逻辑
    /// </summary>
    public partial class DrainageADPrivateUI : ThCustomWindow
    {
        private static ThDrainageADPViewModel VM = null;
        public DrainageADPrivateUI()
        {
            InitializeComponent();
            if (VM == null)
            {
                VM = new ThDrainageADPViewModel();
            }
            DataContext = VM;
            var value = ThDrainageADSetting.Instance.qL;
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;

            Save(VM);

            FocusToCAD();
            using (var cmd = new ThDrainageADPCmd())
            {
                cmd.Execute();
            }
        }

        public static void Save(ThDrainageADPViewModel VM)
        {
            ThDrainageADSetting.Instance.qL = VM.qL;
            ThDrainageADSetting.Instance.m = VM.m;
            ThDrainageADSetting.Instance.Kh = (int)VM.Kh;
            ThDrainageADSetting.Instance.BlockNameDict = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();

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


    }
}
