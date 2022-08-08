using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

using AcHelper;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.HydrantLayout.Command;
using ThMEPWSS.HydrantLayout.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// HydrantLayout.xaml 的交互逻辑
    /// </summary>
    public partial class HydrantLayoutUI : ThCustomWindow
    {
        private static ThHydrantViewModel VM = null;

        public HydrantLayoutUI()
        {
            InitializeComponent();
            if (VM == null)
            {
                VM = new ThHydrantViewModel();
            }
            DataContext = VM;
            var value = HydrantLayoutSetting.Instance.LayoutMode;
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            if (VM.CheckHydrant == false && VM.ChechExtinguisher == false)
            {
                MessageBox.Show("请选择布置对象");
                return;
            }

            if (Active.Document == null)
                return;

            Save(VM);

            FocusToCAD();
            using (var cmd = new ThHydrantLayoutCmd())
            {
                cmd.Execute();
            }
        }
        
        public static void Save(ThHydrantViewModel VM)
        {
            if (VM.CheckHydrant == true && VM.ChechExtinguisher == true)
            {//消火栓（0）灭火器（1）两者都考虑（2）
                HydrantLayoutSetting.Instance.LayoutObject = 2;
            }
            else if (VM.CheckHydrant == true)
            {
                HydrantLayoutSetting.Instance.LayoutObject = 0;
            }
            else
            {
                HydrantLayoutSetting.Instance.LayoutObject = 1;
            }

            HydrantLayoutSetting.Instance.LayoutMode = (int)VM.LayoutMode;//一字（0） L字（1） 两者都考虑（2）
            HydrantLayoutSetting.Instance.SearchRadius = VM.SearchRadius;
            HydrantLayoutSetting.Instance.AvoidParking = VM.AvoidParking;
            HydrantLayoutSetting.Instance.LayoutInMiddle = VM.LayoutInMiddle;
            HydrantLayoutSetting.Instance.BlockNameDict = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();

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
            System.Diagnostics.Process.Start(@"http://thlearning.thape.com.cn/kng/view/video/8f0a9111993e4fe1b45294d73dbb02c8.html");
        }
    }

    public class LayoutConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LayoutModeType s = (LayoutModeType)value;
            return s == (LayoutModeType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (LayoutModeType)int.Parse(parameter.ToString());
        }
    }

}
