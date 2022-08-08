using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPLighting.ServiceModels;
using ThMEPLighting.UI.ViewModels;

namespace ThMEPLighting.UI.UI
{
    /// <summary>
    /// uiEvaIndicatorSign.xaml 的交互逻辑
    /// </summary>
    public partial class uiEvaIndicatorSign : ThCustomWindow
    {
        private static EvaSignViewModel signViewModel =null;
        List<string> layerNames = new List<string>();
        /// <summary>
        /// 0 疏散路径;（1,2 布置灯具）1 优先壁装 2优先吊装
        /// </summary>
        public int commondType = 0;
        public uiEvaIndicatorSign()
        {
            InitializeComponent();
            if (null == signViewModel)
                signViewModel = new EvaSignViewModel();
            //这里先调用Commond所在dll,不然后面直接调用命令会不存在
            var value = ThEmgLightService.Instance.MaxLightSpace;
            this.DataContext = signViewModel;

            layerNames.Add(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);
            layerNames.Add(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
            layerNames.Add(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
            layerNames.Add(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);

            checkFEIHide.IsChecked = true;
        }
        private void btnStartLayout_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;
            if (!CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作","天华-提醒",MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }
            SetValueToService();
            commondType = signViewModel.LightLayoutType == LayoutTypeEnum.WallLayout?1:2;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THDSSSZSDBZ");
            FocusToCAD();
        }
        private void btnLayoutUnderGround_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;
            if (!CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SetValueToService();
            commondType = signViewModel.LightLayoutType == LayoutTypeEnum.WallLayout ? 1 : 2;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THSSZSDBZ");
            FocusToCAD();
        }
        private bool CheckInputData() 
        {
            //获取该页面中的Textbox进行验证是否有输入不正确的数据
            var allTextBox = FindControlUtil.GetChildObjects<TextBox>(this, "").ToList();
            List<string> errorMsgs = new List<string>();
            foreach (var textBox in allTextBox) 
            {
                var errors = Validation.GetErrors(textBox);
                if (errors == null || errors.Count < 1)
                    continue;
                foreach (var error in errors) 
                {
                    var errorStr = error.ErrorContent.ToString();
                    if (string.IsNullOrEmpty(errorStr))
                        continue;
                    errorMsgs.Add(errorStr);
                }
            }
            return errorMsgs.Count<1;
        }
        private void btnLayoutExit_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THDSFEL");
            FocusToCAD();
        }
        private void btnLayoutLaneLine_Click(object sender, RoutedEventArgs e)
        {
            //commondType = 0;
            //this.DialogResult = true;
            if (Active.Document == null)
                return;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THSSLJ");
            FocusToCAD();
        }
        private void SetValueToService() 
        {
            var space = (double)signViewModel.ParallelSelectItem.Tag;
            ThEmgLightService.Instance.MaxLightSpace = space * 1000;
            ThEmgLightService.Instance.IsHostingLight = signViewModel.LightLayoutType == LayoutTypeEnum.HostingLayout;
            ThEmgLightService.Instance.BlockScale = signViewModel.BlockSacleSelectItem.Value;
        }

        private void checkFEIHide_Checked(object sender, RoutedEventArgs e)
        {
            HideLayer(false);
        }

        private void checkFEIHide_Unchecked(object sender, RoutedEventArgs e)
        {
            HideLayer(true);
        }
        /// <summary>
        /// 图层的显隐
        /// </summary>
        /// <param name="hideLayer"></param>
        /// <param name="isHide"></param>
        void HideLayer(bool isHide)
        {
            if (Active.Document == null)
                return;
            using (Active.Document.LockDocument())
            using (var db = AcadDatabase.Active())
            {
                FocusToCAD();
                foreach (var layer in db.Layers)
                {
                    if (!layerNames.Any(c => c.Equals(layer.Name)))
                        continue;
                    layer.UpgradeOpen();
                    layer.IsLocked = false;
                    layer.IsOff = isHide;
                    layer.DowngradeOpen();
                }
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

        private void btnVideoHelper_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/view/video/9c0675925da740d8a5f4b386a980bff1.html?m=1&view=1");
        }
    }

    public class LayoutToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LayoutTypeEnum s = (LayoutTypeEnum)value;
            return s == (LayoutTypeEnum)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (LayoutTypeEnum)int.Parse(parameter.ToString());
        }
    }
}
