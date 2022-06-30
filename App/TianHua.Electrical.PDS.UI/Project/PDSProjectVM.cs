using TianHua.Electrical.PDS.UI.ViewModels;

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

        #region ViewModel 集合
        public GlobalParameterViewModel GlobalParameterViewModel { get; set; } = new();
        public InformationMatchViewModel InformationMatchViewModel { get; set; } = new();
        public LoadCalculationViewModel LoadCalculationViewModel { get; set; } = new();
        #endregion
    }
}
