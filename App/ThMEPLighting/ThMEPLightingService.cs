using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThMEPElectrical.Model;
using ThMEPElectrical.ViewModel;
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
        public List<ThLigitingWiringModel> Parameter = ConvertToModel(new WiringConnectingViewModel().configLst);

        public static List<ThLigitingWiringModel> ConvertToModel(ObservableCollection<LoopConfig> configLst)
        {
            List<ThLigitingWiringModel> thLigitingWirings = new List<ThLigitingWiringModel>();
            var lightingModel = configLst.Where(x => x.systemType == "照明").FirstOrDefault();
            if (lightingModel == null)
            {
                return thLigitingWirings;
            }
            foreach (var model in lightingModel.configModels)
            {
                ThLigitingWiringModel config = new ThLigitingWiringModel();
                config.loopType = model.loopType;
                config.layerType = model.layerType;
                config.pointNum = model.pointNum;
                thLigitingWirings.Add(config);
            }
            return thLigitingWirings;
        }
    }
}
