﻿using QuikGraph;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.Project.Module;
using System;

namespace TianHua.Electrical.PDS.UI.Project
{
    /// <summary>
    /// PDS项目ViewModel
    /// </summary>
    public class PDSProjectVM
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static PDSProjectVM instance = new PDSProjectVM();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static PDSProjectVM() { }
        internal PDSProjectVM() { }
        public static PDSProjectVM Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThPDSProjectVMGraphInfo graphInfo;

        public Action ProjectViewModelChanged;

        public void ProjectDataChanged()
        {
            ConverterFactory.ConvertToViewModel();//数据转换ViewModel
            if (!instance.ProjectViewModelChanged.IsNull())
                instance.ProjectViewModelChanged();//推送VM状态改变
        }

        #region ViewModel 集合
        public InformationMatchViewModel InformationMatchViewModel { get; set; } //信息匹配视图模型
        #endregion
    }
}
