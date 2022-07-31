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
    /// SeparateOrSharedNaturalUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SeparateOrSharedNaturalUserControl : UserControl
    {
        public SeparateOrSharedNaturalUserControl()
        {
            InitData();
            this.DataContext = ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void InitData()
        {
            if (ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel == null)
            {
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel = new SeparateOrSharedNaturalViewModel();
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FrontRoomTabControl = new ObservableCollection<TabControlInfo>()
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
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.StairRoomTabControl = new ObservableCollection<TabControlInfo>()
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
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FrontRoomTabControlIndex = 0;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.StairRoomTabControlIndex = 0;
            }
            ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.checkValue = new CheckValue(CheckLjValue);
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
            ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.RefreshData();
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
            ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.RefreshData();
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

        /// <summary>
        /// 添加新行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddStairRowCopy_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.StairTab.SelectedItem as TabControlInfo;
            sltItem.FloorInfoItems.Add(new FloorInfo(true));
            ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.RefreshData();
        }

        /// <summary>
        /// 删除行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDelStairRow_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.StairTab.SelectedItem as TabControlInfo;
            sltItem.FloorInfoItems.Remove(sltItem.SelectInfoData);
            ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.RefreshData();
        }

        /// <summary>
        /// 上移行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMoveUpStairRow_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.StairTab.SelectedItem as TabControlInfo;
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
        private void btnMoveDownStairRow_Click(object sender, RoutedEventArgs e)
        {
            var sltItem = this.StairTab.SelectedItem as TabControlInfo;
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
            ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.RefreshData();
        }

        /// <summary>
        /// 校核Lj数据
        /// </summary>
        /// <param name="minvalue"></param>
        /// <param name="maxvalue"></param>
        private void CheckLjValue(double minvalue, double maxvalue)
        {
            this.txtResult.Text = "";
            if (ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FloorType != FloorTypeEnum.lowFloor)
            {
                if (ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.OverAk > 3.2)
                {
                    if (ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.LjTotal < minvalue)
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
                    if (ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.LjTotal < 0.75 * minvalue)
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

        private void CheckValueUserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
