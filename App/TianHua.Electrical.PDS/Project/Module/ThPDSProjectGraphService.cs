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
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 切换进线形式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="type"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void UpdateFormInType(ThPDSProjectGraph graph, CircuitFormInType type)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 切换回路样式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="type"></param>
        public static void SwitchFormOutType(ThPDSProjectGraph graph, ThPDSProjectGraphNode node, CircuitFormOutType type)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 锁定回路（解锁回路）
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="doLock"></param>
        public static void Lock(ThPDSProjectGraph graph, ThPDSProjectGraphNode node, bool doLock)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 删除回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void Delete(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 插入过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InsertUndervoltageProtector(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 移除过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveUndervoltageProtector(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 插入电能表
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InsertEnergyMeter(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 移除电能表
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveEnergyMeter(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 更新图
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void UpdateWithNode(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            //throw new NotImplementedException();
        }
    }
}
