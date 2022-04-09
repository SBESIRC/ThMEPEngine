using System;
using ThCADExtension;
using System.ComponentModel;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Helpers
{
    public class ThPDSBreakerCustomTypeDescriptor : CustomTypeDescriptor
    {
        private const string RCDType = "RCD类型";
        private const string ResidualCurrent = "剩余电流动作";
        private readonly ThPDSBreakerExModel _breaker;

        public ThPDSBreakerCustomTypeDescriptor(ICustomTypeDescriptor parent, object instance) : base(parent)
        {
            _breaker = (ThPDSBreakerExModel)instance;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            var coll = base.GetProperties();
            if (_breaker.Type.GetEnumName<ComponentType>() == ComponentType.CB)
            {
                coll.Remove(coll.Find(RCDType, false));
                coll.Remove(coll.Find(ResidualCurrent, false));
            }
            return coll;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var coll = base.GetProperties(attributes);
            if (_breaker.Type.GetEnumName<ComponentType>() == ComponentType.CB)
            {
                coll.Remove(coll.Find(RCDType, false));
                coll.Remove(coll.Find(ResidualCurrent, false));
            }
            return coll;
        }
    }
}
