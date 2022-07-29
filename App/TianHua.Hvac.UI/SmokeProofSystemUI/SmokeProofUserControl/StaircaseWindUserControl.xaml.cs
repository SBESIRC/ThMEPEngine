using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ThMEPHVAC;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofUserControl
{
    /// <summary>
    /// StaircaseWindUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class StaircaseWindUserControl : UserControl
    {
        public StaircaseWindUserControl()
        {
            InitData();
            this.DataContext = ThMEPHVACStaticService.Instance.staircaseWindViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void InitData()
        {
            if (ThMEPHVACStaticService.Instance.staircaseWindViewModel == null)
            {
                ThMEPHVACStaticService.Instance.staircaseWindViewModel = new StaircaseWindViewModel();
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.ListTabControl = new ObservableCollection<TabControlInfo>()
                {
                    new TabControlInfo()
                    {
                        FloorName = "楼层一",
                    },
                    new TabControlInfo()
                    {
                        FloorName = "楼层二",
                    },
                    new TabControlInfo()
                    {
                        FloorName = "楼层三",
                    },
                };
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.SelectTabControlIndex = 0;
            }
            ThMEPHVACStaticService.Instance.staircaseWindViewModel.checkValue = new CheckValue(CheckLjValue);
        }

        /// <summary>
        /// 添加新行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddRowCopy_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.FloorTab.SelectedItem as TabControlInfo;
            sltItem.FloorInfoItems.Add(new FloorInfo(true));
            ThMEPHVACStaticService.Instance.staircaseWindViewModel.RefreshData();
        }

        /// <summary>
        /// 删除行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDelRow_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.FloorTab.SelectedItem as TabControlInfo;
            sltItem.FloorInfoItems.Remove(sltItem.SelectInfoData);
            ThMEPHVACStaticService.Instance.staircaseWindViewModel.RefreshData();
        }

        /// <summary>
        /// 上移行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMoveUpRow_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.FloorTab.SelectedItem as TabControlInfo;
            var index = sltItem.FloorInfoItems.IndexOf(sltItem.SelectInfoData);
            if (sltItem.SelectInfoData != null)
            {
                var sltData = sltItem.SelectInfoData;
                sltItem.FloorInfoItems.Remove(sltItem.SelectInfoData);
                if (index > 0)
                {
                    index = index - 1;
                }
                sltItem.FloorInfoItems.Insert(index, sltData);
            }
            else
            {
                MessageBox.Show("选中某行之后才能进行上移行操作！");
            }
        }

        /// <summary>
        /// 下移行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMoveDownRow_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.FloorTab.SelectedItem as TabControlInfo;
            var count = sltItem.FloorInfoItems.Count - 1;
            var index = sltItem.FloorInfoItems.IndexOf(sltItem.SelectInfoData);
            if (sltItem.SelectInfoData != null)
            {
                var sltData = sltItem.SelectInfoData;
                sltItem.FloorInfoItems.Remove(sltItem.SelectInfoData);
                if (count != index)
                {
                    index = index + 1;
                }
                sltItem.FloorInfoItems.Insert(index, sltData);
            }
            else
            {
                MessageBox.Show("选中某行之后才能进行下移行操作！");
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ThMEPHVACStaticService.Instance.staircaseWindViewModel.RefreshData();
        }

        /// <summary>
        /// 校核Lj数据
        /// </summary>
        /// <param name="minvalue"></param>
        /// <param name="maxvalue"></param>
        private void CheckLjValue(double minvalue, double maxvalue)
        {
            this.txtResult.Text = "";
            if (ThMEPHVACStaticService.Instance.staircaseWindViewModel.FloorType != FloorTypeEnum.lowFloor)
            {
                if (ThMEPHVACStaticService.Instance.staircaseWindViewModel.OverAk > 3.2)
                {
                    if (ThMEPHVACStaticService.Instance.staircaseWindViewModel.LjTotal < minvalue)
                    {
                        this.txtResult.Text = "计算值不满足规范";
                        this.txtResult.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    }
                    else
                    {
                        this.txtResult.Text = "计算值满足规范";
                        this.txtResult.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    }
                }
                else
                {
                    if (ThMEPHVACStaticService.Instance.staircaseWindViewModel.LjTotal < 0.75 * minvalue)
                    {
                        this.txtResult.Text = "计算值不满足规范";
                        this.txtResult.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    }
                    else
                    {
                        this.txtResult.Text = "计算值满足规范";
                        this.txtResult.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                    }

                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
