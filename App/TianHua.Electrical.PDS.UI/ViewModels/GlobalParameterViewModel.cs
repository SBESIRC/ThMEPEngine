using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class GlobalParameterViewModel : NotifyPropertyChangedBase
    {
        public GlobalParameterViewModel(ProjectGlobalConfiguration projectGlobalConfiguration)
        {
            _configuration = projectGlobalConfiguration;
        }

        private readonly ProjectGlobalConfiguration _configuration;
        public ProjectGlobalConfiguration Configuration => _configuration;
    }
}
