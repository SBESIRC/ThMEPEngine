using Xbim.Ifc;
using ThMEPEngineCore.xBIM;

namespace ThMEPEngineCore.Model
{
    public class ThIfcStoreService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThIfcStoreService instance = new ThIfcStoreService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThIfcStoreService() { }
        internal ThIfcStoreService() { }
        public static ThIfcStoreService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public IfcStore Model { get; set; }

        public void Initialize(string projectName)
        {
            Model = ThModelExtension.CreateAndInitModel(projectName);
        }
    }
}
