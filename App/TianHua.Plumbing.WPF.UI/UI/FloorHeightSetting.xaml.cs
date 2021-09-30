using ThMEPWSS.ViewModel;
using System.Windows.Media;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using TianHua.Plumbing.WPF.UI.Validations;
using System.Collections.Generic;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Linq;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class FloorHeightSettingWindow : ThCustomWindow
    {
        FloorHeightsViewModel vm;
        FloorHeightsViewModel _vm;

        static FloorHeightSettingWindow Instance;
        public static void ShowModelSingletonWindow()
        {
            if (Instance == null)
            {
                Instance = new FloorHeightSettingWindow(FloorHeightsViewModel.Instance);
                Instance.Topmost = true;
                Instance.Closed += (s, e) => { Instance = null; };
            }
            Instance.ShowDialog();
        }
        public FloorHeightSettingWindow(FloorHeightsViewModel vm)
        {
            InitializeComponent();
            _vm = ObjFac.CloneByJson(vm);
            this.vm = vm;
            this.DataContext = _vm;
            TextChangedEventHandler f = null;

            f = (s, e) =>
            {
                var text = tbx.Text;
                if (string.IsNullOrEmpty(text)) return;
                var newText = Regex.Replace(text, @"[^\d\-,]", "");
                if (newText != text)
                {
                    tbx.TextChanged -= f;
                    try
                    {
                        tbx.Text = newText;
                        tbx.Select(tbx.Text.Length, 0);
                    }
                    finally
                    {
                        tbx.TextChanged += f;
                    }
                }

            };
            tbx.TextChanged += f;
            void update()
            {
                var text = FloorHeightsViewModel.GetValidFloorString(tbx.Text);
                if (text != tbx.Text)
                {
                    tbx.TextChanged -= f;
                    tbx.Text = text;
                    tbx.TextChanged += f;
                }

                var d = _vm.Items.ToDictionary(x => x.Floor, x => x.Height);
                _vm.Items.Clear();
                foreach (var str in text.Split(','))
                {
                    if (string.IsNullOrEmpty(str)) continue;
                    if (!d.TryGetValue(str, out int height)) height = _vm.GeneralFloor;
                    _vm.Items.Add(new FloorHeightsViewModel.Item() { Floor = str, Height = height });
                }
            }
            tbx.LostFocus += (s, e) =>
            {
                update();
            };
            tbx.PreviewKeyDown += (s, e) =>
            {
                if (e.Key is Key.Enter or Key.Return)
                {
                    update();
                    e.Handled = true;
                }
            };
        }
        private void btnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnOk(object sender, RoutedEventArgs e)
        {
            ObjFac.CopyProperties(_vm, vm);
            Close();
        }
    }
}
