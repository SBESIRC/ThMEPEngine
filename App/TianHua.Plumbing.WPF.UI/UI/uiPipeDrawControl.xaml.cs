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
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiPipeDrawControl.xaml 的交互逻辑
    /// </summary>
    public partial class uiPipeDrawControl : ThCustomWindow
    {
        public uiPipeDrawControl()
        {
            InitializeComponent();
        }
        private void SetBtnEnabled(bool isEnabled)
        {
            btnRoomWaterPipe.IsEnabled = isEnabled;
            btnBalconyPipe.IsEnabled = isEnabled;
            btnCondensatePipe.IsEnabled = isEnabled;
            btnFloorDrain.IsEnabled = isEnabled;
            btnSewageWastePipe.IsEnabled = isEnabled;
            btnWasteWaterPipe.IsEnabled = isEnabled;
            btnVentilatePipe.IsEnabled = isEnabled;
            btnCaissonPipe.IsEnabled = isEnabled;
            btnRoomCondensateFloorDrain.IsEnabled = isEnabled;
            btnCondensateFloorDrain.IsEnabled = isEnabled;
            btnBalconyCondensateFloorDrain.IsEnabled = isEnabled;
            btnRoomBalconyFloorDrain.IsEnabled = isEnabled;
            btnBalconyFloorDrain.IsEnabled = isEnabled;
            btnSewageWasteFloorDrain.IsEnabled = isEnabled;
            btnWasteVentilateSewageWaste.IsEnabled = isEnabled;
            
        }

        private void btnRoomWaterPipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "屋面雨水立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnBalconyPipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "阳台立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnCondensatePipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "冷凝水立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "地漏-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnSewageWastePipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "污废合流立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnWasteWaterPipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "废水立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnSewageWaterPipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "污水立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnVentilatePipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "通气立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnCaissonPipe_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "沉箱立管-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnRoomCondensateFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "屋面+冷凝+地漏-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnCondensateFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "冷凝+地漏-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnBalconyCondensateFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "阳台+冷凝+地漏-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnRoomBalconyFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "屋面+阳台+地漏-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnBalconyFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "阳台+地漏-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnSewageWasteFloorDrain_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "污废+通气-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnWasteVentilateSewageWaste_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "废水+通气+污废合流-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
        private void btnWasteVentilateSewage_Click(object sender, RoutedEventArgs e)
        {
            ThPipeDrawCmd pipDrawCmd = new ThPipeDrawCmd();
            pipDrawCmd.BlockName = "废水+通气+污水-AI";
            SetBtnEnabled(false);
            pipDrawCmd.Execute();
            SetBtnEnabled(true);
        }
    }
}
