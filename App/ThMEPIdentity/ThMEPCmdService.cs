using System.Collections.Generic;

namespace ThMEPIdentity
{
    public class ThMEPCmdService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPCmdService instance = new ThMEPCmdService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPCmdService() { }
        internal ThMEPCmdService() { }
        public static ThMEPCmdService Instance { get { return instance; } }

        public bool IsTHCommand(string name)
        {
            return name.StartsWith("TH");
        }

        public string Description(string name)
        {
            return string.Empty;
        }
    }
}
