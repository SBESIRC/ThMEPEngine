using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 元器件
    /// </summary>
    public class PDSBaseComponent
    {
        /// <summary>
        /// 元器件类型
        /// </summary>
        public ComponentType ComponentType { get; set; }
    }

    /// <summary>
    /// 断路器元器件
    /// </summary>
    public class Breaker : PDSBaseComponent
    {
    }

    /// <summary>
    /// 元器件类型
    /// </summary>
    public enum ComponentType
    {
        隔离开关,
        接触器,
        热继电器,
        断路器,
        剩余电流断路器,
    }
}
