using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Project
{
    public static class ConverterFactory
    {
        public static void ConvertToViewModel()
        {
            PDSProjectVM.Instance.InformationMatchViewModel = new InformationMatchViewModel(PDSProject.Instance.graphData.Graph);
            PDSProjectVM.Instance.GlobalParameterViewModel = new GlobalParameterViewModel(PDSProject.Instance.projectGlobalConfiguration);
        }

        public static void ConvertToGlobalParameterViewModel()
        {
            PDSProjectVM.Instance.GlobalParameterViewModel = new GlobalParameterViewModel(PDSProject.Instance.projectGlobalConfiguration);
        }
    }
}
