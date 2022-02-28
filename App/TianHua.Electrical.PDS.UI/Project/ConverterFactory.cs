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
    /// <summary>
    /// 转换器工厂
    /// </summary>
    public static class ConverterFactory
    {
        //public static output Convert<input,output>(input viewerModule) where input : PDSProjectModule where output : NotifyPropertyChangedBase
        //{
        //    return null as output;
        //}

        //public static output GetViewModel<output>() where output : NotifyPropertyChangedBase
        //{
            
        //    return null as output;
        //}

        //public static bool SaveToProject<input>() where input : NotifyPropertyChangedBase
        //{
        //    return true;
        //}

        public static void ConvertToViewModel()
        {
            PDSProjectVM.Instance.graphData = PDSProject.Instance.graphData.Graph.Clone();
            PDSProjectVM.Instance.InformationMatchViewModel = new ViewModels.InformationMatchViewModel(PDSProjectVM.Instance.graphData);
        }

        public static void ConvertToProject()
        {
            PDSProject.Instance.graphData.Graph = PDSProjectVM.Instance.graphData.Clone();
        }
    }
}
