using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ScopeOfApplicationAttribute : Attribute
    {
        public ScopeOfApplicationType scopeOfApplicationType { get; set; }

        /// <summary>
        /// 适用范围
        /// </summary>
        public ScopeOfApplicationAttribute(ScopeOfApplicationType type)
        {
            scopeOfApplicationType = type;
        }
    }

    public enum ScopeOfApplicationType
    {
        ForPowerCircuits,
        ForControlCircuits
    }
}
