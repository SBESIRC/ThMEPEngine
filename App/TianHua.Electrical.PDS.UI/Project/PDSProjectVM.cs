using QuickGraph;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project
{
    /// <summary>
    /// PDS项目ViewModel
    /// </summary>
    public class PDSProjectVM
    {
        private static PDSProjectVM instance = new PDSProjectVM();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static PDSProjectVM() { }
        internal PDSProjectVM() { }
        public static PDSProjectVM Instance { get { return instance; } }

        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graphData;



        #region ViewModel 集合
        public InformationMatchViewModel InformationMatchViewModel { get; set; } //信息匹配视图模型
        #endregion
    }
}
