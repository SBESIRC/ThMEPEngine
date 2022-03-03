using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 剩余电流断路器
    /// </summary>
    public class ResidualCurrentCircuitBreaker: Breaker
    {
        public ResidualCurrentCircuitBreaker()
        {
            ComponentType = ComponentType.剩余电流断路器;
        }
    }
}
