using ThControlLibraryWPF.CustomControl;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.UI
{
    public partial class InsertRoomNameUI : ThCustomWindow
    {
        public ThInsertRoomNameVM _vm;
        public InsertRoomNameUI(ThInsertRoomNameVM vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = vm;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            this.Topmost = true;
        }

        public void UpdateDataContext(ThInsertRoomNameVM vm)
        {
            if(vm==null)
            {
                _vm = new ThInsertRoomNameVM();
            }
            else
            {
                if(vm.Id == _vm.Id)
                {
                    return;
                }
                else
                {
                    _vm = vm;
                    this.DataContext = vm;
                }
            }
        }

        private void ThCustomWindow_Closed(object sender, System.EventArgs e)
        {
        }
    }
}
