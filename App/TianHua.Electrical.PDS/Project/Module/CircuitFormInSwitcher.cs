using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class CircuitFormInSwitcher
    {
        private ThPDSProjectGraphNode _node;
        public CircuitFormInSwitcher(ThPDSProjectGraphNode node)
        {
            this._node = node;
        }

        /// <summary>
        /// 获取可选择回路列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<CircuitFormInType> AvailableTypes()
        {
            return ThPDSProjectGraphService.AvailableTypes(_node);
        }
    }
}
