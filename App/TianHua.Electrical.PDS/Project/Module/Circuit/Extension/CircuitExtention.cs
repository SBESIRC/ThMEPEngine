using System;
using System.Collections.Generic;
using System.Reflection;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
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

        public static bool Contains(this PDSBaseInCircuit circuit, PDSBaseComponent component)
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

        public static bool SetCircuitComponentValue(this PDSBaseOutCircuit circuit, PDSBaseComponent component, PDSBaseComponent value)
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
                        prop.SetValue(circuit, value);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool SetCircuitComponentValue(this PDSBaseInCircuit circuit, PDSBaseComponent component, PDSBaseComponent value)
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
                        prop.SetValue(circuit, value);
                        return true;
                    }
                }
            }
            return false;
        }

        public static List<Breaker> GetCircuitBreakers(this PDSBaseOutCircuit circuit)
        {
            List<Breaker> result = new List<Breaker>();
            foreach (PropertyInfo prop in circuit.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(Breaker))
                {
                    object oValue = prop.GetValue(circuit);
                    if (oValue.IsNull())
                        continue;
                    result.Add(oValue as Breaker);
                }
            }
            return result;
        }
    }
}
