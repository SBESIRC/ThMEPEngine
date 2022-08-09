using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSSetting : Window
    {
        public ThPDSSetting()
        {
            InitializeComponent();
            Loaded += ThPDSSetting_Loaded;
        }

        Setting setting;
        const string file = "THPDSCONFIG.JSON";
        private void ThPDSSetting_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(file))
                {
                    setting = JsonConvert.DeserializeObject<Setting>(File.ReadAllText(file));
                }
            }
            catch { }
            setting ??= new();
            DataContext = setting;
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(setting.Path))
            {
                try
                {
                    Directory.CreateDirectory(setting.Path);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
            }
            File.WriteAllText(file, JsonConvert.SerializeObject(setting));
            Close();
        }

        private void btnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnBrowseClick(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                setting.Path = dlg.SelectedPath;
            }
        }
        class Setting : ObservableObject
        {
            private string path;
            private int interval = 10;
            private bool individual;
            private string dwgRatio = "1:100";
            public string Path
            {
                get => path;
                set => SetProperty(ref path, value);
            }
            public int Interval
            {
                get => interval;
                set => SetProperty(ref interval, value);
            }
            public bool Individual
            {
                get => individual;
                set => SetProperty(ref individual, value);
            }
            public string DwgRatio
            {
                get => dwgRatio;
                set => SetProperty(ref dwgRatio, value);
            }
            public string[] DwgRatioItemsSouce { get; } = new string[] { "1:100", "1:150" };
        }
    }
}
