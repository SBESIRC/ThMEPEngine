using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using ThMEPElectrical.Model;
using ThMEPElectrical.ViewModel;

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

        /// <summary>
        /// 火灾报警
        /// </summary>
        public List<ThFireAlarmModel> fireAlarmParameter = ConvertToModel(new WiringConnectingViewModel().configLst);

        public static List<ThFireAlarmModel> ConvertToModel(ObservableCollection<LoopConfig> configLst)
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
