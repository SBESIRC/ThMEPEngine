using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class GlobalParameterViewModel : NotifyPropertyChangedBase
    {
        private ProjectGlobalConfiguration configuration;
        public GlobalParameterViewModel(ProjectGlobalConfiguration projectGlobalConfiguration)
        {
            configuration = projectGlobalConfiguration;
        }

        public ProjectGlobalConfiguration Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }
    }
}
