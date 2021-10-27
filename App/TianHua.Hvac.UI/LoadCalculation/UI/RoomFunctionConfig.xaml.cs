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

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// ColdNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class RoomFunctionConfig : ThCustomWindow
    {
        public string RoomName { get; set; }
        public RoomFunctionConfig(string roomFunction)
        {
            InitializeComponent();
            this.RoomFunctionTxt.Text = roomFunction;
        }
        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.RoomName = RoomFunctionTxt.Text;
            this.DialogResult = true;
            this.Close();
        }
    }
}
