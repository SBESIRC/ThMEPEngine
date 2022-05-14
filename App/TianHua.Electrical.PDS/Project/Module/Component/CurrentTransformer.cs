﻿using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 间接表
    /// </summary>
    [Serializable]
    public class CurrentTransformer : Meter
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="calculateCurrent"></param>
        /// <param name="polesNum"></param>
        /// <exception cref="NotSupportedException"></exception>
        public CurrentTransformer(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.CT;
            if (polesNum != "1P" && polesNum != "3P")
            {
                throw new NotSupportedException();
            }
            var meters = CurrentTransformerConfiguration.CTComponentInfos.Where(o => o.Amps > calculateCurrent).ToList();
            if (meters.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的CT");
            }
            CurrentTransformers = meters;
            PolesNum = polesNum;
            var meter = meters.First();
            MeterParameter = meter.parameter;
            AlternativeParameters = meters.Select(o => o.parameter).ToList();
        }

        public override List<string> GetParameters()
        {
            return AlternativeParameters;
        }
        public override void SetParameters(string parameters)
        {
            if (CurrentTransformers.Any(o => o.parameter == parameters))
            {
                MeterParameter = parameters;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 直接表类型
        /// </summary>
        public string MeterSwitchType = "1.5(6)";

        /// <summary>
        /// 断路器信息
        /// </summary>
        private List<CTComponentInfo> CurrentTransformers { get; set; }
    }
}
