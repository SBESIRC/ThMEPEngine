using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    [CascadeComponent]
    public class CPS : PDSBaseComponent
    {
        /// <summary>
        /// CPS
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        public CPS(double calculateCurrent)
        {
            this.ComponentType = ComponentType.CPS;
            var cPSPicks = CPSConfiguration.CPSComponentInfos.Where(o => o.Amps > calculateCurrent).ToList();
            if (cPSPicks.Count == 0)
            {
                throw new NotSupportedException();
            }
            this.CPSPicks = cPSPicks;
            var CPS = cPSPicks.First();
            Model = CPS.Model;
            FrameSpecification = CPS.FrameSize;
            PolesNum = CPS.Poles;
            RatedCurrent = CPS.Amps;

            AlternativeModels = new List<string>() { "CPS" };
            AlternativeFrameSpecifications = cPSPicks.Select(o => o.FrameSize).Distinct().ToList();
            AlternativePolesNums = cPSPicks.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = cPSPicks.Select(o => o.Amps).Distinct().ToList();
            AlternativeCombinations = CPS.CPSCombination.Split(';').ToList();
            Combination = AlternativeCombinations.First();
            AlternativeCodeLevels = CPS.CPSCharacteristics.Split(';').ToList();
            CodeLevel = AlternativeCodeLevels.First();
        }

        //public string Content { get { return $"{BreakerType}{FrameSpecifications}-{TripUnitType}{RatedCurrent}/{PolesNum}"; } }

        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        public string FrameSpecification { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public double RatedCurrent { get; set; }

        /// <summary>
        /// 组合形式
        /// </summary>
        public string Combination { get; set; }

        /// <summary>
        /// 级别代号
        /// </summary>
        public string CodeLevel { get; set; }

        public List<string> GetModels()
        {
            return AlternativeModels;
        }
        public void SetModel(string model)
        {
            if(CPSPicks.Any(o => o.Model == model
            && o.Poles == PolesNum
            && o.FrameSize == FrameSpecification
            && o.Amps == RatedCurrent
            && o.CPSCombination.Contains(Combination)
            && o.CPSCharacteristics.Contains(CodeLevel)))
            {
                this.Model = model;
            }
            else
            {
                var cps = CPSPicks.First(o => o.Model == model);
                Model = cps.Model;
                FrameSpecification = cps.FrameSize;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
            }
        }

        public List<string> GetFrameSpecifications()
        {
            return AlternativeFrameSpecifications;
        }
        public void SetFrameSpecification(string frameSpecification)
        {
            if (CPSPicks.Any(o => o.Poles == PolesNum
             && o.FrameSize == frameSpecification
             && o.Model == Model
             && o.Amps == RatedCurrent
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)))
            {
                this.FrameSpecification = frameSpecification;
            }
            else
            {
                var cps = CPSPicks.First(o => o.FrameSize == frameSpecification);
                Model = cps.Model;
                FrameSpecification = cps.FrameSize;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
            }
        }

        public List<string> GetPolesNums()
        {
            return AlternativePolesNums;
        }
        public void SetPolesNum(string polesNum)
        {
            if (CPSPicks.Any(o => o.Poles == polesNum
             &&o.FrameSize == FrameSpecification
             && o.Model == Model
             && o.Amps == RatedCurrent
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var cps = CPSPicks.First(o => o.Poles == polesNum);
                Model = cps.Model;
                FrameSpecification = cps.FrameSize;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
            }
        }

        public List<double> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }
        public void SetRatedCurrent(double ratedCurrent)
        {
            if (CPSPicks.Any(o => o.Amps == ratedCurrent
             && o.FrameSize == FrameSpecification
             && o.Model == Model
             && o.Poles == PolesNum
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var cps = CPSPicks.First(o => o.Amps == ratedCurrent);
                Model = cps.Model;
                FrameSpecification = cps.FrameSize;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
            }
        }

        public List<string> GetCombinations()
        {
            return AlternativeCombinations;
        }
        public void SetCombination(string combination)
        {
            this.Combination = combination;
        }

        public List<string> GetCodeLevels()
        {
            return AlternativeCodeLevels;
        }
        public void SetCodeLevel(string codeLevel)
        {
            this.CodeLevel = codeLevel;
        }

        /// <summary>
        /// 备选型号
        /// </summary>
        private List<string> AlternativeModels { get; set; }

        /// <summary>
        /// 备选壳架规格
        /// </summary>
        private List<string> AlternativeFrameSpecifications { get; set; }

        /// <summary>
        /// 备选级数
        /// </summary>
        private List<string> AlternativePolesNums { get; set; }

        /// <summary>
        /// 备选额定电流
        /// </summary>
        private List<double> AlternativeRatedCurrents { get; set; }

        /// <summary>
        /// 备选组合形式
        /// </summary>
        private List<string> AlternativeCombinations { get; set; }

        /// <summary>
        /// 备选级别代号
        /// </summary>
        private List<string> AlternativeCodeLevels { get; set; }
        private List<CPSComponentInfo> CPSPicks { get; }

        public override double GetCascadeRatedCurrent()
        {
            return RatedCurrent;
        }
    }
}
