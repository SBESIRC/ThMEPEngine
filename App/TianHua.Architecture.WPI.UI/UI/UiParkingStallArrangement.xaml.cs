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
using ThControlLibraryWPF.CustomControl;
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ParkingStallArrangement;
using ThMEPArchitecture.ViewModel;

namespace TianHua.Architecture.WPI.UI.UI
{
    /// <summary>
    /// UiParkingStallArrangement.xaml 的交互逻辑
    /// </summary>
    public partial class UiParkingStallArrangement : ThCustomWindow
    {
        static ParkingStallArrangementViewModel _ViewModel = null;
        public UiParkingStallArrangement()
        {
            if(_ViewModel == null)
            {
                _ViewModel = new ParkingStallArrangementViewModel();
            }
            DataContext = _ViewModel;
            InitializeComponent();
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (_ViewModel == null) return;

            if (_ViewModel.CommandType == CommandTypeEnum.RunWithoutIteration)
            {
                if (_ViewModel.UseMultiProcess)
                {
                    using (var cmd = new ThMPArrangementCmd(_ViewModel))
                    {
                        cmd.Execute();
                    }
                }
                else
                {
                    using (var cmd = new GenerateParkingStallDirectlyCmd(_ViewModel))
                    {
                        cmd.Execute();
                    }
                }
            }
            else if(_ViewModel.CommandType == CommandTypeEnum.RunWithIteration)
            {
                if (_ViewModel.UseMultiProcess)
                {
                    using (var cmd = new ThMPArrangementCmd(_ViewModel))
                    {
                        cmd.Execute();
                    }
                }
                else
                {
                    using (var cmd = new ThParkingStallArrangementCmd(_ViewModel))
                    {
                        cmd.Execute();
                    }
                }
            }
            else
            {
                using (var cmd = new WithoutSegLineCmd(_ViewModel))
                {
                    cmd.Execute();
                }
            }
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
