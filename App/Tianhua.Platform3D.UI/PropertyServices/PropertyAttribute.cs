using System;
using System.Collections.Generic;
using System.Linq;

namespace Tianhua.Platform3D.UI.PropertyServices
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PropertyAttribute : Attribute
    {
        public string TypeName { get; private set; }
        public string Tag { get; private set; }
        static List<Type> _placementTypesCache = null;
        static object _locker = new object();
        public PropertyAttribute(string typeName, string tag)
        {
            this.TypeName = typeName;
            this.Tag = tag;
        }
        public static List<Type> GetAvailabilityTypes(string assembly)
        {
            lock (_locker)
            {
                var ass = System.Reflection.Assembly.LoadFrom(assembly);
                _placementTypesCache = ass.GetTypes().
                    Where(o =>
                        o.GetInterfaces().Contains(typeof(ITHProperty)) &&
                        o.GetCustomAttributes(typeof(PropertyAttribute), false).Length > 0).
                        ToList();

            }
            return _placementTypesCache;
        }
    }
}
