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
        private bool _IsEmptyLoad;//是否是空负载
        private double _lowPower;//单速功率/双速低速功率
        private double _highPower;//双速高速功率
        private string _characteristics;//瞬时脱扣器类型
        private List<string> _tripDevice;//脱扣器类型
        private double _calculateCurrent;//计算电流
        private double _calculateCurrentMagnification;//计算电流
        private string _polesNum;//级数

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
            _IsEmptyLoad = edge.Target.Type==PDSNodeType.Empty;
            _isDualPower = edge.Target.Details.IsDualPower;
            _isFireLoad = edge.Target.Load.FireLoad;
            _lowPower = edge.Target.Details.LowPower;
            _highPower = edge.Target.Details.HighPower;
            _calculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
            _calculateCurrentMagnification = edge.Target.Load.CalculateCurrent * PDSProject.Instance.projectGlobalConfiguration.CalculateCurrentMagnification;//计算电流放大倍数
            _polesNum = "3P"; //极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            if (edge.Target.Load.Phase == ThPDSPhase.一相)
            {
                _polesNum = "1P";
            }
            _characteristics = "";//瞬时脱扣器类型
            _tripDevice = edge.Target.Load.LoadTypeCat_1.GetTripDevice(edge.Target.Load.FireLoad, out _characteristics);//脱扣器类型
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
        /// 获取消防应急照明回路信息
        /// </summary>
        /// <returns></returns>
        public FireEmergencyLighting GetFireEmergencyLighting()
        {
            var CircuitForm = new FireEmergencyLighting();
            CircuitForm.Conductor = CreatFireEmergencyLightingConductor();
            return CircuitForm;
        }

        /// <summary>
        /// 获取电动机-分立元件回路信息
        /// </summary>
        /// <param name="type"></param>
        public Motor_DiscreteComponentsCircuit GetDiscreteComponentsCircuit()
        {
            var CircuitForm = new Motor_DiscreteComponentsCircuit();
            var configs = _isFireLoad ? MotorConfiguration.Fire_DiscreteComponentsInfos : MotorConfiguration.NonFire_DiscreteComponentsInfos;
            var config = configs.First(o => o.InstalledCapacity > _highPower);
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
            var config = configs.First(o => o.InstalledCapacity > _highPower);
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
            var config = configs.First(o => o.InstalledCapacity > _highPower);
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
            var config = configs.First(o => o.InstalledCapacity > _highPower);
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
            _contactorConfig = highConfig.QAC;
            CircuitForm.contactor = CreatContactor();
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

        /// <summary>
        /// 获取上海住宅回路配置
        /// </summary>
        /// <returns></returns>
        public DistributionMetering_ShanghaiMTCircuit GetShanghaiMTCircuit()
        {
            var DistributionMeteringConfigs = DistributionMeteringConfiguration.ShanghaiResidential;
            var config = DistributionMeteringConfigs.FirstOrDefault(o => o.HighPower >= _highPower );
            if(config.IsNull() || config.Phase != _edge.Target.Load.Phase)
            {
                return null;
            }
            var CircuitForm = new DistributionMetering_ShanghaiMTCircuit();
            CircuitForm.breaker1 = CreatBreaker(config.CB1);
            CircuitForm.meter = CreatMeterTransformer(config.MT);
            CircuitForm.breaker2 = CreatBreaker(config.CB2);
            CircuitForm.Conductor = CreatConductor(config.Conductor);
            return CircuitForm;
        }

        /// <summary>
        /// 获取江苏住宅回路配置
        /// </summary>
        /// <returns></returns>
        public DistributionMetering_MTInFrontCircuit GetMTInFrontCircuit()
        {
            var DistributionMeteringConfigs = DistributionMeteringConfiguration.JiangsuResidential;
            var config = DistributionMeteringConfigs.FirstOrDefault(o => o.HighPower >= _highPower);
            if (config.IsNull() || config.Phase != _edge.Target.Load.Phase)
            {
                return null;
            }
            var CircuitForm = new DistributionMetering_MTInFrontCircuit();
            CircuitForm.meter = CreatMeterTransformer();
            CircuitForm.breaker = CreatBreaker(config.CB);
            CircuitForm.Conductor = CreatConductor(config.Conductor);
            return CircuitForm;
        }

        public override Breaker CreatBreaker()
        {
            return new Breaker(_breakerConfig);
        }

        private Breaker CreatBreaker(List<string> config)
        {
            return new Breaker(config, _tripDevice, _characteristics);
        }

        public override Conductor CreatConductor()
        {
            if (_IsEmptyLoad)
                return null;
            return new Conductor(_conductorConfig, _highPower, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber, _edge.Target.Load.CableLayingMethod1, _edge.Target.Load.CableLayingMethod2);
        }

        private Conductor CreatConductor(string config)
        {
            return new Conductor(_conductorConfig, _highPower, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber, _edge.Target.Load.CableLayingMethod1, _edge.Target.Load.CableLayingMethod2, _edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel);
        }

        /// <summary>
        /// 创建消防应急照明回路导体
        /// </summary>
        /// <returns></returns>
        private Conductor CreatFireEmergencyLightingConductor()
        {
            if (_IsEmptyLoad)
                return null;
            return new Conductor("2x2.5+E2.5", MaterialStructure.YJY, _highPower, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber, _edge.Target.Load.CableLayingMethod1, _edge.Target.Load.CableLayingMethod1);
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
            if (_calculateCurrent < 100)
            {
                return new MeterTransformer(_calculateCurrent, _polesNum);
            }
            else
            {
                return CreatCurrentTransformer();
            }
        }

        private Meter CreatMeterTransformer(List<string> mT)
        {
            return new MeterTransformer(mT);
        }

        public override CurrentTransformer CreatCurrentTransformer()
        {
            return new CurrentTransformer(_calculateCurrent, _polesNum);
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

        public override Breaker CreatResidualCurrentBreaker()
        {
            throw new NotImplementedException();
        }

        public override OUVP CreatOUVP()
        {
            throw new NotImplementedException();
        }
    }
}
