using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.ServiceModels;
using ThMEPLighting.ViewModel;

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
        /// 照明
        /// </summary>
        public List<ThLigitingWiringModel> Parameter = ConvertToLigitingWiringModel(new WiringConnectingViewModel().configLst);

        public static List<ThLigitingWiringModel> ConvertToLigitingWiringModel(ObservableCollection<LoopConfig> configLst)
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

        /// <summary>
        /// 火灾报警
        /// </summary>
        public List<ThFireAlarmModel> AFASParameter = ConvertToFireAlarmModel(new WiringConnectingViewModel().configLst);
        public static List<ThFireAlarmModel> ConvertToFireAlarmModel(ObservableCollection<LoopConfig> configLst)
        {
            List<ThFireAlarmModel> thFireAlarms = new List<ThFireAlarmModel>();
            var fireAlarmModel = configLst.Where(x => x.systemType == "火灾自动报警").FirstOrDefault();
            if (fireAlarmModel == null)
            {
                return thFireAlarms;
            }
            foreach (var model in fireAlarmModel.configModels)
            {
                ThFireAlarmModel config = new ThFireAlarmModel();
                config.loopType = model.loopType;
                config.layerType = model.layerType;
                config.pointNum = model.pointNum;
                thFireAlarms.Add(config);
            }
            return thFireAlarms;
        }
    }
}
