using System;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Project
{
    /// <summary>
    /// PDS项目
    /// </summary>
    [Serializable]
    public class PDSProject
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static PDSProject instance = new PDSProject();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static PDSProject() { }
        internal PDSProject() { }
        public static PDSProject Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThPDSProjectGraph graphData;

        public ProjectGlobalConfiguration projectGlobalConfiguration;

        public List<THPDSProjectSubstation> substations;

        public THPDSSubstationMap substationMap;

        private bool InitializedState;

        public Action DataChanged;

        public Action GlobalConfigurationChanged;

        /// <summary>
        /// 加载项目
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if (!InitializedState)
            {
                substations = new List<THPDSProjectSubstation>();
                substationMap = new THPDSSubstationMap();
                this.LoadGlobalConfig();
                InitializedState = true;
            }
            if (string.IsNullOrEmpty(url))
            {
                //Creat New Project
                this.graphData = new ProjectGraph().CreatPDSProjectGraph();
                PDSProjectExtend.CalculateProjectInfo();
                this.projectGlobalConfiguration = new ProjectGlobalConfiguration();
                instance.DataChanged?.Invoke();
            }
            else
            {
                PDSProjectManagement.ImportProject(url);
                instance.DataChanged?.Invoke();
            }
        }

        public void LoadGlobalConfiguration(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                PDSProjectManagement.ImportGlobalConfiguration(url);
                instance.GlobalConfigurationChanged?.Invoke();
            }
        }
    }
}
