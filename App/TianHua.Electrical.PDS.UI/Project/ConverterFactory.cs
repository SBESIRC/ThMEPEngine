using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project
{
    public static class ConverterFactory
    {
        public static void ConvertToViewModel()
        {
            PDSProjectVM.Instance.graphInfo = new ThPDSProjectVMGraphInfo() ;
            PDSProjectVM.Instance.graphInfo.graphData = PDSProject.Instance.graphData.Graph.Clone();
            PDSProjectVM.Instance.InformationMatchViewModel = new ViewModels.InformationMatchViewModel(PDSProjectVM.Instance.graphInfo);
            PDSProjectVM.Instance.GlobalParameterViewModel = new ViewModels.GlobalParameterViewModel(PDSProject.Instance.projectGlobalConfiguration);
        }
        public static void ConvertToProject()
        {
            PDSProject.Instance.graphData.Graph = PDSProjectVM.Instance.graphInfo.graphData.Clone();
        }
    }
}
