using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 热继电器
    /// </summary>
    [Serializable]
    public class ThermalRelay : PDSBaseComponent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="calculateCurrent"></param>
        public ThermalRelay(double calculateCurrent)
        {
            ComponentType = ComponentType.KH;
            var thermalRelays = ThermalRelayConfiguration.thermalRelayInfos.
                Where(o => o.MinAmps <= calculateCurrent && o.MaxAmps >= calculateCurrent);
            var thermalRelay = thermalRelays.OrderBy(o => Math.Abs(2 * calculateCurrent - o.MinAmps - o.MaxAmps)).FirstOrDefault();
            if (thermalRelay.IsNull())
            {
                thermalRelay = ThermalRelayConfiguration.thermalRelayInfos.First();
            }
            ThermalRelaySelection = thermalRelay;
            Model = thermalRelay.Model;
            PolesNum = thermalRelay.Poles;
            RatedCurrent = $"{thermalRelay.MinAmps}~{thermalRelay.MaxAmps}";
            AlternativeModels = new List<string>() { Model };
            AlternativePolesNums = new List<string>() { PolesNum };
            AlternativeRatedCurrents = new List<string>() { RatedCurrent };
        }

        public ThermalRelay(string thermalRelayConfig)
        {
            ComponentType = ComponentType.KH;

            //JR20-10 0.8/1.2A
            string[] configs = thermalRelayConfig.Split(' ');
            string[] detaileds = configs[1].Split('/');
            var model = configs[0];
            var poles = "3P";
            var minAmps = detaileds[0];
            var maxAmps = detaileds[1].Replace("A", "");

            var thermalRelay = ThermalRelayConfiguration.thermalRelayInfos.
                FirstOrDefault(o => o.Model == model
                && o.MinAmps.ToString() == minAmps 
                && o.MaxAmps.ToString() == maxAmps
                && o.Poles == poles);
            if (thermalRelay.IsNull())
            {
                thermalRelay = ThermalRelayConfiguration.thermalRelayInfos.First();
            }
            ThermalRelaySelection = thermalRelay;
            Model = thermalRelay.Model;
            PolesNum = thermalRelay.Poles;
            RatedCurrent = $"{thermalRelay.MinAmps}~{thermalRelay.MaxAmps}";
            AlternativeModels = new List<string>() { Model };
            AlternativePolesNums = new List<string>() { PolesNum };
            AlternativeRatedCurrents = new List<string>() { RatedCurrent };
        }

        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 电流整定范围
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetModel(string model)
        {
            //暂时先不支持切换，且也无法切换
        }
        public List<string> GetModels()
        {
            return AlternativeModels;
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetPolesNum(string polesNum)
        {
            //暂时先不支持切换，且也无法切换
        }
        public List<string> GetPolesNums()
        {
            return AlternativePolesNums;
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetRatedCurrent(string ratedCurrent)
        {
            //暂时先不支持切换，且也无法切换
        }
        public List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }

        private List<string> AlternativeModels { get; set; }
        private List<string> AlternativePolesNums { get; set; }
        private List<string> AlternativeRatedCurrents { get; set; }
        private ThermalRelayConfigurationItem ThermalRelaySelection { get; set; }
    }
}
