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
        private readonly bool _isCentralizedPowerCircuit;
        public CircuitFormOutSwitcher(ThPDSProjectGraphEdge edge)
        {
            _edge = edge;
            _isCentralizedPowerCircuit = edge.Source.Details.CircuitFormType.CircuitFormType == CircuitFormInType.集中电源;
        }

        /// <summary>
        /// 获取可选择回路列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<string> AvailableTypes()
        {
            if(_isCentralizedPowerCircuit)
            {
                return new List<string>() { "消防应急照明回路（WFEL）" };
            }
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
