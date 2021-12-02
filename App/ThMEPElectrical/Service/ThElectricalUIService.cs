using ThMEPElectrical.Model;

namespace ThMEPElectrical.Service
{
    public class ThElectricalUIService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThElectricalUIService instance = new ThElectricalUIService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThElectricalUIService() { }
        internal ThElectricalUIService() { }
        public static ThElectricalUIService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        /// <summary>
        /// 安防平面
        /// </summary>
        public ThSecurityPlaneSystemParameter Parameter = new ThSecurityPlaneSystemParameter();
    }
}
