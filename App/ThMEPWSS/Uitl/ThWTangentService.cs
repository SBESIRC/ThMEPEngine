using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Uitl
{
    public class ThWTangentService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThWTangentService instance = new ThWTangentService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThWTangentService() { }
        internal ThWTangentService() { }
        public static ThWTangentService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public bool IsSprinkler(Entity e)
        {
            return ThMEPTCHService.IsTCHSprinkler(e);
        }
    }
}