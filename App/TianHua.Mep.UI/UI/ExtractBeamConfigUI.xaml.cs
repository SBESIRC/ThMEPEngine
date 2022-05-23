using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.UI
{
    public partial class ExtractBeamConfigUI : ThCustomWindow
    {
        private ThExtractBeamConfigVM ExtractBeamConfigVM { get; set; } 
        public ExtractBeamConfigUI()
        {
            ExtractBeamConfigVM= new ThExtractBeamConfigVM();
            InitializeComponent();
            this.DataContext = ExtractBeamConfigVM;
        }

        private void rbLayer_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddLayer.IsEnabled = false;
            this.btnDelLayer.IsEnabled = false;
            this.listBox1.IsEnabled = false;
        }
        private void rbDB_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddLayer.IsEnabled = false;
            this.btnDelLayer.IsEnabled = false;
            this.listBox1.IsEnabled = false;
        }

        private void btnAddLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractBeamConfigVM.SelectLayer();
        }

        private void btnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractBeamConfigVM.Confirm();
            this.Close();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void rbBeamArea_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddLayer.IsEnabled = true;
            this.btnDelLayer.IsEnabled = true;
            this.listBox1.IsEnabled = true;
        }

        private void btnDelLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < listBox1.SelectedItems.Count; i++)
            {
                layers.Add((listBox1.SelectedItems[i] as ThLayerInfo).Layer);
            }
            ExtractBeamConfigVM.RemoveLayers(layers);
            this.listBox1.ItemsSource=null;
            this.listBox1.ItemsSource = ExtractBeamConfigVM.LayerInfos;
        }
    }
}
