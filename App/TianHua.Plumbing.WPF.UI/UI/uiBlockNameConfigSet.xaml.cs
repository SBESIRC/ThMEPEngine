using System.Windows.Controls;
using ThMEPWSS.ViewModel;
using System.Windows;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class uiBlockNameConfigSet : ThCustomWindow
    {
        public BlockConfigSetViewModel setViewModel;
        public BlockConfigSetViewModel setViewModelCloned;
        public uiBlockNameConfigSet(BlockConfigSetViewModel viewModel)
        {
            InitializeComponent();
            Title = viewModel.BlockName;
            setViewModel = viewModel;
            setViewModelCloned = viewModel?.Clone();
            if (null == viewModel)
            {
                setViewModel = new BlockConfigSetViewModel();
            }
            DataContext = setViewModel;
        }

        private void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThMEPWSS.BlockNameConfig.Cmd(setViewModel))
            {
                cmd.Execute();
            }
        }
        private void BtnSet2_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThMEPWSS.BlockNameConfig.Cmd(setViewModel))
            {
                cmd.Execute2();
            }
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            setViewModelCloned = setViewModel?.Clone();
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            setViewModel = setViewModelCloned?.Clone();
            this.DialogResult = false;
            this.Close();
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            var layerName = btn.Tag.ToString().Trim();
            foreach(var config in setViewModel.ConfigList)
            {
                if(config.layerName.Equals(layerName))
                {
                    setViewModel.ConfigList.Remove(config);
                    return;
                }
            }
        }
    }
}
