using ThControlLibraryWPF.CustomControl;
using TianHua.Hvac.UI.Command;
using ThMEPHVAC.Model;
using ThMEPEngineCore.Model;
using System.Collections.Generic;

namespace TianHua.Hvac.UI.UI
{
    public partial class uiFGDXParameter : ThCustomWindow
    {
        ThFGDXParameter Parameter = null;
        private List<ThIfcRoom> Rooms { get; set; }
        public static uiFGDXParameter Instance = null;

        static uiFGDXParameter()
        {           
            Instance = new uiFGDXParameter();
        }
        uiFGDXParameter()
        {
            InitializeComponent();
            if (Parameter == null)
            {
                Parameter = new ThFGDXParameter();
            }            
            DataContext = Parameter;
        }

        private void btnInsert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var cmd = new ThHvacFGDXInsertCmd(Parameter, Rooms))
            {
                cmd.Execute();
            }
            if (Instance != null)
            {
                Instance.Close();
            }
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
        public void SetRooms(List<ThIfcRoom> rooms)
        {
            Rooms = rooms;
        }
    }
}
