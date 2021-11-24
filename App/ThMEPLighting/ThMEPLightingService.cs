using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting
{
    public class ThMEPLightingService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPLightingService instance = new ThMEPLightingService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPLightingService() { }
        internal ThMEPLightingService() 
        {
            LightArrangeUiParameter = new ThLightArrangeUiParameter();
        }
        public static ThMEPLightingService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThLightArrangeUiParameter LightArrangeUiParameter { get; set; }

        /// <summary>
        /// 照目
        /// </summary>
        public List<ThLigitingWiringModel> Parameter = new List<ThLigitingWiringModel>();
    }
}
