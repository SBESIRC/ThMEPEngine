using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Uitl
{
    public class ThWStandardService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThWStandardService instance = new ThWStandardService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThWStandardService() { }
        internal ThWStandardService() { }
        public static ThWStandardService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public bool IsSprinkler(Entity entity)
        {
            if (entity is BlockReference reference)
            {
                switch (reference.GetEffectiveName())
                {
                    case ThWSSCommon.SprayUpBlockName:
                    case ThWSSCommon.SprayDownBlockName:
                        return true;
                }
            }
            return false;
        }
    }
}
