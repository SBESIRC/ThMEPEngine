using System;
using System.Linq;
using System.Reflection;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.Extension
{
    public static class UpdateCircuitTypeExtension
    {
        public static void UpdateCircuit(this ThPDSProjectGraphEdge edge ,PDSBaseOutCircuit circuit, CircuitFormOutType circuitFormOutType)
        {
            var NewType = circuitFormOutType.GetCircuitType();
            if(NewType.EqualsGroup(circuit.GetType()))
            {
                //仅同组的回路才可以来回切换
                if(NewType.GetCircuitGroup() == CircuitGroup.Group1)
                {
                    var newCircuit = (PDSBaseOutCircuit)System.Activator.CreateInstance(NewType);
                    var OriginalComponents = circuit.GetType().GetProperties().Where(prop => prop.PropertyType.IsSubclassOf(typeof(PDSBaseComponent))).Select(prop => prop.GetValue(circuit)).Cast<PDSBaseComponent>().ToList();
                    //获取当前Type下所有的属性上标记的特性
                    var props = NewType.GetProperties();
                    for (int i = 0; i < props.Length; i++)
                    {
                        PropertyInfo prop = props[i];
                        //定义PDSBaseComponent本身就是预留的元器件，不做赋值处理
                        if (prop.PropertyType == typeof(PDSBaseComponent) )
                        {
                            object oValue = prop.GetValue(newCircuit);
                            oValue = null;
                            prop.SetValue(newCircuit, null);
                        }
                        else if(prop.PropertyType.IsSubclassOf(typeof(PDSBaseComponent)))
                        {
                            var MatchingComponent = OriginalComponents.Where(o => o.GetType() == prop.PropertyType).OrderBy(o => o.GetCascadeRatedCurrent()).FirstOrDefault();
                            if(MatchingComponent.IsNull())
                            {
                                prop.SetValue(newCircuit, edge.ComponentSelection(prop.PropertyType, circuitFormOutType));
                            }
                            else
                            {
                                prop.SetValue(newCircuit, MatchingComponent);
                            }
                        }
                    }
                    edge.Details.CircuitForm = newCircuit;
                }
                else
                {
                    SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
                    edge.Details.CircuitForm = specifyComponentFactory.GetMotorCircuit(NewType);
                }
            }
        }

        public static Type GetCircuitType(this CircuitFormOutType circuitFormOutType)
        {
            switch (circuitFormOutType)
            {
                case CircuitFormOutType.常规:
                    {
                        return typeof(RegularCircuit);
                    }
                case CircuitFormOutType.漏电:
                    {
                        return typeof(LeakageCircuit);
                    }
                case CircuitFormOutType.接触器控制:
                    {
                        return typeof(ContactorControlCircuit);
                    }
                case CircuitFormOutType.热继电器保护:
                    {
                        return typeof(ThermalRelayProtectionCircuit);
                    }
                case CircuitFormOutType.配电计量_上海CT:
                    {
                        return typeof(DistributionMetering_ShanghaiCTCircuit);
                    }
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        return typeof(DistributionMetering_ShanghaiMTCircuit);
                    }
                case CircuitFormOutType.配电计量_CT表在前:
                    {
                        return typeof(DistributionMetering_CTInFrontCircuit);
                    }
                case CircuitFormOutType.配电计量_直接表在前:
                    {
                        return typeof(DistributionMetering_MTInFrontCircuit);
                    }
                case CircuitFormOutType.配电计量_CT表在后:
                    {
                        return typeof(DistributionMetering_CTInBehindCircuit);
                    }
                case CircuitFormOutType.配电计量_直接表在后:
                    {
                        return typeof(DistributionMetering_MTInBehindCircuit);
                    }
                case CircuitFormOutType.电动机_分立元件:
                    {
                        return typeof(Motor_DiscreteComponentsCircuit);
                    }
                case CircuitFormOutType.电动机_分立元件星三角启动:
                    {
                        return typeof(Motor_DiscreteComponentsStarTriangleStartCircuit);
                    }
                case CircuitFormOutType.电动机_CPS:
                    {
                        return typeof(Motor_CPSCircuit);
                    }
                case CircuitFormOutType.电动机_CPS星三角启动:
                    {
                        return typeof(Motor_CPSStarTriangleStartCircuit);
                    }
                case CircuitFormOutType.双速电动机_分立元件detailYY:
                    {
                        return typeof(TwoSpeedMotor_DiscreteComponentsDYYCircuit);
                    }
                case CircuitFormOutType.双速电动机_分立元件YY:
                    {
                        return typeof(TwoSpeedMotor_DiscreteComponentsYYCircuit);
                    }
                case CircuitFormOutType.双速电动机_CPSYY:
                    {
                        return typeof(TwoSpeedMotor_CPSYYCircuit);
                    }
                case CircuitFormOutType.双速电动机_CPSdetailYY:
                    {
                        return typeof(TwoSpeedMotor_CPSDYYCircuit);
                    }
                case CircuitFormOutType.消防应急照明回路WFEL:
                    {
                        return typeof(FireEmergencyLighting);
                    }
                default:
                    {
                        //暂时只测试以上回路
                        throw new NotSupportedException();
                    }
            }
        }

        public static bool EqualsGroup(this Type type1,Type type2)
        {
            return type1.GetCircuitGroup().Equals(type2.GetCircuitGroup());
        }

        public static CircuitGroup GetCircuitGroup(this Type type)
        {
            if (type.IsDefined(typeof(CircuitGroupAttribute), false))
            {
                //先判断再获取--为了提高性能
                foreach (Attribute attribute in type.GetCustomAttributes(true))
                {
                    if (attribute is CircuitGroupAttribute circuitGroupAttribute)
                    {
                        return circuitGroupAttribute.groupType;
                    }
                }
            }
            throw new NotSupportedException();
        }
    }
}
