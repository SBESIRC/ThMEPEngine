using ThMEPWSS.FlushPoint.Model;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThFlushPointParameterService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThFlushPointParameterService instance = new ThFlushPointParameterService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThFlushPointParameterService() { }
        internal ThFlushPointParameterService()
        {
            FlushPointParameter = new ThFlushPointParameter();
        }
        public static ThFlushPointParameterService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThFlushPointParameter FlushPointParameter { get; set; }
    }
}
