using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.UI
{
    public partial class ExtractLayerConfigUI : ThCustomWindow
    {
        private ThExtractLayerConfigVM ExtractLayerConfigVM { get; set; } 
        public ExtractLayerConfigUI()
        {
            ExtractLayerConfigVM= new ThExtractLayerConfigVM();
            InitializeComponent();
            this.DataContext = ExtractLayerConfigVM;
        }

        private void rbLayer_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddBeamLayer.IsEnabled = false;
            this.btnDelBeamLayer.IsEnabled = false;
            this.listBox1.IsEnabled = false;
        }
        private void rbDB_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddBeamLayer.IsEnabled = false;
            this.btnDelBeamLayer.IsEnabled = false;
            this.listBox1.IsEnabled = false;
        }

        private void btnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractLayerConfigVM.Save();
            this.Close();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void rbBeamArea_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddBeamLayer.IsEnabled = true;
            this.btnDelBeamLayer.IsEnabled = true;
            this.listBox1.IsEnabled = true;
        }
        private void btnAddBeamLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractLayerConfigVM.PickBeamLayer();
        }

        private void btnDelBeamLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < listBox1.SelectedItems.Count; i++)
            {
                layers.Add((listBox1.SelectedItems[i] as ThLayerInfo).Layer);
            }
            ExtractLayerConfigVM.RemoveBeamLayers(layers);
            this.listBox1.ItemsSource = null;
            this.listBox1.ItemsSource = ExtractLayerConfigVM.BeamLayerInfos;
        }

        private void btnAddShearWallLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractLayerConfigVM.PickShearWallLayer();
        }

        private void btnDelShearWallLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < listBox2.SelectedItems.Count; i++)
            {
                layers.Add((listBox2.SelectedItems[i] as ThLayerInfo).Layer);
            }
            ExtractLayerConfigVM.RemoveShearWallLayers(layers);
            this.listBox2.ItemsSource = null;
            this.listBox2.ItemsSource = ExtractLayerConfigVM.ShearWallLayerInfos;
        }
    }
}
