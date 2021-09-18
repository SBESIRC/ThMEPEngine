using System.Collections.Generic;

namespace TianHua.FanSelection.UI
{
    public class ThFanSelectionService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThFanSelectionService instance = new ThFanSelectionService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThFanSelectionService() { }
        internal ThFanSelectionService() { }
        public static ThFanSelectionService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public FanDataModel Model { get; set; }

        public FanDataModel SubModel { get; set; }

        public List<FanDataModel> ErasedModels { get; set; }
        public List<FanDataModel> UnerasedModels { get; set; }
        public Dictionary<FanDataModel, FanDataModel> ModelMapping { get; set; }
        public object Message { get; set; }
        public object MessageArgs { get; set; }
    }
}
