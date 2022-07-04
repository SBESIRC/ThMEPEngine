using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSCreateLoad : Window
    {
        public ThPDSCreateLoad()
        {
            InitializeComponent();
            defaultKV.SelectedItem = "0.38";
            defaultKV.ItemsSource = new List<string>() { "0.38", "0.22" };
            defaultPower.Text = "0";
            var array = typeof(PDSImageSources).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            for (int i = 0; i < array.Length; i++)
            {
                var fi = array[i];
                var v = (ImageSource)fi.GetValue(null);
                var img = new Image() { Source = v };
                cbx.Items.Add(img);
                if (i == 0) cbx.SelectedItem = img;
            }
        }
        static class PDSImageSources
        {
            static readonly ImageSourceConverter cvt = new();
            public static readonly ImageSource RD = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Residential Distribution Panel.png"));
            public static readonly ImageSource RS = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Roller Shutter.png"));
            public static readonly ImageSource Socket = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Socket.png"));
            public static readonly ImageSource ACB = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/AC Charger.png"));
            public static readonly ImageSource DC = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/DC Charger.png"));
            public static readonly ImageSource AC = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Electrical Control Panel.png"));
            public static readonly ImageSource AW = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Electrical Meter Panel.png"));
            public static readonly ImageSource ALE = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Emergency Lighting Distribution Panel.png"));
            public static readonly ImageSource APE = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Emergency Power Distribution Panel.png"));
            public static readonly ImageSource FEL = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Fire Emergency Lighting Distribution Panel.png"));
            public static readonly ImageSource INT = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Isolation Switch Panel.png"));
            public static readonly ImageSource AL = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Lighting Distribution Panel.png"));
            public static readonly ImageSource Light = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Luminaire.png"));
            public static readonly ImageSource Motor = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Motor.png"));
            public static readonly ImageSource AP = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Power Distribution Panel.png"));
            public static readonly ImageSource Pump = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Pump.png"));
        }
        ImageLoadType ImageLoadType
        {
            get
            {
                var array = typeof(PDSImageSources).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                for (int i = 0; i < array.Length; i++)
                {
                    var fi = array[i];
                    var v = (ImageSource)fi.GetValue(null);
                    if ((cbx.SelectedItem as Image)?.Source == v)
                    {
                        foreach (ImageLoadType type in Enum.GetValues(typeof(ImageLoadType)))
                        {
                            if (type.ToString() == fi.Name)
                            {
                                return type;
                            }
                        }
                    }
                }
                return ImageLoadType.None;
            }
        }
        private void btnInsert(object sender, RoutedEventArgs e)
        {
            double.TryParse(defaultPower.Text, out var v);
            new ThPDSUpdateToDwgService().AddLoadDimension(ThPDSProjectGraphService.CreatNewLoad(/*defaultKV: double.Parse(defaultKV.SelectedItem.ToString()), */defaultLoadID: defaultLoadID.Text, defaultPower: v /*defaultPower: double.Parse(defaultPower.Text)*/, defaultDescription: defaultDescription.Text, defaultFireLoad: defaultFireLoad.IsChecked == true, imageLoadType: ImageLoadType));
            WeakReferenceMessenger.Default.Send(new GraphNodeAddMessage("btnInsert Click"));
            Close();
        }
        private void btnSave(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(defaultPower.Text, out var v))
            {
                ThPDSProjectGraphService.CreatNewLoad(
                    defaultLoadID: defaultLoadID.Text,
                    defaultPower: v,
                    defaultDescription: defaultDescription.Text,
                    defaultFireLoad: defaultFireLoad.IsChecked == true,
                    imageLoadType: ImageLoadType);
                WeakReferenceMessenger.Default.Send(new GraphNodeAddMessage("btnSave Click"));
            }
            Close();
        }
    }
}
