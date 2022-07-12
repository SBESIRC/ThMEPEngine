using System;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MaterialAttribute : Attribute
    {
        public MaterialType MaterialType { get; set; }

        /// <summary>
        /// 适用范围
        /// </summary>
        public MaterialAttribute(MaterialType type)
        {
            MaterialType = type;
        }
    }

    public enum MaterialType
    {
        Copper,
    }
}
