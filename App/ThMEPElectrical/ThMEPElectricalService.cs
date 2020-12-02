using ThMEPElectrical.Model;
using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical
{
    public class ThMEPElectricalService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPElectricalService instance = new ThMEPElectricalService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPElectricalService() { }
        internal ThMEPElectricalService() { }
        public static ThMEPElectricalService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        /// <summary>
        /// 烟感布置参数
        /// </summary>
        public PlaceParameter Parameter { get; set; }

        /// <summary>
        /// 提资块转换参数
        /// </summary>
        public ThBConvertParameter ConvertParameter { get; set; }
    }
}
