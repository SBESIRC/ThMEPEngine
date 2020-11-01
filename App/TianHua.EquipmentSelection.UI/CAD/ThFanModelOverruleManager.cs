using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanModelOverruleManager
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThFanModelOverruleManager instance = new ThFanModelOverruleManager();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThFanModelOverruleManager() { }
        internal ThFanModelOverruleManager() { }
        public static ThFanModelOverruleManager Instance { get { return instance; } }

        //-------------SINGLETON-----------------

        private ThFanModelObjectOverrule ObjectOverrule { get; set; }

        public void Register()
        {
            ObjectOverrule = new ThFanModelObjectOverrule();
            ObjectOverrule.SetXDataFilter(ThFanSelectionCommon.RegAppName_FanSelection);
            Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)), ObjectOverrule, true);
        }

        public void UnRegister()
        {
            Overrule.RemoveOverrule(RXClass.GetClass(typeof(BlockReference)), ObjectOverrule);
        }
    }
}
