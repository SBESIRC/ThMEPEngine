using System.Windows;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    /// <summary>
    /// 柱配筋构件绘制交互逻辑
    /// </summary>
    public partial class ColumnReinforceDrawUI : ThCustomWindow
    {
        private ColumnReinforceDrawVM drawVM = new ColumnReinforceDrawVM();
        public ColumnReinforceDrawUI()
        {
            InitializeComponent();
            DataContext = drawVM;
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

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            drawVM.Select();
            ReSetTable1ItemsSource();
            this.Show();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            drawVM.Clear();
            ReSetTable1ItemsSource();
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            drawVM.Merge();
            ReSetTable1ItemsSource();
        }

        private void ReSetTable1ItemsSource()
        {
            table1.ItemsSource = null;
            table1.ItemsSource = drawVM.Columns;
        }

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            drawVM.Draw();
            this.Show();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            drawVM.RemoveComponentFrames();
        }
    }
}
