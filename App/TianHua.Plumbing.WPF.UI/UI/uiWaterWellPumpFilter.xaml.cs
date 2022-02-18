using System.Linq;
using System.Windows;
using ThMEPWSS.Pipe.Model;
using System.Windows.Controls;
using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiWaterWellPumpFilter.xaml 的交互逻辑
    /// </summary>
    public partial class uiWaterWellPumpFilter : ThCustomWindow
    {
        private List<TextBox> WhiteTextBox = new List<TextBox>();
        private List<TextBox> BlackTextBox = new List<TextBox>();
        private WaterWellIdentifyConfigInfo identifyConfigInfo = new WaterWellIdentifyConfigInfo();
        public uiWaterWellPumpFilter()
        {
            InitializeComponent();
        }
        public WaterWellIdentifyConfigInfo GetIdentfyConfigInfo()
        {
            return identifyConfigInfo;
        }
        public void InitUi()
        {
            for (int i = 0; i < identifyConfigInfo.WhiteList.Count; i++)
            {
                var tmpTBox = new TextBox();
                tmpTBox.Text = identifyConfigInfo.WhiteList[i];
                tmpTBox.Margin = new Thickness(0.0, 0.0, 0.0, 5.0);
                WhiteSpanel.Children.Add(tmpTBox);
                WhiteTextBox.Add(tmpTBox);
            }

            for (int i = 0; i < identifyConfigInfo.BlackList.Count; i++)
            {
                var tmpTBox = new TextBox();
                tmpTBox.Text = identifyConfigInfo.BlackList[i];
                tmpTBox.Margin = new Thickness(0.0, 0.0, 0.0, 5.0);
                BlackSpanel.Children.Add(tmpTBox);
                BlackTextBox.Add(tmpTBox);
            }
        }

        public void SetWaterWellIdentifyConfigInfo(WaterWellIdentifyConfigInfo info)
        {
            identifyConfigInfo = info;
            InitUi();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < WhiteTextBox.Count;i++)
            {
                identifyConfigInfo.WhiteList[i] = WhiteTextBox[i].Text;
            }

            for (int i = 0; i < BlackTextBox.Count; i++)
            {
                identifyConfigInfo.BlackList[i] = BlackTextBox[i].Text;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void WhiteAddBtn_Click(object sender, RoutedEventArgs e)
        {
            identifyConfigInfo.WhiteList.Add("");
            var tmpTBox = new TextBox();
            tmpTBox.Margin = new Thickness(0.0, 0.0, 0.0, 5.0);
            WhiteSpanel.Children.Add(tmpTBox);
            WhiteTextBox.Add(tmpTBox);
        }

        private void WhiteRedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (identifyConfigInfo.WhiteList.Count <= 3)
            {
                return;
            }
            identifyConfigInfo.WhiteList.Remove(identifyConfigInfo.WhiteList.Last());
            WhiteSpanel.Children.Remove(WhiteTextBox.Last());
            WhiteTextBox.Remove(WhiteTextBox.Last());
        }

        private void BlackAddBtn_Click(object sender, RoutedEventArgs e)
        {
            identifyConfigInfo.BlackList.Add("");
            var tmpTBox = new TextBox();
            tmpTBox.Margin = new Thickness(0.0, 0.0, 0.0, 5.0);
            BlackSpanel.Children.Add(tmpTBox);
            BlackTextBox.Add(tmpTBox);
        }

        private void BlackRedBtn_Click(object sender, RoutedEventArgs e)
        {
            if(identifyConfigInfo.BlackList.Count <= 3)
            {
                return;
            }
            identifyConfigInfo.BlackList.Remove(identifyConfigInfo.BlackList.Last());
            BlackSpanel.Children.Remove(BlackTextBox.Last());
            BlackTextBox.Remove(BlackTextBox.Last());
        }
    }
}
