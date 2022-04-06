using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory
{
    /// <summary>
    /// 元器件生产工厂
    /// </summary>
    public abstract class PDSBaseComponentFactory
    {
        public abstract Breaker CreatBreaker();
        public abstract ResidualCurrentBreaker CreatResidualCurrentBreaker();
        public abstract ThermalRelay CreatThermalRelay();
        public abstract Contactor CreatContactor();
        public abstract CPS CreatCPS();
        public abstract Meter CreatMeterTransformer();
        public abstract CurrentTransformer CreatCurrentTransformer();
        public abstract IsolatingSwitch CreatIsolatingSwitch();
        public abstract AutomaticTransferSwitch CreatAutomaticTransferSwitch(); 
        public abstract ManualTransferSwitch CreatManualTransferSwitch();
        public abstract Conductor CreatConductor();
    }
}
