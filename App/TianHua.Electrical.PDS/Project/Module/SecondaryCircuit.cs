using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure;
namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 控制回路
    /// </summary>
    [Serializable]
    public class SecondaryCircuit
    {
        public int Index;
        public SecondaryCircuit(int index, SecondaryCircuitInfo secondaryCircuitInfo)
        {
            Index = index;
            CircuitDescription = secondaryCircuitInfo.Description;

            if (SecondaryCircuitConfiguration.FireSecondaryCircuitInfos.Contains(secondaryCircuitInfo))
            {
                AlternativeSecondaryCircuitInfos = SecondaryCircuitConfiguration.FireSecondaryCircuitInfos;
            }
            else
            {
                AlternativeSecondaryCircuitInfos = SecondaryCircuitConfiguration.NonFireSecondaryCircuitInfos;
            }
        }

        /// <summary>
        /// 回路ID
        /// </summary>
        public string CircuitID
        {
            get
            {
                return $"WC{Index.ToString("00")}";
            }
        }

        /// <summary>
        /// 回路描述
        /// </summary>
        public string CircuitDescription { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }

        public List<string> GetDescriptions()
        {
            return AlternativeSecondaryCircuitInfos.Select(o => o.Description).ToList();
        }

        public void SetDescription(string description)
        {
            var secondaryCircuitInfo = AlternativeSecondaryCircuitInfos.FirstOrDefault(o => o.Description.Equals(description));
            if (secondaryCircuitInfo.IsNull())
            {
                CircuitDescription = description;
                Conductor.IsCustom = true;
            }
            else
            {
                CircuitDescription = secondaryCircuitInfo.Description;
                Conductor.IsCustom = false;
                var conductorInfo = secondaryCircuitInfo.Conductor;
                if (conductorInfo.Contains('-'))
                {
                    Conductor.SetBAControl();
                }
                else
                {
                    Conductor.SetControlCircuitConfig(conductorInfo);
                }
            }
        }

        /// <summary>
        /// 备选控制回路
        /// </summary>
        private List<SecondaryCircuitInfo> AlternativeSecondaryCircuitInfos { get; set; }
    }
}
