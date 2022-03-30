using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

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
        private ThPDSProjectGraphEdge<ThPDSProjectGraphNode> Edge;
        public CircuitFormOutSwitcher(ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            this.Edge = edge;
        }

        /// <summary>
        /// 获取可选择回路列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<string> AvailableTypes()
        {
            CircuitGroup circuitGroup = Edge.Details.CircuitForm.CircuitFormType.GetCircuitType().GetCircuitGroup();
            switch (circuitGroup)
            {
                case CircuitGroup.Group1:
                    { return ProjectSystemConfiguration.Group1Switcher; }
                case CircuitGroup.Group2:
                    { return ProjectSystemConfiguration.Group2Switcher; }
                case CircuitGroup.Group3:
                    { return ProjectSystemConfiguration.Group3Switcher; }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        /// <summary>
        /// 获取对应CircuitFormOutType
        /// </summary>
        /// <param name="CircuitName"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public CircuitFormOutType Switch(string CircuitName)
        {
            if (CircuitName == "常规配电回路")
                return CircuitFormOutType.常规;
            else if (CircuitName == "漏电保护回路")
                return CircuitFormOutType.漏电;
            else if (CircuitName == "带接触器回路")
                return CircuitFormOutType.接触器控制;
            else if (CircuitName == "带热继电器回路")
                return CircuitFormOutType.热继电器保护;
            else if (CircuitName == "计量(上海)")
            {
                if (Edge.Target.Details.LowPower < 100)
                    return CircuitFormOutType.配电计量_上海直接表;
                else
                    return CircuitFormOutType.配电计量_上海CT;
            }
            else if (CircuitName == "计量(表在前)")
            {
                if (Edge.Target.Details.LowPower < 100)
                    return CircuitFormOutType.配电计量_直接表在前;
                else
                    return CircuitFormOutType.配电计量_CT表在前;
            }
            else if (CircuitName == "计量(表在后)")
            {
                if (Edge.Target.Details.LowPower < 100)
                    return CircuitFormOutType.配电计量_直接表在后;
                else
                    return CircuitFormOutType.配电计量_CT表在后;
            }
            else if (CircuitName == "电动机配电回路")
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (Edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        return CircuitFormOutType.电动机_分立元件;
                    }
                    else
                    {
                        return CircuitFormOutType.电动机_分立元件星三角启动;
                    }
                }
                else
                {
                    if (Edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        return CircuitFormOutType.电动机_CPS;
                    }
                    else
                    {
                        return CircuitFormOutType.电动机_CPS星三角启动;
                    }
                }
            }
            else
            {
                //其他目前暂不支持
                throw new NotSupportedException();
            }
        }
    }
}
