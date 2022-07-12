using System.Windows;
using TianHua.Electrical.PDS.Project.Module;
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
            var vm = this.DataContext as ThPDSCreateLoadVM;
            ThPDSProjectGraphService.CreatNewLoad(CreateData(vm));
            Close();
        }
        private void btnSave(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ThPDSCreateLoadVM;
            ThPDSProjectGraphService.CreatNewLoad(CreateData(vm));
            Close();
        }

        private ThPDSProjectGraphNodeData CreateData(ThPDSCreateLoadVM vm)
        {
            var data = ThPDSProjectGraphNodeData.Create();
            data.Power = vm.Power;
            data.Storey = vm.Storey;
            data.Number = vm.Number;
            data.Type = vm.Type.Type;
            data.FireLoad = vm.FireLoad;
            data.Description = vm.Description;
            data.Sync();
            return data;
        }
    }
}
