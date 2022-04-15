using System;
using System.Reflection;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.Extension
{
    public static class CascadeCircuitExtension
    {
        public static double GetCascadeCurrent(this PDSBaseOutCircuit outCircuit)
        {
            double resultValue = 0;
            if (outCircuit.IsNull())
                return resultValue;
            Type type = outCircuit.GetType();
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(PDSBaseComponent) ||prop.PropertyType.IsSubclassOf(typeof(PDSBaseComponent)))
                {
                    object oValue = prop.GetValue(outCircuit);
                    if (oValue.IsNull())
                        continue;
                    PDSBaseComponent component = (PDSBaseComponent)oValue;
                    resultValue =System.Math.Max(resultValue, component.GetCascadeCurrent());
                }
            }
            return resultValue;
        }

        public static double GetCascadeCurrent(this PDSBaseInCircuit inCircuit)
        {
            double resultValue = 0;
            if (inCircuit.IsNull())
                return resultValue;
            Type type = inCircuit.GetType();
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(PDSBaseComponent) ||prop.PropertyType.IsSubclassOf(typeof(PDSBaseComponent)))
                {
                    object oValue = prop.GetValue(inCircuit);
                    if (oValue.IsNull())
                        continue;
                    PDSBaseComponent component = (PDSBaseComponent)oValue;
                    resultValue =System.Math.Max(resultValue, component.GetCascadeCurrent());
                }
            }
            return resultValue;
        }

        public static double GetCascadeCurrent(this MiniBusbar miniBusbar)
        {
            double resultValue = 0;
            if (miniBusbar.IsNull())
                return resultValue;
            Type type = miniBusbar.GetType();
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(PDSBaseComponent) ||prop.PropertyType.IsSubclassOf(typeof(PDSBaseComponent)))
                {
                    object oValue = prop.GetValue(miniBusbar);
                    if (oValue.IsNull())
                        continue;
                    PDSBaseComponent component = (PDSBaseComponent)oValue;
                    resultValue =System.Math.Max(resultValue, component.GetCascadeCurrent());
                }
            }
            return resultValue;
        }

        public static double GetCascadeCurrent(this PDSBaseComponent component)
        {
            if (component.IsCascadeComponent())
            {
                var value = component.GetCascadeRatedCurrent();
                return value;
            }
            else
            {
                return 0;
            }
        }
    }
}
