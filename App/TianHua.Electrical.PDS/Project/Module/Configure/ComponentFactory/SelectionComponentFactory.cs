using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory
{
    /// <summary>
    /// (选型)元器件工厂
    /// </summary>
    public class SelectionComponentFactory : PDSBaseComponentFactory
    {
        private ThPDSProjectGraphEdge _edge;
        private double _calculateCurrent;//计算电流
        private double _cascadeCurrent;//获取Target级联电流
        private double _maxCalculateCurrent;//获取Target级联电流
        private string _polesNum;//级数
        private string _specialPolesNum;//特殊元器件级数
        private string _ouvpPolesNum;//OUVP元器件级数
        private string _characteristics;//瞬时脱扣器类型
        private List<string> _tripDevice;//脱扣器类型
        public SelectionComponentFactory(ThPDSProjectGraphEdge edge)
        {
            this._edge = edge;
            _calculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
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
            _calculateCurrent = miniBusbar.CalculateCurrent;//计算电流
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
            _calculateCurrent = node.Load.CalculateCurrent;//计算电流
            _cascadeCurrent = cascadeCurrent;//额定级联电流
            _maxCalculateCurrent = Math.Max(_calculateCurrent, _cascadeCurrent);
            _polesNum = "3P";//极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            _specialPolesNum = "4P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
            if (node.Load.Phase == ThPDSPhase.一相)
            {
                _polesNum = "1P";
                _ouvpPolesNum = "1P+N";
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
                _ouvpPolesNum = "3P+N";
                if (node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                {
                    _specialPolesNum = "3P";
                }
            }
        }

        public override Breaker CreatBreaker()
        {
            return new Breaker(_maxCalculateCurrent, _tripDevice, _polesNum, _characteristics, _edge.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.DomesticWaterPump, false);
        }

        public override Conductor CreatConductor()
        {
            return new Conductor(_calculateCurrent, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.LoadTypeCat_1, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber);
        }

        public Conductor GetSecondaryCircuitConductor(SecondaryCircuitInfo secondaryCircuitInfo)
        {
            return new Conductor(secondaryCircuitInfo.Conductor, secondaryCircuitInfo.ConductorCategory, _edge.Target.Load.Phase, _edge.Target.Load.CircuitType, _edge.Target.Load.FireLoad, _edge.Circuit.ViaConduit, _edge.Circuit.ViaCableTray, _edge.Target.Load.Location.FloorNumber);
        }

        public override Contactor CreatContactor()
        {
            return new Contactor(_calculateCurrent, _polesNum);
        }

        public override CPS CreatCPS()
        {
            return new CPS(_calculateCurrent, _edge.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.DomesticWaterPump);
        }
        public override Meter CreatMeterTransformer()
        {
            if(_calculateCurrent < 100)
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
            return new IsolatingSwitch(_calculateCurrent, _specialPolesNum);
        }

        public override AutomaticTransferSwitch CreatAutomaticTransferSwitch()
        {
            return new AutomaticTransferSwitch(_calculateCurrent, _specialPolesNum);
        }

        public override ManualTransferSwitch CreatManualTransferSwitch()
        {
            return new ManualTransferSwitch(_calculateCurrent, _specialPolesNum);
        }

        public override Breaker CreatResidualCurrentBreaker()
        {
            return new Breaker(_maxCalculateCurrent, _tripDevice, _specialPolesNum, _characteristics, _edge.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.DomesticWaterPump, true);
        }

        public override ThermalRelay CreatThermalRelay()
        {
            return new ThermalRelay(_calculateCurrent);
        }

        public override OUVP CreatOUVP()
        {
            return new OUVP(_calculateCurrent, _ouvpPolesNum);
        }
    }
}
