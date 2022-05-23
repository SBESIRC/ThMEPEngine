﻿using System;
using System.ComponentModel;

namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 回路类型
    /// </summary>
    public enum ThPDSCircuitType
    {
        /// <summary>
        /// 未知回路
        /// </summary>
        [Description("未知")]
        None = 1,

        /// <summary>
        /// 照明回路
        /// </summary>
        [Description("照明回路")]
        Lighting = 2,

        /// <summary>
        /// 插座回路
        /// </summary>
        [Description("插座回路")]
        Socket = 3,

        /// <summary>
        /// 动力回路
        /// </summary>
        [Description("动力回路")]
        PowerEquipment = 4,

        /// <summary>
        /// 应急照明回路
        /// </summary>
        [Description("应急照明回路")]
        EmergencyLighting = 5,

        /// <summary>
        /// 消防动力回路
        /// </summary>
        [Description("消防动力回路")]
        EmergencyPowerEquipment = 6,

        /// <summary>
        /// 消防应急照明回路
        /// </summary>
        [Description("消防应急照明回路")]
        FireEmergencyLighting = 7,

        /// <summary>
        /// 控制回路
        /// </summary>
        [Description("控制回路")]
        Control = 8,
    }

    /// <summary>
    /// 回路
    /// </summary>
    [Serializable]
    public class ThPDSCircuit
    {
        public ThPDSCircuit()
        {
            CircuitUID = System.Guid.NewGuid().ToString();
            ID = new ThPDSID();
        }

        /// <summary>
        /// 回路GUID
        /// </summary>
        public string CircuitUID { get; set; }

        /// <summary>
        /// 特征编号
        /// </summary>
        public ThPDSID ID { get; set; }

        /// <summary>
        /// 回路是否利用桥架
        /// </summary>
        public bool ViaCableTray { get; set; }

        /// <summary>
        /// 回路是否利用管线
        /// </summary>
        public bool ViaConduit { get; set; }
    }
}
