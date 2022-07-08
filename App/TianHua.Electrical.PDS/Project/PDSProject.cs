using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.GlobalConfiguration;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using static Microsoft.IO.RecyclableMemoryStreamManager;
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

        private bool InitializedState;
        private Task TimingTask;

        #region Data

        /// <summary>
        /// Graph
        /// </summary>
        public ThPDSProjectGraph graphData;

        /// <summary>
        /// 全局配置
        /// </summary>
        public ProjectGlobalConfiguration projectGlobalConfiguration;

        /// <summary>
        /// 变电所
        /// </summary>
        public List<THPDSProjectSubstation> substations;

        /// <summary>
        /// 变电所映射
        /// </summary>
        public THPDSSubstationMap substationMap;
        #endregion

        #region Event

        public event EventHandler<ProjectDataChangedEventArgs> ProjectDataChanged;

        public event EventHandler<ProjectGlobalConfigurationChangedEventArgs> ProjectGlobalConfigurationChanged;
        #endregion

        #region API

        /// <summary>
        /// 初始化成空白项目
        /// </summary>
        public void SetDefaults()
        {
            if (!InitializedState)
            {
                this.LoadGlobalConfig();
                InitializedState = true;
            }
            substations = new List<THPDSProjectSubstation>();
            substationMap = new THPDSSubstationMap();
            this.graphData = new ProjectGraph().CreatPDSProjectGraph();
            PDSProjectExtend.CalculateProjectInfo();
            instance.BroadcastProjectDataChanged("SetDefaults", "初始化成空白项目");
            this.projectGlobalConfiguration = new ProjectGlobalConfiguration();
            instance.BroadcastProjectGlobalConfigurationChanged("SetDefaults", "初始化成空白项目");
            AutoSave();
        }

        /// <summary>
        /// 从文件中装载项目
        /// </summary>
        /// <param name="projectConfigUrl"></param>
        public void LoadFromFile(string projectConfigUrl)
        {
            if (!InitializedState)
            {
                this.LoadGlobalConfig();
                InitializedState = true;
            }
            //变电所信息后期也会慢慢加到配置文件里被加载出来，而不是在这里赋值
            //这是临时的处理办法
            substations = new List<THPDSProjectSubstation>();
            substationMap = new THPDSSubstationMap();
            PDSProjectManagement.ImportProject(projectConfigUrl);
            instance.BroadcastProjectDataChanged("LoadFromFile", "从文件中装载项目");
            instance.BroadcastProjectGlobalConfigurationChanged("LoadFromFile", "从文件中装载项目");
            AutoSave();
        }

        /// <summary>
        /// 装载全局配置
        /// </summary>
        public void LoadGlobalConfiguration(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                PDSProjectManagement.ImportGlobalConfiguration(url);
                instance.BroadcastProjectGlobalConfigurationChanged("LoadGlobalConfiguration", "装载全局配置");
            }
        }

        /// <summary>
        /// 保存到项目文件中
        /// </summary>
        public void Save(string filePath, string fileName)
        {
            ThPDSProjectGraphService.ExportProject(filePath, fileName);
        }

        /// <summary>
        /// 自动保存
        /// </summary>
        private void AutoSave()
        {
            //if (instance.TimingTask.IsNull())
            //{
            //    instance.TimingTask = new Task(AutomaticallySaveProjectFile);
            //    instance.TimingTask.Start();
            //}
        }

        private void AutomaticallySaveProjectFile()
        {
            //Thread.Sleep(FolderSetting.TimeInterval * 60 * 1000);
            //Save(FolderSetting.DefaultPath, $"{DateTime.Now.ToString("yyyyMMdd HHmmss")}.pdsProject");
            //AutomaticallySaveProjectFile();
        }

        /// <summary>
        /// 广播ProjectDataChanged
        /// </summary>
        public void BroadcastProjectDataChanged(string apiName, string message)
        {
            instance.ProjectDataChanged?.Invoke(this, new ProjectDataChangedEventArgs(apiName, message));
        }

        /// <summary>
        /// 广播ProjectGlobalConfigurationChanged
        /// </summary>
        public void BroadcastProjectGlobalConfigurationChanged(string apiName, string message)
        {
            instance.ProjectGlobalConfigurationChanged?.Invoke(this, new ProjectGlobalConfigurationChangedEventArgs(apiName, message));
        }
        #endregion
    }
}
