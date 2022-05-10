﻿using System;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 三路进线
    /// </summary>
    [Serializable]
    public class ThreeWayInCircuit : PDSBaseInCircuit
    {
        public ThreeWayInCircuit()
        {
            CircuitFormType = CircuitFormInType.三路进线;
        }

        /// <summary>
        /// 坑位1：隔离开关/断路器
        /// </summary>
        public PDSBaseComponent Component1 { get; set; }

        /// <summary>
        /// 坑位2：隔离开关/断路器
        /// </summary>
        public PDSBaseComponent Component2 { get; set; }

        /// <summary>
        /// 坑位3: 转换开关1
        /// </summary>
        public TransferSwitch transferSwitch1 { get; set; }

        /// <summary>
        /// 坑位4：隔离开关/断路器
        /// </summary>
        public PDSBaseComponent Component3 { get; set; }

        /// <summary>
        /// 坑位5: 转换开关2
        /// </summary>
        public TransferSwitch transferSwitch2 { get; set; }

        /// <summary>
        /// 坑位6：OUVP/Meter
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }
    }
}
