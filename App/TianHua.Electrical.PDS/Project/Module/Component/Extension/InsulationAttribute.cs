using System;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InsulationAttribute : Attribute
    {
        public InsulationType InsulationType { get; set; }

        /// <summary>
        /// 适用范围
        /// </summary>
        public InsulationAttribute(InsulationType type)
        {
            InsulationType = type;
        }
    }

    public enum InsulationType
    {
        XLPE,
        PVC,
        InorganicMaterial,
    }
}
