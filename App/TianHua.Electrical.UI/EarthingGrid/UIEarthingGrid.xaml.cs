using System.Windows;

namespace TianHua.Electrical.UI.EarthingGrid
{
    public partial class UIEarthingGrid : Window
    {
        private ThEarthingGridVM EarthingGridVM { get; set; }
        public UIEarthingGrid()
        {
            EarthingGridVM= new ThEarthingGridVM();
            InitializeComponent();
            this.DataContext = EarthingGridVM;
        }

        private void btnEarthInnerOutline_Click(object sender, RoutedEventArgs e)
        {
            EarthingGridVM.DrawInnerOutline();
        }

        private void btnEarthOutterOutline_Click(object sender, RoutedEventArgs e)
        {
            EarthingGridVM.DrawOutterOutline();
        }

        private void btnDrawEarthingGrid_Click(object sender, RoutedEventArgs e)
        {
            EarthingGridVM.DrawEarthingGrid();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            EarthingGridVM.ResetCurrentLayer();
        }

        private void NonMerge_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Merge_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void btnShowVideo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"https://short.yunxuetang.cn/W9VQw6k1");
        }
    }
}
