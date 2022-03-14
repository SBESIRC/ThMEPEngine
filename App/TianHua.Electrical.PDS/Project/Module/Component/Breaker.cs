using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 断路器（抽象基类）
    /// </summary>
    public abstract class BreakerBaseComponent : PDSBaseComponent
    {

    }

    /// <summary>
    /// 断路器
    /// </summary>
    public class Breaker : BreakerBaseComponent
    {
        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="tripDevice">脱扣器类型</param>
        /// <param name="polesNum">极数</param>
        /// <param name="characteristics">瞬时脱扣器类型</param>
        public Breaker(double calculateCurrent, List<string> tripDevice, string polesNum, string characteristics)
        {
            ComponentType = ComponentType.断路器;
            var breaker = BreakerConfiguration.breakerComponentInfos.
                FirstOrDefault(o => o.DefaultPick 
                && double.Parse(o.Amps.Split(';').Last())>calculateCurrent
                && tripDevice.Contains(o.TripDevice)
                && o.Poles == polesNum
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics)));
            if(breaker.IsNull())
            {
                throw new NotSupportedException();
            }
            BreakerType = breaker.Model;
            FrameSpecifications = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.Split(';').Select(o => double.Parse(o)).First(o => o > calculateCurrent).ToString();
            TripUnitType =breaker.TripDevice;
        }

        public string Content { get { return $"{BreakerType}{FrameSpecifications}-{TripUnitType}{RatedCurrent}/{PolesNum}"; } }

        /// <summary>
        /// 模型
        /// </summary>
        public string BreakerType { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        public string FrameSpecifications { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 脱扣器类型
        /// </summary>
        public string TripUnitType { get; set; }

        /// <summary>
        /// 附件
        /// </summary>
        public string Appendix { get; set; }
    }

    /// <summary>
    /// 剩余电流断路器（RCCB）
    /// </summary>
    public class ResidualCurrentCircuitBreaker : BreakerBaseComponent
    {
        public ResidualCurrentCircuitBreaker()
        {
            ComponentType = ComponentType.剩余电流断路器;
        }
    }
}
