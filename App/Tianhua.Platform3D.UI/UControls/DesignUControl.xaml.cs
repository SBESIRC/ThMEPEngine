using AcHelper;
using AcHelper.Commands;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tianhua.Platform3D.UI.UControls
{
    /// <summary>
    /// DesignUControl.xaml 的交互逻辑
    /// </summary>
    public partial class DesignUControl : UserControl
    {
        public DesignUControl()
        {
            InitializeComponent();
            mainGrid.DataContext = this;
        }
        RelayCommand<string> cmdButtonClick;
        public ICommand OnButtonClickCommand
        {
            get
            {
                if (null == cmdButtonClick)
                    cmdButtonClick = new RelayCommand<string>((cmdString) => CmdButtonClick(cmdString));
                return cmdButtonClick;
            }
        }
        private void CmdButtonClick(string cmdName)
        {
            if (string.IsNullOrEmpty(cmdName))
                return;
            SendCommand(cmdName);
        }
        private void SendCommand(string cmdName)
        {
            if (Active.Document == null)
                return;
            CommandHandlerBase.ExecuteFromCommandLine(false, cmdName);
        }
    }
}
