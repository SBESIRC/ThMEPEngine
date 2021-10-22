using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Assistant;
using ThMEPWSS.Command;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class SetHighlevelNozzleAndSemiPlatformNozzleUI : ThCustomWindow
    {
        readonly SetHighlevelNozzleAndSemiPlatformNozzlesViewModel vm;
        readonly SetHighlevelNozzleAndSemiPlatformNozzlesViewModel _vm;
        public SetHighlevelNozzleAndSemiPlatformNozzleUI(SetHighlevelNozzleAndSemiPlatformNozzlesViewModel vm)
        {
            InitializeComponent();
            Title = "设置高位接管&半平台接管";
            _vm = ObjFac.CloneByJson(vm);
            this.vm = vm;
            this.DataContext = _vm;

            void f() { sp.IsEnabled = _vm.Items.Any(x => x.IsHalfPlatform); }
            foreach (var m in _vm.Items)
            {
                m.PropertyChanged += (s, e) => { f(); };
            }
            f();
        }
        static SetHighlevelNozzleAndSemiPlatformNozzleUI Singleton;
        public static void ShowModelSingletonWindow()
        {
            if (Singleton == null)
            {
                var vm = SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.Singleton;
                var items = vm.Items.ToList();
                vm.Items.Clear();
                for (int i = 1; i <= FireControlSystemDiagramViewModel.Singleton.CountsGeneral; i++)
                {
                    vm.Items.Add(items.FirstOrDefault(x => x.PipeId == i) ?? new SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.Item() { PipeId = i, PipeConnectionType = "低位", IsHalfPlatform = false });
                }
                Singleton = new SetHighlevelNozzleAndSemiPlatformNozzleUI(SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.Singleton) { Topmost = true };
               var w = Singleton;
                w.Closed += (s, e) => { Singleton = null; };
                if (vm.Items.Count <= 4)
                {
                    var col = w.dg.Columns[1];
                    col.Width = new System.Windows.Controls.DataGridLength(col.Width.Value + 15, col.Width.UnitType);
                }
            }
            Singleton.ShowDialog();
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
