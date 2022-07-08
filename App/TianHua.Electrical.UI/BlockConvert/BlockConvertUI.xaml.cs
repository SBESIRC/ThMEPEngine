using ThMEPElectrical.Model;
using ThControlLibraryWPF.CustomControl;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Data;
using System.Linq;

namespace TianHua.Electrical.UI.BlockConvert
{
    public partial class BlockConvertUI : ThCustomWindow
    {
        public ThBlockConvertModel Parameter { get; set; }
        public bool GoOn { get; set; }
        
        public BlockConvertUI()
        {
            Parameter = new ThBlockConvertModel();
            GoOn = false;            
            InitializeComponent();
            this.DataContext = Parameter;
        }

        private void btnBlockConvert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GoOn = true;
            this.Close();
        }

        private void btnUpdateCompare_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void wssConvertStrongEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = false;
            convertManualActuator.IsChecked = false;
        }

        private void wssConvertWeakEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = true;
        }

        private void wssConvertAllEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = true;
        }

        private void btnBlockUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void ignoreChange_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var ids = table1.SelectedItems.OfType<BlockConvertInfo>().Select(o=>o.Id).ToList();
            if(ids.Count>0)
            {
                Parameter.IgnoreBlockConvertInfos(ids);
                table1.ItemsSource = null;
                table1.ItemsSource = Parameter.BlockConvertInfos;
            }
        }

        private void localUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void table1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //DataGrid datagrid = sender as DataGrid;
            //Point aP = e.GetPosition(datagrid);
            //IInputElement obj = datagrid.InputHitTest(aP);
            //var target = obj as DependencyObject;
            //while(target!=null)
            //{
            //    if (target is DataGridRow)
            //    {
            //        DataGridRow dgr = target as DataGridRow;
            //        DataRowView theDRV = dgr.Item as DataRowView;
            //        table1.SelectedItem = theDRV;
            //        break;
            //    }
            //    target = VisualTreeHelper.GetParent(target);
            //}
        }
    }
}
