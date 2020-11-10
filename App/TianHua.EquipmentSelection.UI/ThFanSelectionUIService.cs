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
    }
}
