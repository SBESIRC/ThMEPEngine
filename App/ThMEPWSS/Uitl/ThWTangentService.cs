using ThCADExtension;
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

        public bool IsSprinkler(Entity obj)
        {
            var dxfName = obj.GetRXClass().DxfName;
            return dxfName == ThCADCommon.DxfName_TCH_EQUIPMENT_16 ||
                dxfName == ThCADCommon.DxfName_TCH_EQUIPMENT_12;
        }
    }
}