using System.Windows;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSCreateLoad : Window
    {
        public ThPDSCreateLoad()
        {
            InitializeComponent();
            this.DataContext = new ThPDSCreateLoadVM();
        }

        private void btnInsert(object sender, RoutedEventArgs e)
        {
            //double.TryParse(defaultPower.Text, out var v);
            //new ThPDSUpdateToDwgService().AddLoadDimension(ThPDSProjectGraphService.CreatNewLoad(/*defaultKV: double.Parse(defaultKV.SelectedItem.ToString()), */defaultLoadID: defaultLoadID.Text, defaultPower: v /*defaultPower: double.Parse(defaultPower.Text)*/, defaultDescription: defaultDescription.Text, defaultFireLoad: defaultFireLoad.IsChecked == true, imageLoadType: ImageLoadType));
            //WeakReferenceMessenger.Default.Send(new GraphNodeAddMessage("btnInsert Click"));
            Close();
        }
        private void btnSave(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ThPDSCreateLoadVM;
            //if (double.TryParse(defaultPower.Text, out var v))
            //{
            //    ThPDSProjectGraphService.CreatNewLoad(
            //        defaultLoadID: defaultLoadID.Text,
            //        defaultPower: v,
            //        defaultDescription: defaultDescription.Text,
            //        defaultFireLoad: defaultFireLoad.IsChecked == true,
            //        imageLoadType: ImageLoadType);
            //    WeakReferenceMessenger.Default.Send(new GraphNodeAddMessage("btnSave Click"));
            //}
            Close();
        }
    }
}
