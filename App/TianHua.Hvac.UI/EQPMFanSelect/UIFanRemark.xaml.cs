using System.Windows;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    /// <summary>
    /// UIFanRemark.xaml 的交互逻辑
    /// </summary>
    public partial class UIFanRemark : ThCustomWindow
    {
        int buttonClickType = -1;
        string remarkStr = "";
        public UIFanRemark(string hisMsg)
        {
            InitializeComponent();
            txtRemark.Text = hisMsg;
        }
        public string GetInputRemark(out int type) 
        {
            type = buttonClickType;
            return remarkStr;
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            buttonClickType = 0;
            remarkStr = "";
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            buttonClickType = 1;
            remarkStr = txtRemark.Text;
        }

        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            buttonClickType = 99;
            remarkStr = txtRemark.Text;
        }
    }
}
