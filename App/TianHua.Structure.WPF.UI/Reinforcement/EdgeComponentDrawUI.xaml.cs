using System.Windows;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    /// <summary>
    /// 边缘构件绘制交互逻辑
    /// </summary>
    public partial class EdgeComponentDrawUI : ThCustomWindow
    {
        private EdgeComponentDrawVM drawVM;
        public EdgeComponentDrawUI(EdgeComponentDrawVM vm)
        {
            InitializeComponent();
            drawVM = vm;
            DataContext = vm;
        }

        private void imgWallColumnLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            drawVM.SetWallColumnLayer();
            this.Show();
        }

        private void imgTextLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            drawVM.SetTextLayer();
            this.Show();
        }

        private void imgWallLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            drawVM.SetWallLayer();
            this.Show();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            drawVM.Select();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            drawVM.Clear();
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            drawVM.Merge();
        }

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            drawVM.Draw();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
