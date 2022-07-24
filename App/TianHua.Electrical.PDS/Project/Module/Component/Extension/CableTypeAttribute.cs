using System;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CableTypeAttribute : Attribute
    {
        public CableType cableType { get; set; }

        /// <summary>
        /// 适用范围
        /// </summary>
        public CableTypeAttribute(CableType type)
        {
            cableType = type;
        }
    }

    public enum CableType
    {
        FixedCable,
        FlexibleCable,
        Cord,
        MICC,
    }
}
