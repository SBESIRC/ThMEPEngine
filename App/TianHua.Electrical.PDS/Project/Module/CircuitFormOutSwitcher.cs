using System;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 出线回路转换器
    /// </summary>
    public class CircuitFormOutSwitcher
    {
        /*常规配电回路
         *漏电保护回路
         *带接触器回路
         *带热继电器回路
         *计量(上海)
         *计量(表在前)
         *计量(表在后)
         *电动机配电回路
         *双速电机D-YY
         *双速电机Y-Y
         *分支母排
         */
        private readonly ThPDSProjectGraphEdge<ThPDSProjectGraphNode> _edge;
        public CircuitFormOutSwitcher(ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
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
