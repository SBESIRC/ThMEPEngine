using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.Hvac.UI.EQPMFanSelect.EventMonitor
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
            ObjectOverrule.SetXDataFilter(ThHvacCommon.RegAppName_FanSelectionEx);
            Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)), ObjectOverrule, true);
        }

        public void UnRegister()
        {
            Overrule.RemoveOverrule(RXClass.GetClass(typeof(BlockReference)), ObjectOverrule);
        }
    }
}
