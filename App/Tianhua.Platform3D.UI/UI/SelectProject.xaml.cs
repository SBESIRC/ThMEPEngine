using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ThPlatform3D.Model.Project;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// SelectProject.xaml 的交互逻辑
    /// </summary>
    public partial class SelectProject : Window
    {
        public SelectProject(List<DBProject> prjs,string selectId)
        {
            InitializeComponent();
            dGridPrj.ItemsSource = prjs;
            if(!string.IsNullOrEmpty(selectId))
                dGridPrj.SelectedItem = prjs.Where(c => c.Id == selectId).FirstOrDefault();
        }
        public DBProject GetSelectProject() 
        {
            if (null == dGridPrj.SelectedItem)
                return null;
            return dGridPrj.SelectedItem as DBProject;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
