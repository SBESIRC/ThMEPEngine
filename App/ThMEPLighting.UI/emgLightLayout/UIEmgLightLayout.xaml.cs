using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
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
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.UI.emgLightLayout;

namespace ThMEPLighting.UI.emgLightLayout
{
    /// <summary>
    /// UIEmgLightLayout.xaml 的交互逻辑
    /// </summary>
    public partial class UIEmgLightLayout : ThCustomWindow
    {
        private static emgLightLayoutViewModel emgLightVM = null;
        public UIEmgLightLayout()
        {
            InitializeComponent();
            if (emgLightVM == null)
            {
                emgLightVM = new emgLightLayoutViewModel();
            }

            var value = LayoutUISettingService.Instance.scale;

            this.DataContext = emgLightVM;


        }

        private void btnLayoutEmg_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;

            LayoutUISettingService.Instance.scale =  (double)emgLightVM.scaleItem.Tag;
            LayoutUISettingService.Instance.blkType = (int)emgLightVM.blkType;
            LayoutUISettingService.Instance.singleSide = (int)emgLightVM.singleLayout;

            if (LayoutUISettingService.Instance.singleSide == 0)
            {
                //两侧布点
                CommandHandlerBase.ExecuteFromCommandLine(false, "THYJZMSC");
                FocusToCAD();
            }
            else if (LayoutUISettingService.Instance.singleSide == 1)
            {
                //单侧布点
                CommandHandlerBase.ExecuteFromCommandLine(false, "THYJZMDC");
            }
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
    public class SingleLayoutConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SideLayoutEnum s = (SideLayoutEnum)value;
            return s == (SideLayoutEnum)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (SideLayoutEnum)int.Parse(parameter.ToString());
        }
    }
    public class BlkTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BlkTypeEnum s = (BlkTypeEnum)value;
            return s == (BlkTypeEnum)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (BlkTypeEnum)int.Parse(parameter.ToString());
        }
    }

}
