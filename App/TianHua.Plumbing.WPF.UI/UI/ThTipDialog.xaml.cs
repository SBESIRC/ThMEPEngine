using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class ThTipDialog : ThCustomWindow
    {
        public ThTipDialog(string title,string content)
        {
            InitializeComponent();
            this.Title = title;
            this.tbTip.Text = content;
        }  
    }
}
