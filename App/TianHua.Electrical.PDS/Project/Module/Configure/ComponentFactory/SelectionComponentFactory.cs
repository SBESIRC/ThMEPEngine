using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory
{
    /// <summary>
    /// (选型)元器件工厂
    /// </summary>
    public class SelectionComponentFactory : PDSBaseComponentFactory
    {
        private ThPDSProjectGraphEdge _edge;
        private double _calculateCurrent;//计算电流
        private double _calculateCurrentMagnification;//计算电流
        private double _cascadeCurrent;//获取Target级联电流
        private double _maxCalculateCurrent;//获取Target级联电流
        private string _polesNum;//级数
        private string _specialPolesNum;//特殊元器件级数
        private string _ouvpPolesNum;//OUVP元器件级数
        private string _characteristics;//瞬时脱扣器类型
        private List<string> _tripDevice;//脱扣器类型
        private bool _isLeakageProtection;//是否是生活水泵
        private bool _IsEmptyLoad;//是否是空负载
        public SelectionComponentFactory(ThPDSProjectGraphEdge edge)
        {
            this._edge = edge;
            _IsEmptyLoad = edge.Target.Type==PDSNodeType.Empty;
            _isLeakageProtection = _edge.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.DomesticWaterPump;
            _calculateCurrent = edge.Target.Details.LoadCalculationInfo.HighCalculateCurrent;//计算电流
            _calculateCurrentMagnification = edge.Target.Details.LoadCalculationInfo.HighCalculateCurrent * PDSProject.Instance.projectGlobalConfiguration.CalculateCurrentMagnification;//计算电流放大倍数
            _cascadeCurrent = edge.Target.Details.CascadeCurrent;//额定级联电流
            _maxCalculateCurrent = Math.Max(_calculateCurrent, _cascadeCurrent);
            _polesNum = "3P"; //极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            _specialPolesNum = "3P+N"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
            if (edge.Target.Load.Phase == ThPDSPhase.一相)
            {
                _polesNum = "1P";
                if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.OutdoorLights)
                {
                    _specialPolesNum = "1P";
                }
                else
                {
                    _specialPolesNum = "1P+N";
                }
            }
            _characteristics = "";//瞬时脱扣器类型
            _tripDevice = edge.Target.Load.LoadTypeCat_1.GetTripDevice(edge.Target.Load.FireLoad, out _characteristics);//脱扣器类型
        }

        public SelectionComponentFactory(ThPDSProjectGraphNode node, MiniBusbar miniBusbar, double cascadeCurrent)
        {
            _isLeakageProtection = node.Details.MiniBusbars[miniBusbar].Any(o => o.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.DomesticWaterPump);
            _calculateCurrent = miniBusbar.CalculateCurrent;//计算电流
            _calculateCurrentMagnification = miniBusbar.CalculateCurrent * PDSProject.Instance.projectGlobalConfiguration.CalculateCurrentMagnification;//计算电流放大倍数
            _cascadeCurrent = cascadeCurrent;//额定级联电流
            _maxCalculateCurrent = Math.Max(_calculateCurrent, _cascadeCurrent);
            _polesNum = "3P"; //极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            _specialPolesNum = "3P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
            if (miniBusbar.Phase == ThPDSPhase.一相)
            {
                _polesNum = "1P";
                _specialPolesNum = "2P";
            }
            _characteristics = "";//瞬时脱扣器类型
            _tripDevice = ThPDSLoadTypeCat_1.LumpedLoad.GetTripDevice(node.Details.MiniBusbars[miniBusbar].Any(o => o.Target.Load.FireLoad), out _characteristics);//脱扣器类型
        }

        public SelectionComponentFactory(ThPDSProjectGraphNode node, double cascadeCurrent)
        {
            _calculateCurrent = node.Details.LoadCalculationInfo.HighCalculateCurrent;//计算电流
            _calculateCurrentMagnification = node.Details.LoadCalculationInfo.HighCalculateCurrent * PDSProject.Instance.projectGlobalConfiguration.CalculateCurrentMagnification;//计算电流放大倍数
            _cascadeCurrent = cascadeCurrent;//额定级联电流
            _maxCalculateCurrent = Math.Max(_calculateCurrent, _cascadeCurrent);
            _polesNum = "3P";//极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            _specialPolesNum = "4P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
            if (node.Load.Phase == ThPDSPhase.一相)
            {
                _polesNum = "1P";
                _ouvpPolesNum = "2P";
                //当相数为1时，若负载类型不为“Outdoor Lights”，且断路器不是ATSE前的主进线开关，则断路器选择1P；
                //当相数为1时，若负载类型为“Outdoor Lights”，或断路器是ATSE前的主进线开关，则断路器选择2P；
                if (node.Load.LoadTypeCat_2 != ThPDSLoadTypeCat_2.OutdoorLights && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                {
                    _specialPolesNum = "1P";
                }
                else
                {
                    _specialPolesNum = "2P";
                }
            }
            else if (node.Load.Phase == ThPDSPhase.三相)
            {
                _ouvpPolesNum = "4P";
                if (node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                {
                    _specialPolesNum = "4P";
                }
            }
            _characteristics = "";//瞬时脱扣器类型
            _tripDevice = node.Load.LoadTypeCat_1.GetTripDevice(node.Load.FireLoad, out _characteristics);//脱扣器类型
        }

        public override Breaker CreatBreaker()
        {
            Breaker breaker;
            try
            {
                breaker = new Breaker(_maxCalculateCurrent, _tripDevice, _polesNum, _characteristics, _isLeakageProtection, false);
            }
            catch (NotFoundComponentException)
            {
                breaker = new Breaker(_calculateCurrent, _tripDevice, _polesNum, _characteristics, _isLeakageProtection, false);
            }
            catch
            {
                throw;
            }
            if (breaker.GetCascadeRatedCurrent() < _calculateCurrentMagnification)
            {
                var ratedCurrent = breaker.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
                breaker.SetRatedCurrent(ratedCurrent);
            }
            _maxCalculateCurrent =  Math.Max(_maxCalculateCurrent, breaker.GetCascadeRatedCurrent());
            return breaker;
        }

        /// <summary>
        /// 创建二级断路器
        /// </summary>
        /// <param name="breaker"></param>
        /// <returns></returns>
        public Breaker CreatBreaker(Breaker primaryBreaker)
        {
            var maxCalculateCurrent = Math.Max(primaryBreaker.GetCascadeRatedCurrent(), _maxCalculateCurrent);
            var breaker = new Breaker(maxCalculateCurrent, _tripDevice, _polesNum, _characteristics, _isLeakageProtection, false);
            var ratedCurrent = breaker.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            breaker.SetRatedCurrent(ratedCurrent);
            _maxCalculateCurrent =  Math.Max(_maxCalculateCurrent, breaker.GetCascadeRatedCurrent());
            return breaker;
        }

        public override Conductor CreatConductor()
        {
            if(_IsEmptyLoad)
                return null;
            try
            {
                var conductor = new Conductor(_maxCalculateCurrent, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber, _edge.Target.Load.CableLayingMethod1, _edge.Target.Load.CableLayingMethod2, _edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel);
                return conductor;
            }
            catch (NotFoundComponentException)
            {
                var conductor = new Conductor(_calculateCurrent, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber, _edge.Target.Load.CableLayingMethod1, _edge.Target.Load.CableLayingMethod2, _edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel);
                return conductor;
            }
            catch
            {
                throw;
            }
        }

        public Conductor GetSecondaryCircuitConductor(SecondaryCircuitInfo secondaryCircuitInfo)
        {
            return new Conductor(secondaryCircuitInfo.Conductor, secondaryCircuitInfo.ConductorCategory, _edge.Target.Load.Phase, ThPDSCircuitType.Control, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber, LayingSite.CC, LayingSite.None);
        }

        public override Contactor CreatContactor()
        {
            var contactor = new Contactor(_calculateCurrent, _polesNum);
            var ratedCurrent = contactor.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            contactor.SetRatedCurrent(ratedCurrent);
            return contactor;
        }

        public override CPS CreatCPS()
        {
            return new CPS(_calculateCurrent, _isLeakageProtection);
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

        public override CurrentTransformer CreatCurrentTransformer()
        {
            return new CurrentTransformer(_calculateCurrent, _polesNum);
        }

        public override IsolatingSwitch CreatIsolatingSwitch()
        {
            var isolatingSwitch =  new IsolatingSwitch(_calculateCurrent, _specialPolesNum);
            var ratedCurrent = isolatingSwitch.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            isolatingSwitch.SetRatedCurrent(ratedCurrent);
            return isolatingSwitch;
        }

        public IsolatingSwitch CreatOneWayIsolatingSwitch()
        {
            var isolatingSwitch = new IsolatingSwitch(_calculateCurrent, _polesNum);
            var ratedCurrent = isolatingSwitch.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            isolatingSwitch.SetRatedCurrent(ratedCurrent);
            return isolatingSwitch;
        }

        public override AutomaticTransferSwitch CreatAutomaticTransferSwitch()
        {
            var ATSE = new AutomaticTransferSwitch(_calculateCurrent, _specialPolesNum);
            var ratedCurrent = ATSE.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            ATSE.SetRatedCurrent(ratedCurrent);
            return ATSE;
        }

        public override ManualTransferSwitch CreatManualTransferSwitch()
        {
            var MTSE = new ManualTransferSwitch(_calculateCurrent, _specialPolesNum);
            var ratedCurrent = MTSE.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            MTSE.SetRatedCurrent(ratedCurrent);
            return MTSE;
        }

        public override Breaker CreatResidualCurrentBreaker()
        {
            var breaker = new Breaker(_maxCalculateCurrent, _tripDevice, _specialPolesNum, _characteristics, _isLeakageProtection, true);
            var ratedCurrent = breaker.GetRatedCurrents().First(o => double.Parse(o) > _calculateCurrentMagnification);
            breaker.SetRatedCurrent(ratedCurrent);
            _maxCalculateCurrent =  Math.Max(_maxCalculateCurrent, breaker.GetCascadeRatedCurrent());
            return breaker;
        }

        public override ThermalRelay CreatThermalRelay()
        {
            var thermalRelay = new ThermalRelay(_calculateCurrentMagnification);
            return thermalRelay;
        }

        public override OUVP CreatOUVP()
        {
            var ouvp = new OUVP(_calculateCurrent, _ouvpPolesNum);
            var ratedCurrent = ouvp.GetRatedCurrents().First(o => o > _calculateCurrentMagnification);
            ouvp.SetRatedCurrent(ratedCurrent);
            return ouvp;
        }
    }
}
