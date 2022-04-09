using System;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    public static class ComponentTypeSelector
    {
        public static Type GetComponentType(this ComponentType component)
        {
            switch (component)
            {
                case ComponentType.QL:
                    return typeof(IsolatingSwitch);
                case ComponentType.QAC:
                    return typeof(Contactor);
                case ComponentType.KH:
                    return typeof(ThermalRelay);
                case ComponentType.CB:
                    return typeof(Breaker);
                case ComponentType.一体式RCD:
                    return typeof(Breaker);
                case ComponentType.组合式RCD:
                    return typeof(Breaker);
                case ComponentType.ATSE:
                    return typeof(AutomaticTransferSwitch);
                case ComponentType.MTSE:
                    return typeof(ManualTransferSwitch);
                case ComponentType.MT:
                    return typeof(MeterTransformer);
                case ComponentType.CT:
                    return typeof(CurrentTransformer);
                case ComponentType.CPS:
                    return typeof(CPS);
                case ComponentType.FU:
                    throw new NotImplementedException();
                case ComponentType.SPD:
                    throw new NotImplementedException();
                case ComponentType.SS:
                    throw new NotImplementedException();
                case ComponentType.FC:
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
