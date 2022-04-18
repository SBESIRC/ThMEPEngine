using TianHua.Electrical.PDS.Project;

namespace TianHua.Electrical.PDS.UI.Project
{
    public static class ConverterFactory
    {
        public static void ConvertToViewModel()
        {
            PDSProjectVM.Instance.graphInfo = new ThPDSProjectVMGraphInfo();
            PDSProjectVM.Instance.graphInfo.graphData = PDSProject.Instance.graphData.Graph;
            PDSProjectVM.Instance.InformationMatchViewModel = new ViewModels.InformationMatchViewModel(PDSProjectVM.Instance.graphInfo);
            PDSProjectVM.Instance.GlobalParameterViewModel = new ViewModels.GlobalParameterViewModel(PDSProject.Instance.projectGlobalConfiguration);
        }
    }
}
