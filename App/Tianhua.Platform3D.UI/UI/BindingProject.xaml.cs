using AcHelper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThPlatform3D.Model.Project;
using ThPlatform3D.Model.User;
using ThPlatform3D.Service;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// BindingProject.xaml 的交互逻辑
    /// </summary>
    public partial class BindingProject : Window
    {
        ProjectDBService  dbProject = new ProjectDBService();
        List<DBProject> allProjects = new List<DBProject>();
        BindingProjectVM projectVM;
        UserInfo currentUser;
        public BindingProject(UserInfo user,string selectPrjId,string selectSubPrjId,string selectMajor)
        {
            InitializeComponent();
            currentUser = user;
            var userId = user.PreSSOId;
            allProjects = dbProject.GetUserProjects(userId);
            projectVM = new BindingProjectVM(allProjects, selectPrjId, selectSubPrjId, selectMajor);
            this.DataContext = projectVM;
        }

        private void btnSelectPrj_Click(object sender, RoutedEventArgs e)
        {
            var selectId = projectVM.SelectProject == null ? string.Empty : projectVM.SelectProject.Id;
            var selectPrj = new SelectProject(projectVM.AllProjects, selectId);
            selectPrj.Owner = this;
            var res = selectPrj.ShowDialog();
            if (res != true)
                return;
            var select = selectPrj.GetSelectProject();
            if (null == select || select.Id == projectVM.SelectProject.Id)
                return;
            projectVM.SelectProject = select;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnBinding_Click(object sender, RoutedEventArgs e)
        {
            if (null == projectVM.SelectProject || null == projectVM.SelectSubProject || string.IsNullOrEmpty(projectVM.SelectMajor))
            {
                MessageBox.Show("没有选中项目或子项，无法进行绑定操作，请选中相应的项目子项后再进行绑定操作!", "提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //在数据库中插入记录
            var docName = Active.DocumentName;
            ProjectFile projectFile = new ProjectFile();
            projectFile.ProjectFileId = System.Guid.NewGuid().ToString();
            projectFile.PrjId = projectVM.SelectProject.Id;
            projectFile.SubPrjId = projectVM.SelectSubProject.SubentryId;
            projectFile.MajorName = projectVM.SelectMajor;
            projectFile.ApplicationName = "CAD";
            projectFile.FileName = Path.GetFileNameWithoutExtension(docName);
            projectFile.CreaterId = currentUser.UserLogin.Username;
            projectFile.CreaterName = currentUser.ChineseName;
            projectFile.IsDel = 0;
            var res = dbProject.CADBindingProject(projectFile,out string msg);
            if (!res) 
            {
                MessageBox.Show(string.Format("绑定失败-{0}", msg), "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
        public string GetBindingResult(out DBProject selectPrj,out DBSubProject selectSuPrj) 
        {
            selectPrj = projectVM.SelectProject;
            selectSuPrj = projectVM.SelectSubProject;
            return projectVM.SelectMajor;
        }
    }
    class BindingProjectVM : NotifyPropertyChangedBase 
    {
        public List<DBProject> AllProjects { get; set; }
        private DBProject selectPrj { get; set; }
        public DBProject SelectProject 
        {
            get { return selectPrj; }
            set 
            {
                selectPrj = value;
                if(null != selectPrj)
                    SelctProjectChange();
                this.RaisePropertyChanged();
            }
        }
        
        public BindingProjectVM(List<DBProject> allProject,string selectId,string selectSubPrjId,string selectMajor) 
        {
            AllProjects = new List<DBProject>();
            AllProjects.AddRange(allProject);
            SubProjects = new ObservableCollection<DBSubProject>();
            if (!string.IsNullOrEmpty(selectId))
                SelectProject = AllProjects.Where(c => c.Id == selectId).FirstOrDefault();
            else
                SelectProject = AllProjects.FirstOrDefault();
            if (SubProjects.Count > 0) 
            {
                if (!string.IsNullOrEmpty(selectSubPrjId))
                    SelectSubProject = SubProjects.Where(c => c.SubentryId == selectSubPrjId).FirstOrDefault();
                else
                    SelectSubProject = SubProjects.FirstOrDefault();
            }
            Majors = new List<string>();
            Majors.Add("结构");
            Majors.Add("建筑");
            if (string.IsNullOrEmpty(selectMajor)) 
            {
                SelectMajor = Majors.First();
            }
            else 
            {
                SelectMajor = Majors.Where(c => c == selectMajor).FirstOrDefault();
            }
            
        }
        public List<string> Majors { get; set; }
        private string selectMajor { get; set; }
        public string SelectMajor 
        {
            get { return selectMajor; }
            set 
            {
                selectMajor = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<DBSubProject> subProjects { get; set; }
        public ObservableCollection<DBSubProject> SubProjects
        {
            get { return subProjects; }
            set 
            {
                subProjects = value;
                this.RaisePropertyChanged();
            }
        }
        private DBSubProject selectSubPrj { get; set; }
        public DBSubProject SelectSubProject 
        {
            get { return selectSubPrj; }
            set
            {
                selectSubPrj = value;
                this.RaisePropertyChanged();
            }
        }
        private string prjNum { get; set; }
        public string ShowPrjNum 
        {
            get { return prjNum; }
            set 
            {
                prjNum = value;
                this.RaisePropertyChanged();
            }
        }
        private string prjName { get; set; }
        public string ShowPrjName
        {
            get { return prjName; }
            set
            {
                prjName = value;
                this.RaisePropertyChanged();
            }
        }
        private void SelctProjectChange() 
        {
            if (null == selectPrj)
            {
                ShowPrjNum = "";
                ShowPrjName = "";
            }
            ShowPrjNum = selectPrj.PrjNo;
            ShowPrjName = selectPrj.PrjName;
            SubProjects.Clear();
            foreach (var item in selectPrj.SubProjects)
                SubProjects.Add(item);
            SelectSubProject = subProjects.FirstOrDefault();
        }
    }
}
