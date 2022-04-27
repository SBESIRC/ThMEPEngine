﻿using System;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 消防应急照明
    /// </summary>
    [Serializable]
    [CircuitGroup(CircuitGroup.Group1)]
    public class FireEmergencyLighting : PDSBaseOutCircuit
    {
        /// <summary>
        /// 消防应急照明
        /// </summary>
        public FireEmergencyLighting()
        {
            CircuitFormType = CircuitFormOutType.消防应急照明回路WFEL;
        }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }
    }
}
