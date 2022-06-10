using System;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPTCH.TCHXmlDataConvert
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TCHConvertAttribute:Attribute
    {
        public string AttributName { get; }
        static object _locker = new object();
        static List<Type> _placementTypesCache = null;

        public TCHConvertAttribute(string name)
        {
            this.AttributName = name;
        }
        public static List<Type> GetAvailabilityTypes(string assembly)
        {
            lock (_locker)
            {
                var ass = System.Reflection.Assembly.LoadFrom(assembly);
                _placementTypesCache = ass.GetTypes().
                    Where(o =>
                        o.GetInterfaces().Contains(typeof(ITCHXmlConvert)) &&
                        o.GetCustomAttributes(typeof(TCHConvertAttribute), false).Length > 0).
                        ToList();

            }
            return _placementTypesCache;
        }
    }
}
