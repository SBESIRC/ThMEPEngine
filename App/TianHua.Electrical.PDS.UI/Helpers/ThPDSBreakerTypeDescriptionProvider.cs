using System;
using System.ComponentModel;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Helpers
{
    // https://www.codeproject.com/articles/26992/using-a-typedescriptionprovider-to-support-dynamic#_articleTop
    public class ThPDSBreakerTypeDescriptionProvider : TypeDescriptionProvider
    {
        private static TypeDescriptionProvider defaultTypeProvider =
               TypeDescriptor.GetProvider(typeof(ThPDSBreakerExModel));

        public ThPDSBreakerTypeDescriptionProvider() : base(defaultTypeProvider)
        {
            //
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            ICustomTypeDescriptor defaultDescriptor =
                                  base.GetTypeDescriptor(objectType, instance);

            return instance == null ? defaultDescriptor :
                new ThPDSBreakerCustomTypeDescriptor(defaultDescriptor, instance);
        }
    }
}
