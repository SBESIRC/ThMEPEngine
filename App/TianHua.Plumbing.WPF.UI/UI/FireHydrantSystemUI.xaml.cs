using AcHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.ViewModel;
using TianHua.Plumbing.WPF.UI.UI;

namespace ThMEPWSS.UndergroundFireHydrantSystem.UI
{
    public partial class FireHydrantSystemUI : ThCustomWindow
    {
        FireHydrantSystemUIViewModel vm;
        public FireHydrantSystemUI(FireHydrantSystemUIViewModel vm)
        {
            InitializeComponent();
            cbxRatio.ItemsSource = new string[] { "1:100", "1:150" };
            {
                void f(object sender, TextChangedEventArgs e)
                {
                    var text = tbxPrefix.Text;
                    var newText = Regex.Replace(text, @"\s+", "");
                    if (newText != text)
                    {
                        tbxPrefix.TextChanged -= f;
                        tbxPrefix.Text = newText;
                        tbxPrefix.TextChanged += f;
                        tbxPrefix.SelectionStart = newText.Length;
                        tbxPrefix.SelectionLength = 0;
                    }
                }
                tbxPrefix.TextChanged += f;
            }
            {
                void f(object sender, TextChangedEventArgs e)
                {
                    var text = tbxStartNum.Text;
                    var newText = Regex.Replace(text, @"\D+", "");
                    if (int.TryParse(newText, out int num))
                    {
                        newText = num.ToString();
                    }
                    else
                    {
                        newText = "";
                    }
                    if (newText != text)
                    {
                        tbxStartNum.TextChanged -= f;
                        tbxStartNum.Text = newText;
                        tbxStartNum.TextChanged += f;
                        tbxStartNum.SelectionStart = newText.Length;
                        tbxStartNum.SelectionLength = 0;
                    }
                }
                tbxStartNum.TextChanged += f;
                tbxStartNum.LostFocus += (s, e) =>
                {
                    var text = tbxStartNum.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        var newText = "1";
                        tbxStartNum.TextChanged -= f;
                        tbxStartNum.Text = newText;
                        tbxStartNum.TextChanged += f;
                        tbxStartNum.SelectionStart = newText.Length;
                        tbxStartNum.SelectionLength = 0;
                    }
                };
            }
            this.DataContext = vm;
            this.vm = vm;
        }
        static FireHydrantSystemUI singleton;
        public static FireHydrantSystemUI TryCreateSingleton()
        {
            if (singleton == null)
            {
                singleton = new FireHydrantSystemUI(FireHydrantSystemUIViewModel.Singleton);
                singleton.Closed += (s, e) => { singleton = null; };
                return singleton;
            }
            return null;
        }
        private void btnLabelRing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                vm.cbLabelRing?.Invoke();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Show();
            }
        }

        private void btnLabelNode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                vm.cbLabelNode?.Invoke();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Show();
            }
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                vm.cbGenerate?.Invoke();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Show();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/view/video/693b4adf25cc42e5b64d0a4c89507bf5.html");
        }
    }
}
