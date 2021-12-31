using System.Windows;
using System.Windows.Controls;
using ThMEPHVAC.ViewModel;
using ThControlLibraryWPF.CustomControl;
using TianHua.Hvac.UI.Command;

namespace TianHua.Hvac.UI.UI
{
    public partial class uiAirPortParameter : ThCustomWindow
    {
        AirPortParameterVM ViewModel = null;
        public static uiAirPortParameter Instance = null;
        static uiAirPortParameter()
        {
            Instance = new uiAirPortParameter();
        }
        uiAirPortParameter()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new AirPortParameterVM();
            }
            DataContext = ViewModel;
        }

        private void btnInsert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var cmd = new ThHvacCRFKInsertCmd(ViewModel.Parameter))
            {
                cmd.Execute();
            }
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnTotalAirVolume_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.ReadRoomAirVolume();
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void cbAirPortType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.Parameter.AirPortType == "圆形风口")
            {
                this.tbWidth.Visibility = Visibility.Hidden;
                this.lblSizeConnector.Visibility = Visibility.Hidden;
            }
            else
            {
                this.tbWidth.Visibility = Visibility.Visible;
                this.lblSizeConnector.Visibility = Visibility.Visible;
            }
        }
    }
}
