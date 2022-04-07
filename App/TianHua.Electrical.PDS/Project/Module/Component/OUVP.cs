using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    public class OUVP : PDSBaseComponent
    {
        public OUVP(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.OUVP;

            var ouvpPicks = OUVPConfiguration.OUVPComponentInfos.Where(o => o.Poles.Equals(polesNum) && o.Amps > calculateCurrent).ToList();
            if (ouvpPicks.Count == 0)
            {
                throw new NotSupportedException();
            }
            this.OUVPPicks = ouvpPicks;
            var ouvp = ouvpPicks.First();
            Model = ouvp.Model;
            PolesNum = ouvp.Poles;
            RatedCurrent = ouvp.Amps;

            AlternativeModels = new List<string>() { "OUVP" };
            AlternativePolesNums = ouvpPicks.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = ouvpPicks.Select(o => o.Amps).Distinct().ToList();
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
        /// 额定电流
        /// </summary>
        public double RatedCurrent { get; set; }

        public List<string> GetModels()
        {
            return AlternativeModels;
        }
        public void SetModel(string model)
        {
            if (OUVPPicks.Any(o => o.Model == model
             && o.Poles == PolesNum
             && o.Amps == RatedCurrent))
            {
                this.Model = model;
            }
            else
            {
                var cps = OUVPPicks.First(o => o.Model == model);
                Model = cps.Model;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
            }
        }

        public List<string> GetPolesNums()
        {
            return AlternativePolesNums;
        }
        public void SetPolesNum(string polesNum)
        {
            if (OUVPPicks.Any(o => o.Poles == polesNum
             && o.Model == Model
             && o.Amps == RatedCurrent))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var cps = OUVPPicks.First(o => o.Poles == polesNum);
                Model = cps.Model;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
            }
        }

        public List<double> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }
        public void SetRatedCurrent(double ratedCurrent)
        {
            if (OUVPPicks.Any(o => o.Amps == ratedCurrent
             && o.Model == Model
             && o.Poles == PolesNum))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var cps = OUVPPicks.First(o => o.Amps == ratedCurrent);
                Model = cps.Model;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
            }
        }

        private List<string> AlternativeModels { get; set; }
        private List<string> AlternativePolesNums { get; set; }
        private List<double> AlternativeRatedCurrents { get; set; }
        private List<OUVPComponentInfo> OUVPPicks { get;  set; }
    }
}
