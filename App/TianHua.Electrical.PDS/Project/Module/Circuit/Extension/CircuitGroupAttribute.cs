using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.Extension
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CircuitGroupAttribute : Attribute
    {
        public CircuitGroup groupType;
        public CircuitGroupAttribute(CircuitGroup GroupType)
        {
            groupType = GroupType;
        }
    }

    public enum CircuitGroup
    {
        [Description("非电动机配电回路")]
        Group1,
        [Description("非变速电动机配电回路")]
        Group2,
        [Description("变速电动机配电回路")]
        Group3,
    }
}
