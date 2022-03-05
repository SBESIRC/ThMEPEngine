using System;

namespace TianHua.Electrical.PDS.Project.Module
{
    public static class ThPDSProjectGraphService
    {
        /// <summary>
        /// 新建回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="circuit"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void AddCircuit(ThPDSProjectGraph graph, CircuitFormOutType type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 切换进线形式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="type"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void UpdateFormInType(ThPDSProjectGraph graph, CircuitFormInType type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 切换回路样式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="type"></param>
        public static void SwitchFormOutType(ThPDSProjectGraph graph, ThPDSProjectGraphNode node, CircuitFormOutType type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 锁定回路（解锁回路）
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="doLock"></param>
        public static void Lock(ThPDSProjectGraph graph, ThPDSProjectGraphNode node, bool doLock)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void Delete(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }
    }
}
