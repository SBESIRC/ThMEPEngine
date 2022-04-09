using System.ComponentModel;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Helpers
{
    public class ThPDSBreakerCustomTypeDescriptor : CustomTypeDescriptor
    {
        private readonly ThPDSBreakerExModel _breaker;

        public ThPDSBreakerCustomTypeDescriptor(ICustomTypeDescriptor parent, object instance): base(parent)
        {
            _breaker = (ThPDSBreakerExModel)instance;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return base.GetProperties();
        }
    }
}
