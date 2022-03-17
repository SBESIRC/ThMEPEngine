using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    public static class CascadeComponentExtension
    {
        public static bool IsCascadeComponent(this PDSBaseComponent component)
        {
            if(component.IsNull())
                return false;
            return component.GetType().IsCascadeComponent();
        }

        public static bool IsCascadeComponent(this Type type)
        {
            return type.IsDefined(typeof(CascadeComponentAttribute), false);
        }
    }
}
