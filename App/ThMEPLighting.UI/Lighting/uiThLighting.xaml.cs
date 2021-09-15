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
using ThControlLibraryWPF.CustomControl;
using ThMEPLighting.Lighting.ViewModels;
using ThMEPLighting.Lighting.Commands;
using ThMEPEngineCore.Command;
using System.Threading;

namespace TianHua.Lighting.UI
{
    /// <summary>
    /// uiThLighting.xaml 的交互逻辑
    /// </summary>
    public partial class uiThLighting : ThCustomWindow
    {
        public static readonly LightingViewModel UIConfigs = new LightingViewModel();
        static uiThLighting()
        {
            var items = UIConfigs.Items;
            items.Add(new LightingViewModel.Item() { Text = "全选" });
            items.Add(new LightingViewModel.Item() { Text = "AD-SIGN" });
        }

        public uiThLighting()
        {
            InitializeComponent();
            InitUI();
            //For single form instance
            MutexName = "Mutext_uiThLighting";
        }

        private void InitUI()
        {
            cbRatio.ItemsSource = new string[] { "1:100" };
            cbSignLightSize.ItemsSource = new string[] { "中型", "大型" };
            DataContext = UIConfigs;
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
                }
                finally
                {
                    lastSelCount = lstBox.SelectedItems.Count;
                }

                {
                    var lst = UIConfigs.Items.Except(lstBox.SelectedItems.Cast<LightingViewModel.Item>()).ToList();
                    if (lst.Count == 1)
                    {
                        if (lst[0].Text == "全选")
                        {
                            lstBox.SelectedItems.Clear();
                        }
                    }
                }

            };
            lstBox.SelectionChanged += f;
            lstBox.PreviewKeyDown += (s, e) => { e.Handled = true; };
        }

        private void btnPlace_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new LightingLayoutCommand(UIConfigs))
            {
                cmd.Execute();
            }
        }
        private void btnRouting_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new LightingRouteCableCommand(UIConfigs))
            {
                cmd.Execute();
            }
        }
      

        private void btnReGen(object sender, RoutedEventArgs e)
        {

        }

        private void btnDrawLightCenter(object sender, RoutedEventArgs e)
        {

        }

        private void btnDrawNonLightCenter(object sender, RoutedEventArgs e)
        {

        }

        private void btnDrawSingleCenter(object sender, RoutedEventArgs e)
        {

        }

        private void btnPickUp(object sender, RoutedEventArgs e)
        {

        }

        private void btnCalcPath(object sender, RoutedEventArgs e)
        {

        }

        
    }
}
