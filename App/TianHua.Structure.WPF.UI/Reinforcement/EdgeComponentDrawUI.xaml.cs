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
            this.Hide();
            drawVM.Select();
            ReSetTable1ItemsSource();
            this.Show();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            drawVM.ClearTable();
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
            table1.ItemsSource = drawVM.EdgeComponents;
        }

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            if (drawVM.IsNeedMerge)
            {
                MessageBox.Show("请先进行归并操作！");
                return;
            }

            if (drawVM.EdgeComponents.Count==0)
            {
                MessageBox.Show("没有要绘制的记录，请点击【选择】按钮进行识别！");
                return;
            }            
            else
            {
                this.Hide();
                drawVM.Draw();
                this.Close();
            }
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
