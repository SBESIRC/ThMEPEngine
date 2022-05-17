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
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSCreateLoad : Window
    {
        public ThPDSCreateLoad()
        {
            InitializeComponent();
            defaultKV.SelectedItem = "0.38";
            defaultKV.ItemsSource = new List<string>() { "0.38", "0.22" };
            foreach (var fi in typeof(PDSImageSources).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                cbx.Items.Add(new Image() { Source = (ImageSource)fi.GetValue(null) });
            }
        }
        static class PDSImageSources
        {
            static readonly ImageSourceConverter cvt = new();
            public static readonly ImageSource ResidentialDistributionPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Residential Distribution Panel.png"));
            public static readonly ImageSource RollerShutter = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Roller Shutter.png"));
            public static readonly ImageSource Socket = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Socket.png"));
            public static readonly ImageSource ACCharger = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/AC Charger.png"));
            public static readonly ImageSource DCCharger = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/DC Charger.png"));
            public static readonly ImageSource ElectricalControlPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Electrical Control Panel.png"));
            public static readonly ImageSource ElectricalMeterPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Electrical Meter Panel.png"));
            public static readonly ImageSource EmergencyLightingDistributionPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Emergency Lighting Distribution Panel.png"));
            public static readonly ImageSource EmergencyPowerDistributionPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Emergency Power Distribution Panel.png"));
            public static readonly ImageSource FireEmergencyLightingDistributionPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Fire Emergency Lighting Distribution Panel.png"));
            public static readonly ImageSource IsolationSwitchPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Isolation Switch Panel.png"));
            public static readonly ImageSource LightingDistributionPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Lighting Distribution Panel.png"));
            public static readonly ImageSource Luminaire = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Luminaire.png"));
            public static readonly ImageSource Motor = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Motor.png"));
            public static readonly ImageSource PowerDistributionPanel = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Power Distribution Panel.png"));
            public static readonly ImageSource Pump = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Pump.png"));
        }
        private void btnInsert(object sender, RoutedEventArgs e)
        {
            new ThPDSUpdateToDwgService().AddLoadDimension(ThPDSProjectGraphService.CreatNewLoad(/*defaultKV: double.Parse(defaultKV.SelectedItem.ToString()), */defaultLoadID: defaultLoadID.Text, defaultPower: double.Parse(defaultPower.Text), defaultDescription: defaultDescription.Text, defaultFireLoad: defaultFireLoad.IsChecked == true));
            Close();
        }

        private void btnSave(object sender, RoutedEventArgs e)
        {
            Project.PDSProjectVM.Instance.InformationMatchViewModel.Graph.AddVertex(ThPDSProjectGraphService.CreatNewLoad(/*defaultKV: double.Parse(defaultKV.SelectedItem.ToString()), */defaultLoadID: defaultLoadID.Text, defaultPower: double.Parse(defaultPower.Text), defaultDescription: defaultDescription.Text, defaultFireLoad: defaultFireLoad.IsChecked == true));
            Close();
        }
    }
}
