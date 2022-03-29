using System;
using System.Reflection;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.Extension
{
    public static class CircuitExtention
    {
        public static bool Contains(this PDSBaseOutCircuit circuit, PDSBaseComponent component)
        {
            Type type = circuit.GetType();
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(PDSBaseComponent) ||prop.PropertyType.IsSubclassOf(typeof(PDSBaseComponent)))
                {
                    object oValue = prop.GetValue(circuit);
                    if (oValue.IsNull())
                        continue;
                    if (oValue.Equals(component))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
