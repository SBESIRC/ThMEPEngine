using System.Windows;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Electrical.UI.ElectricalLoadCalculation
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
            this.RoomFunctionTxt.SelectionStart = this.RoomFunctionTxt.Text.Length;
            this.RoomFunctionTxt.Focus();
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
