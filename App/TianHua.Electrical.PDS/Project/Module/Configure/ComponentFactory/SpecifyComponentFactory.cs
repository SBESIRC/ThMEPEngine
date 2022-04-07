using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Circuit;

namespace TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory
{
    /// <summary>
    /// (指定)元器件工厂
    /// </summary>
    public class SpecifyComponentFactory : PDSBaseComponentFactory
    {
        private ThPDSProjectGraphEdge _edge;
        private bool _isDualPower;//是否双功率
        private bool _isFireLoad;//是否是消防负载
        private double _lowPower;//单速功率/双速低速功率
        private double _highPower;//双速高速功率

        /// <summary>
        /// 断路器配置
        /// </summary>
        private string _breakerConfig;
        /// <summary>
        /// 导体配置
        /// </summary>
        private string _conductorConfig;
        /// <summary>
        /// 接触器配置
        /// </summary>
        private string _contactorConfig;
        /// <summary>
        /// CPS配置
        /// </summary>
        private string _cPSConfig;
        /// <summary>
        /// 热继电器配置
        /// </summary>
        private string _thermalRelayConfig;
        public SpecifyComponentFactory(ThPDSProjectGraphEdge edge)
        {
            this._edge = edge;
            _isDualPower = edge.Target.Details.IsDualPower;
            _isFireLoad = edge.Target.Load.FireLoad;
            _lowPower = edge.Target.Details.LowPower;
            _highPower = edge.Target.Details.HighPower;
        }

        public PDSBaseOutCircuit GetMotorCircuit(Type type)
        {
            if (type == typeof(Motor_DiscreteComponentsCircuit))
                return GetDiscreteComponentsCircuit();
            else if (type == typeof(Motor_CPSCircuit))
                return GetCPSCircuit();
            else if (type == typeof(Motor_DiscreteComponentsStarTriangleStartCircuit))
                return GetDiscreteComponentsStarTriangleStartCircuit();
            else if (type == typeof(Motor_CPSStarTriangleStartCircuit))
                return GetCPSStarTriangleStartCircuit();
            else if (type == typeof(TwoSpeedMotor_CPSYYCircuit))
                return GetTwoSpeedMotorCPSYYCircuit();
            else if (type == typeof(TwoSpeedMotor_CPSDYYCircuit))
                return GetTwoSpeedMotorCPSDYYCircuit();
            else if (type == typeof(TwoSpeedMotor_DiscreteComponentsYYCircuit))
                return GetTwoSpeedMotorDiscreteComponentsYYCircuit();
            else if (type == typeof(TwoSpeedMotor_DiscreteComponentsDYYCircuit))
                return GetTwoSpeedMotorDiscreteComponentsDYYCircuit();
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// 获取电动机-分立元件回路信息
        /// </summary>
        /// <param name="type"></param>
        public Motor_DiscreteComponentsCircuit GetDiscreteComponentsCircuit()
        {
            var CircuitForm = new Motor_DiscreteComponentsCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_DiscreteComponentsInfos : MotorConfiguration.NonFire_DiscreteComponentsInfos;
            var config = configs.First(o => o.InstalledCapacity > _lowPower);
            _breakerConfig = config.CB;
            _contactorConfig = config.QAC;
            _thermalRelayConfig = config.KH;
            _conductorConfig = config.Conductor;
            CircuitForm.breaker = CreatBreaker();
            CircuitForm.contactor = CreatContactor();
            CircuitForm.thermalRelay = CreatThermalRelay();
            CircuitForm.Conductor = CreatConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取电动机-CPS回路信息
        /// </summary>
        /// <returns></returns>
        public Motor_CPSCircuit GetCPSCircuit()
        {
            var CircuitForm = new Motor_CPSCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_CPSInfos : MotorConfiguration.NonFire_CPSInfos;
            var config = configs.First(o => o.InstalledCapacity > _lowPower);
            _cPSConfig = config.CPS;
            _conductorConfig = config.Conductor;
            CircuitForm.cps = CreatCPS();
            CircuitForm.Conductor = CreatConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取电动机-分立元件星三角启动回路
        /// </summary>
        /// <returns></returns>
        public Motor_DiscreteComponentsStarTriangleStartCircuit GetDiscreteComponentsStarTriangleStartCircuit()
        {
            var CircuitForm = new Motor_DiscreteComponentsStarTriangleStartCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_DiscreteComponentsStarTriangleStartInfos : MotorConfiguration.NonFire_DiscreteComponentsStarTriangleStartInfos;
            var config = configs.First(o => o.InstalledCapacity > _lowPower);
            _breakerConfig = config.CB;
            CircuitForm.breaker = CreatBreaker();
            _contactorConfig = config.QAC1;
            CircuitForm.contactor1 = CreatContactor();
            _thermalRelayConfig = config.KH;
            CircuitForm.thermalRelay = CreatThermalRelay();
            _contactorConfig = config.QAC2;
            CircuitForm.contactor2 = CreatContactor();
            _contactorConfig = config.QAC3;
            CircuitForm.contactor3 = CreatContactor();
            _conductorConfig = config.Conductor1;
            CircuitForm.Conductor1 = CreatConductor();
            _conductorConfig = config.Conductor2;
            CircuitForm.Conductor2 = CreatConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取电动机-CPS回路信息
        /// </summary>
        /// <returns></returns>
        public Motor_CPSStarTriangleStartCircuit GetCPSStarTriangleStartCircuit()
        {
            var CircuitForm = new Motor_CPSStarTriangleStartCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_CPSStarTriangleStartInfos : MotorConfiguration.NonFire_CPSStarTriangleStartInfos;
            var config = configs.First(o => o.InstalledCapacity > _lowPower);
            _cPSConfig = config.CPS;
            CircuitForm.cps = CreatCPS();
            _contactorConfig = config.QAC1;
            CircuitForm.contactor1 = CreatContactor();
            _contactorConfig = config.QAC2;
            CircuitForm.contactor2 = CreatContactor();
            _conductorConfig = config.Conductor1;
            CircuitForm.Conductor1 = CreatConductor();
            _conductorConfig = config.Conductor2;
            CircuitForm.Conductor2 = CreatConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取双速电动机-CPSY-Y回路信息
        /// </summary>
        /// <returns></returns>
        public TwoSpeedMotor_CPSYYCircuit GetTwoSpeedMotorCPSYYCircuit()
        {
            //if(!_isDualPower)
            //{
            //    //不是双功率无法切换成双速电动机回路
            //    throw new NotSupportedException();
            //}
            var CircuitForm = new TwoSpeedMotor_CPSYYCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_TwoSpeedMotor_CPSInfos : MotorConfiguration.NonFire_TwoSpeedMotor_CPSInfos;
            var lowConfig = configs.First(o => o.InstalledCapacity > _lowPower);
            var highConfig = configs.First(o => o.InstalledCapacity > _highPower);
            _cPSConfig = lowConfig.CPS;
            CircuitForm.cps1 = CreatCPS();
            _conductorConfig = lowConfig.Conductor1;
            CircuitForm.conductor1 = CreatConductor();
            _cPSConfig = highConfig.CPS;
            CircuitForm.cps2 = CreatCPS();
            _conductorConfig = highConfig.Conductor2;
            CircuitForm.conductor2 = CreatConductor();
            _contactorConfig = highConfig.QAC;
            CircuitForm.contactor = CreatContactor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取双速电动机-CPSD-YY回路信息
        /// </summary>
        /// <returns></returns>
        public TwoSpeedMotor_CPSDYYCircuit GetTwoSpeedMotorCPSDYYCircuit()
        {
            //if (!_isDualPower)
            //{
            //    //不是双功率无法切换成双速电动机回路
            //    throw new NotSupportedException();
            //}
            var CircuitForm = new TwoSpeedMotor_CPSDYYCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_TwoSpeedMotor_CPSInfos : MotorConfiguration.NonFire_TwoSpeedMotor_CPSInfos;
            var lowConfig = configs.First(o => o.InstalledCapacity > _lowPower);
            var highConfig = configs.First(o => o.InstalledCapacity > _highPower);
            _cPSConfig = lowConfig.CPS;
            CircuitForm.cps1 = CreatCPS();
            _conductorConfig = lowConfig.Conductor1;
            CircuitForm.conductor1 = CreatConductor();
            _cPSConfig = highConfig.CPS;
            CircuitForm.cps2 = CreatCPS();
            _conductorConfig = highConfig.Conductor2;
            CircuitForm.conductor2 = CreatConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取双速电动机-分立元件Y-Y回路信息
        /// </summary>
        /// <returns></returns>
        public TwoSpeedMotor_DiscreteComponentsYYCircuit GetTwoSpeedMotorDiscreteComponentsYYCircuit()
        {
            //if (!_isDualPower)
            //{
            //    //不是双功率无法切换成双速电动机回路
            //    throw new NotSupportedException();
            //}
            var twoSpeedMotorConfigs = _isFireLoad ? MotorConfiguration.Fire_TwoSpeedMotor_DiscreteComponentsYYCircuitInfos : MotorConfiguration.Fire_TwoSpeedMotor_DiscreteComponentsYYCircuitInfos;
            var discreteComponentsConfigs = _isFireLoad ? MotorConfiguration.Fire_DiscreteComponentsInfos : MotorConfiguration.NonFire_DiscreteComponentsInfos;
            var lowConfig = discreteComponentsConfigs.First(o => o.InstalledCapacity > _lowPower);
            var highConfig = discreteComponentsConfigs.First(o => o.InstalledCapacity > _highPower);
            var twoSpeedMotorConfig = twoSpeedMotorConfigs.First(o => o.InstalledCapacity > _highPower);

            var CircuitForm = new TwoSpeedMotor_DiscreteComponentsYYCircuit();
            _breakerConfig = highConfig.CB;
            CircuitForm.breaker = CreatBreaker();
            _contactorConfig = lowConfig.QAC;
            CircuitForm.contactor1 = CreatContactor();
            _contactorConfig = highConfig.QAC;
            CircuitForm.contactor2 = CreatContactor();
            _thermalRelayConfig = lowConfig.KH;
            CircuitForm.thermalRelay1 = CreatThermalRelay();
            _thermalRelayConfig = highConfig.KH;
            CircuitForm.thermalRelay2 = CreatThermalRelay();
            _conductorConfig = twoSpeedMotorConfig.Conductor1;
            CircuitForm.conductor1 = CreatConductor();
            _conductorConfig = twoSpeedMotorConfig.Conductor2;
            CircuitForm.conductor2 = CreatConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取双速电动机-分立元件D-YY回路信息
        /// </summary>
        /// <returns></returns>
        public TwoSpeedMotor_DiscreteComponentsDYYCircuit GetTwoSpeedMotorDiscreteComponentsDYYCircuit()
        {
            //if (!_isDualPower)
            //{
            //    //不是双功率无法切换成双速电动机回路
            //    throw new NotSupportedException();
            //}
            var twoSpeedMotorConfigs = _isFireLoad ? MotorConfiguration.Fire_TwoSpeedMotor_DiscreteComponentsDYYCircuitInfos : MotorConfiguration.Fire_TwoSpeedMotor_DiscreteComponentsDYYCircuitInfos;
            var discreteComponentsConfigs = _isFireLoad ? MotorConfiguration.Fire_DiscreteComponentsInfos : MotorConfiguration.NonFire_DiscreteComponentsInfos;
            var lowConfig = discreteComponentsConfigs.First(o => o.InstalledCapacity > _lowPower);
            var highConfig = discreteComponentsConfigs.First(o => o.InstalledCapacity > _highPower);
            var twoSpeedMotorConfig = twoSpeedMotorConfigs.First(o => o.InstalledCapacity > _highPower);

            var CircuitForm = new TwoSpeedMotor_DiscreteComponentsDYYCircuit();
            _breakerConfig = highConfig.CB;
            CircuitForm.breaker = CreatBreaker();
            _contactorConfig = lowConfig.QAC;
            CircuitForm.contactor1 = CreatContactor();
            _contactorConfig = highConfig.QAC;
            CircuitForm.contactor2 = CreatContactor();
            _thermalRelayConfig = lowConfig.KH;
            CircuitForm.thermalRelay1 = CreatThermalRelay();
            _thermalRelayConfig = highConfig.KH;
            CircuitForm.thermalRelay2 = CreatThermalRelay();
            _conductorConfig = twoSpeedMotorConfig.Conductor1;
            CircuitForm.conductor1 = CreatConductor();
            _conductorConfig = twoSpeedMotorConfig.Conductor2;
            CircuitForm.conductor2 = CreatConductor();
            _contactorConfig = twoSpeedMotorConfig.QAC3;
            CircuitForm.contactor3 = CreatContactor();
            return CircuitForm;
        }

        public override Breaker CreatBreaker()
        {
            return new Breaker(_breakerConfig);
        }

        public override Conductor CreatConductor()
        {
            return new Conductor(_conductorConfig, _highPower, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber);
        }

        public override Contactor CreatContactor()
        {
            return new Contactor(_contactorConfig);
        }

        public override CPS CreatCPS()
        {
            return new CPS(_cPSConfig , _edge.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.DomesticWaterPump);
        }

        public override ThermalRelay CreatThermalRelay()
        {
            return new ThermalRelay(_thermalRelayConfig);
        }

        public override Meter CreatMeterTransformer()
        {
            throw new NotImplementedException();
        }

        public override CurrentTransformer CreatCurrentTransformer()
        {
            throw new NotImplementedException();
        }

        public override IsolatingSwitch CreatIsolatingSwitch()
        {
            throw new NotImplementedException();
        }

        public override AutomaticTransferSwitch CreatAutomaticTransferSwitch()
        {
            throw new NotImplementedException();
        }

        public override ManualTransferSwitch CreatManualTransferSwitch()
        {
            throw new NotImplementedException();
        }

        public override ResidualCurrentBreaker CreatResidualCurrentBreaker()
        {
            throw new NotImplementedException();
        }

        public override OUVP CreatOUVP()
        {
            throw new NotImplementedException();
        }
    }
}
