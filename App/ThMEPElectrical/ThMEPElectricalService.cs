using ThMEPElectrical.Model;

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


        public PlaceParameter Parameter { get; set; }
    }
}
