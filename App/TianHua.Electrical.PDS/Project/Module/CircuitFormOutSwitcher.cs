using System;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 出线回路转换器
    /// </summary>
    public class CircuitFormOutSwitcher
    {
        private readonly ThPDSProjectGraphEdge _edge;
        public CircuitFormOutSwitcher(ThPDSProjectGraphEdge edge)
        {
            _edge = edge;
        }

        /// <summary>
        /// 获取可选择回路列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<string> AvailableTypes()
        {
            return ThPDSProjectGraphService.AvailableTypes(_edge);
        }

        /// <summary>
        /// 获取对应CircuitFormOutType
        /// </summary>
        /// <param name="CircuitName"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public CircuitFormOutType Switch(string CircuitName)
        {
            return ThPDSProjectGraphService.Switch(_edge, CircuitName);
        }
    }
}
