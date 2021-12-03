﻿using System;
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
using ThControlLibraryWPF.CustomControl;
using ThMEPLighting.Lighting.ViewModels;
using ThMEPLighting.Lighting.Commands;
using ThMEPEngineCore.Command;
using System.Threading;
using ThMEPLighting.Garage;
using AcHelper.Commands;

namespace TianHua.Lighting.UI
{
    /// <summary>
    /// uiThLighting.xaml 的交互逻辑
    /// </summary>
    public partial class uiThLighting : ThCustomWindow
    {
        static LightingViewModel UIConfigs = null;

        //static uiThLighting()
        //{
        //    var items = UIConfigs.Items;
        //    items.Add(new LightingViewModel.Item() { Text = "全选" });
        //    items.Add(new LightingViewModel.Item() { Text = "AD-SIGN" });
        //}

        public uiThLighting()
        {
            InitializeComponent();
            if (UIConfigs == null)
            {
                UIConfigs = new LightingViewModel();
            }
            DataContext = UIConfigs;
            InitUI();
            //For single form instance
            MutexName = "Mutext_uiThLighting";
        }

        private void InitUI()
        {
            cbSignLightSize.ItemsSource = new string[] { "中型", "大型" };

            SelectionChangedEventHandler f = null;
            int lastSelCount = 0;

            //控制拾取车道线listbox 全选事件
            f = (s, e) =>
            {
                try
                {
                    foreach (LightingViewModel.Item item in lstBox.SelectedItems)
                    {
                        if (item.Text == "全选")
                        {
                            try
                            {
                                lstBox.SelectionChanged -= f;
                                if (lstBox.SelectedItems.Count + 1 == UIConfigs.Items.Count && lstBox.SelectedItems.Count < lastSelCount)
                                {
                                    lstBox.SelectedItems.Remove(item);
                                    return;
                                }
                                lstBox.SelectedItems.Clear();
                                foreach (var m in UIConfigs.Items)
                                {
                                    lstBox.SelectedItems.Add(m);
                                }
                            }
                            finally
                            {
                                lstBox.SelectionChanged += f;
                            }
                            return;
                        }
                    }
                    var lst = UIConfigs.Items.Except(lstBox.SelectedItems.Cast<LightingViewModel.Item>()).ToList();
                    if (lst.Count == 1)
                    {
                        if (lst[0].Text == "全选")
                        {
                            lstBox.SelectedItems.Clear();
                        }
                    }
                }
                finally
                {
                    lastSelCount = lstBox.SelectedItems.Count;
                }
            };
            lstBox.SelectionChanged += f;
            lstBox.PreviewKeyDown += (s, e) => { e.Handled = true; };
        }

        private void btnPlace_Click(object sender, RoutedEventArgs e)
        {
            #region ---------- 后期根据UI调整再删除 ----------
            var button = sender as Button;
            if(button.Name == "btnLayout")
            {
                UIConfigs.LightingLayoutType = LightingLayoutTypeEnum.IlluminationLighting; 
            }
            else if (button.Name == "btnCdzmLayout")
            {
                UIConfigs.LightingLayoutType = LightingLayoutTypeEnum.GarageLighting;
            }
            #endregion

            using (var cmd = new LightingLayoutCommand(UIConfigs))
            {
                FocusToCAD();
                cmd.Execute();
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

        private void btnReGen(object sender, RoutedEventArgs e)
        {
        }

        private void btnDrawLightCenter(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThDXDrawingCmd())
            {
                FocusToCAD();
                cmd.Execute();
            }
        }

        private void btnDrawNonLightCenter(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThFDXDrawingCmd())
            {
                FocusToCAD();
                cmd.Execute();
            }
        }

        private void btnDrawSingleCenter(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThSingleRowCenterDrawingCmd())
            {
                FocusToCAD();
                cmd.Execute();
            }
        }

        private void btnPickUp(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThPickUpLaneLineLayerCmd(UIConfigs))
            {
                FocusToCAD();
                cmd.Execute();
            }
        }

        private void btnCalcPath(object sender, RoutedEventArgs e)
        {

        }

        private void rbSingleRow_Checked(object sender, RoutedEventArgs e)
        {
            UIConfigs.LampSpacing = 2700;

        }

        private void rbDoubleRow_Checked(object sender, RoutedEventArgs e)
        {
            UIConfigs.LampSpacing = 5400;
        }

        private void btnExtractLaneLine_Click(object sender, RoutedEventArgs e)
        {
            FocusToCAD();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THTCD");
        }

        private void rbCableTray_Checked(object sender, RoutedEventArgs e)
        {
            connectModeGroup.IsEnabled = false;
        }

        private void rbCableTray_Unchecked(object sender, RoutedEventArgs e)
        {
            connectModeGroup.IsEnabled = true;
        }
    }
}
