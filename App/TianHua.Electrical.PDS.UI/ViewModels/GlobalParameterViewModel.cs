using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class GlobalParameterViewModel : NotifyPropertyChangedBase
    {
        public ProjectGlobalConfiguration Configuration
        {
            get
            {
                return PDSProject.Instance.projectGlobalConfiguration;
            }
        }
    }
}
