using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using ThMEPArchitecture;
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
        public static string Version = "3.3";
        public UiParkingStallArrangement()
        {
            if (_ViewModel == null)
            {
                _ViewModel = new ParkingStallArrangementViewModel();
            }
            if (ParameterStock.ReadHiddenParameter)
            {
                _ViewModel.AdvancedSettingVisibility = Visibility.Visible;
            }
            else
            {
                _ViewModel.AdvancedSettingVisibility = Visibility.Collapsed;
            }
            DataContext = _ViewModel;
            InitializeComponent();
        }
        //private void DoWork()
        //{
        //    ThMEPArchitecture.ParkingStallArrangement.Algorithm.
        //    ProcessForDisplay.CreateSubProcess().Start();
        //}

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (_ViewModel == null) return;
            //Dispatcher.Invoke(() =>
            //                {
            //                    Thread thread = new Thread(DoWork);
            //                    thread.SetApartmentState(ApartmentState.STA);
            //                    thread.IsBackground = true;
            //                    thread.Start();
            //                });
            if (_ViewModel.ObliqueMode)
            {
                using (var cmd = new ThOArrangementCmd(_ViewModel))
                {
                    cmd.Execute();
                    return;
                }
            }

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
            else if (_ViewModel.CommandType == CommandTypeEnum.RunWithIteration)
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

                if (_ViewModel.UseMultiProcess)
                {
                    using (var cmd = new ThMPArrangementCmd(_ViewModel))
                    {
                        cmd.Execute();
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
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {

        }

        private void preprocess_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThParkingStallPreprocessCmd())
            {
                cmd.Execute();
            }
        }
        private void oIteration_Click(object sender, RoutedEventArgs e)
        {
            _ViewModel.CommandType = CommandTypeEnum.RunWithIteration;
            using (var cmd = new ThOArrangementCmd(_ViewModel))
            {
                cmd.Execute();
            }
        }
    }
}
