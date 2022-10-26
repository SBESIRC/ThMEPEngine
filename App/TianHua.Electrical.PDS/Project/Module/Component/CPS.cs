using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    [CascadeComponent]
    [Serializable]
    public class CPS : PDSBaseComponent
    {
        /// <summary>
        /// CPS
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="IsDomesticWaterPump">是否是生活水泵</param>
        public CPS(double calculateCurrent,bool IsDomesticWaterPump)
        {
            this.ComponentType = ComponentType.CPS;
            IsNeglectCombination = true;
            if (IsDomesticWaterPump)
                DefaultResidualCurrent = "300";
            var cPSPicks = CPSConfiguration.CPSComponentInfos.Where(o => o.Amps > calculateCurrent).ToList();
            if (cPSPicks.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的CPS");
            }
            this.CPSPicks = cPSPicks;
            var CPS = cPSPicks.First();
            Model = CPS.Model;
            FrameSpecification = CPS.FrameSize;
            PolesNum = CPS.Poles;
            RatedCurrent = CPS.Amps;
            ResidualCurrent = DefaultResidualCurrent;
            Icu = CPS.IcuConfig;

            AlternativeModels = new List<string>() { "CPS" };
            AlternativeFrameSpecifications = cPSPicks.Select(o => o.FrameSize).Distinct().ToList();
            AlternativePolesNums = cPSPicks.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = cPSPicks.Select(o => o.Amps).Distinct().ToList();
            AlternativeIcus = CPSPicks.Select(o => o.IcuConfig).Distinct().ToList();
            AlternativeCombinations = CPS.CPSCombination.Split(';').ToList();
            AlternativeCombinations.Insert(0, "-");
            AlternativeResidualCurrents = CPS.ResidualCurrent.Split(';').ToList();
            AlternativeResidualCurrents.Insert(0, "-");
            Combination = AlternativeCombinations.First();
            AlternativeCodeLevels = CPS.CPSCharacteristics.Split(';').ToList();
            CodeLevel = AlternativeCodeLevels.First();
        }

        /// <summary>
        /// CPS
        /// </summary>
        /// <param name="cpsConfig"></param>
        public CPS(string cpsConfig, bool IsDomesticWaterPump, bool isNeglectCombination = true)
        {
            this.ComponentType = ComponentType.CPS;
            IsNeglectCombination = isNeglectCombination;
            //CPSJ32-M2.5/3P
            string[] configs = cpsConfig.Split('-');
            string[] detaileds = configs[1].Split('/');
            var cpsconfig = Regex.Replace(configs[0], @"\d", "");
            var cpsModel = cpsconfig.Substring(0 ,3);
            var combination = "-";
            if(cpsconfig.Length > 3)
            {
                combination = cpsconfig.Substring(3,1);
            }
            var frameSpecification = Regex.Replace(configs[0], @"\D", "");
            var polesNum = detaileds[1];
            int numIndex = detaileds[0].IndexOfAny(ProjectSystemConfiguration.NumberArray);
            var ratedCurrent = detaileds[0].Substring(numIndex);
            var codeLevel = detaileds[0].Substring(0, numIndex);

            if (IsDomesticWaterPump)
                DefaultResidualCurrent = "300";
            var cPSPicks = CPSConfiguration.CPSComponentInfos.Where(o => 
            o.Amps.ToString() == ratedCurrent
            && o.FrameSize == frameSpecification
            && o.Poles == polesNum
            && o.Model == cpsModel
            && o.CPSCharacteristics.Contains(codeLevel)
            && (combination == "-" || o.CPSCombination.Contains(combination))).Take(1).ToList();
            if (cPSPicks.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的CPS");
            }
            this.CPSPicks = cPSPicks;
            var CPS = cPSPicks.First();
            Model = CPS.Model;
            FrameSpecification = CPS.FrameSize;
            PolesNum = CPS.Poles;
            RatedCurrent = CPS.Amps;
            ResidualCurrent = DefaultResidualCurrent;
            Combination = combination;
            CodeLevel = codeLevel;
            Icu = CPS.IcuConfig;

            AlternativeModels = new List<string>() { cpsModel };
            AlternativeFrameSpecifications = cPSPicks.Select(o => o.FrameSize).Distinct().ToList();
            AlternativePolesNums = cPSPicks.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = cPSPicks.Select(o => o.Amps).Distinct().ToList();
            AlternativeIcus = CPSPicks.Select(o => o.IcuConfig).Distinct().ToList();
            //AlternativeCombinations = CPS.CPSCombination.Split(';').ToList();
            AlternativeCombinations = new List<string>() { combination };//待定，等张皓确认
            AlternativeResidualCurrents = new List<string>() { DefaultResidualCurrent };
            //AlternativeCodeLevels = CPS.CPSCharacteristics.Split(';').ToList();
            AlternativeCodeLevels = new List<string>() { codeLevel };
        }

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

        /// <summary>
        /// 剩余电流动作
        /// </summary>
        public string ResidualCurrent { get; set; }

        /// <summary>
        /// 分断能力
        /// </summary>
        public string Icu { get; set; }

        /// <summary>
        /// 是否忽略组合形式(仅针对Content)
        /// </summary>
        public bool IsNeglectCombination { get; private set; }

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
            && o.IcuConfig == Icu
            && o.CPSCombination.Contains(Combination)
            && o.CPSCharacteristics.Contains(CodeLevel)
            && (ResidualCurrent.Equals("-") || o.ResidualCurrent.Contains(ResidualCurrent))))
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
                Icu = cps.IcuConfig;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
                AlternativeResidualCurrents = cps.ResidualCurrent.Split(';').ToList();
                AlternativeResidualCurrents.Insert(0, "-");
                ResidualCurrent = DefaultResidualCurrent;
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
             && o.IcuConfig == Icu
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)
             && (ResidualCurrent.Equals("-") || o.ResidualCurrent.Contains(ResidualCurrent))))
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
                Icu = cps.IcuConfig;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
                AlternativeResidualCurrents = cps.ResidualCurrent.Split(';').ToList();
                AlternativeResidualCurrents.Insert(0, "-");
                ResidualCurrent = DefaultResidualCurrent;
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
             && o.IcuConfig == Icu
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)
             && (ResidualCurrent.Equals("-") || o.ResidualCurrent.Contains(ResidualCurrent))))
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
                Icu = cps.IcuConfig;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
                AlternativeResidualCurrents = cps.ResidualCurrent.Split(';').ToList();
                AlternativeResidualCurrents.Insert(0, "-");
                ResidualCurrent = DefaultResidualCurrent;
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
             && o.IcuConfig == Icu
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)
             && (ResidualCurrent.Equals("-") || o.ResidualCurrent.Contains(ResidualCurrent))))
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
                Icu = cps.IcuConfig;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
                AlternativeResidualCurrents = cps.ResidualCurrent.Split(';').ToList();
                AlternativeResidualCurrents.Insert(0, "-");
                ResidualCurrent = DefaultResidualCurrent;
            }
        }

        public List<string> GetIcus()
        {
            return AlternativeIcus;
        }
        public void SetIcu(string icu)
        {
            if (CPSPicks.Any(o => o.Amps == RatedCurrent
             && o.FrameSize == FrameSpecification
             && o.Model == Model
             && o.Poles == PolesNum
             && o.IcuConfig == icu
             && o.CPSCombination.Contains(Combination)
             && o.CPSCharacteristics.Contains(CodeLevel)
             && (ResidualCurrent.Equals("-") || o.ResidualCurrent.Contains(ResidualCurrent))))
            {
                this.Icu = icu;
            }
            else
            {
                var cps = CPSPicks.First(o => o.IcuConfig == icu);
                Model = cps.Model;
                FrameSpecification = cps.FrameSize;
                PolesNum = cps.Poles;
                RatedCurrent = cps.Amps;
                Icu = cps.IcuConfig;
                AlternativeCombinations = cps.CPSCombination.Split(';').ToList();
                Combination = AlternativeCombinations.First();
                AlternativeCodeLevels = cps.CPSCharacteristics.Split(';').ToList();
                CodeLevel = AlternativeCodeLevels.First();
                AlternativeResidualCurrents = cps.ResidualCurrent.Split(';').ToList();
                AlternativeResidualCurrents.Insert(0, "-");
                ResidualCurrent = DefaultResidualCurrent;
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

        public List<string> GetResidualCurrents()
        {
            return AlternativeResidualCurrents;
        }
        public void SetResidualCurrent(string residualCurrent)
        {
            this.ResidualCurrent = residualCurrent;
        }

        /// <summary>
        /// 备选型号下拉框选项
        /// </summary>
        private List<string> AlternativeModels { get; set; }

        /// <summary>
        /// 备选壳架规格下拉框选项
        /// </summary>
        private List<string> AlternativeFrameSpecifications { get; set; }

        /// <summary>
        /// 备选级数下拉框选项
        /// </summary>
        private List<string> AlternativePolesNums { get; set; }

        /// <summary>
        /// 备选额定电流下拉框选项
        /// </summary>
        private List<double> AlternativeRatedCurrents { get; set; }

        /// <summary>
        /// 备选组合形式下拉框选项
        /// </summary>
        private List<string> AlternativeCombinations { get; set; }

        /// <summary>
        /// 备选级别代号下拉框选项
        /// </summary>
        private List<string> AlternativeCodeLevels { get; set; }

        /// <summary>
        /// 备选剩余电流动作下拉框选项
        /// </summary>
        private List<string> AlternativeResidualCurrents { get; set; }

        /// <summary>
        /// 备选分断能力下拉框选项
        /// </summary>
        private List<string> AlternativeIcus { get; set; }

        /// <summary>
        /// 默认剩余电流动作
        /// </summary>
        private string DefaultResidualCurrent { get; set; } = "-";

        private List<CPSComponentInfo> CPSPicks { get; }

        public override double GetCascadeRatedCurrent()
        {
            return RatedCurrent;
        }
    }
}
