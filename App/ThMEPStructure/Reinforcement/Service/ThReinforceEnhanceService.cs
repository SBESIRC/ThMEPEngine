using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThReinforceEnhanceService
    {
        private readonly double DoubleTolerance = 1e-3;
        private string Reinforce { get;set; }
        private double CalculationArea { get; set; }
        /// <summary>
        /// 增强的纵筋
        /// </summary>
        public string EnhancedReinforce { get; private set; } = "";
        /// <summary>
        /// 迭代步数
        /// </summary>
        public int X { get; private set; }
        public ThReinforceEnhanceService(string reinforce,double calculationArea)
        {
            Reinforce = reinforce;
            CalculationArea = calculationArea;
        }
        
        public void Enhance()
        {
            var reinforces = Split();
            if(reinforces.Count==0)
            {
                // 如果传入的纵筋为空
                return;
            }            
            var diameters = GetAllDiameters(reinforces);
            if (diameters.Count < 4)
            {
                // 如果纵筋根数小于4
                return;
            }
            if (IsBiggThanCalculationArea(GetAllReinforceAreas(diameters)))
            {
                // 如果纵筋面积大于计算面积
                return;
            }
            // 实现增强逻辑
            // Step1：先取墙第一端的两根放大
            var firstDiameters = EnhanceFirstPort(diameters);            
            if(firstDiameters.Count==0)
            {
                return;
            }
            else if(IsBiggThanCalculationArea(GetAllReinforceAreas(firstDiameters)))
            {
                EnhancedReinforce = ToReinforceSpec(firstDiameters);
                return;
            }

            // Step2：在Step1的基础上，再取墙第二端的两根放大
            var secondDiameters = EnhanceSecondPort(firstDiameters);
            if (secondDiameters.Count == 0)
            {
                return;
            }
            else if(IsBiggThanCalculationArea(GetAllReinforceAreas(secondDiameters)))
            {
                EnhancedReinforce = ToReinforceSpec(secondDiameters);
                return;
            }

            // Step3：在Step2的基础上，再取墙第一端的两根放大
            var thirdDiameters = EnhanceFirstPort(secondDiameters);
            if (thirdDiameters.Count == 0)
            {
                return;
            }
            else if (IsBiggThanCalculationArea(GetAllReinforceAreas(thirdDiameters)))
            {
                EnhancedReinforce = ToReinforceSpec(thirdDiameters);
                return;
            }

            // Step4：在Step3的基础上，再取墙另一端的两根放大
            var fourthDiameters = EnhanceSecondPort(thirdDiameters);
            if (fourthDiameters.Count == 0)
            {
                return;
            }
            else if (IsBiggThanCalculationArea(GetAllReinforceAreas(fourthDiameters)))
            {
                EnhancedReinforce = ToReinforceSpec(fourthDiameters);
                return;
            }

            // Step5：为墙的第一端增加并筋(两根)
            var fifthDiameters = AddFirstPort(diameters);
            if (IsBiggThanCalculationArea(GetAllReinforceAreas(fifthDiameters)))
            {
                EnhancedReinforce = ToReinforceSpec(fifthDiameters);
                return;
            }

            // Step6：在Step5的基础上，为墙的第二端再增加并筋(两根)
            var sixthDiameters = AddSecondPort(fifthDiameters);
            if (IsBiggThanCalculationArea(GetAllReinforceAreas(sixthDiameters)))
            {
                EnhancedReinforce = ToReinforceSpec(sixthDiameters);
                return;
            }

            //
        }

        private bool IsBiggThanCalculationArea(double allReinforceArea)
        {
            //CalculationArea 是从图纸中提取的计算书的面积值
            //表中实配As = 3455.75，yjk提取值=3456,即满足yjk提取值 <= 实配钢筋
            return ThReinforcementUtils.IsBiggerThan(allReinforceArea, CalculationArea,0);
        }

        private List<double> AddFirstPort(List<double> diameters)
        {
            var results = new List<double>();
            for(int i =0;i< diameters.Count;i++)
            {
                if(i == 0 || i == 1)
                {
                    results.Add(diameters[i]);
                    results.Add(diameters[i]);
                }
                else
                {
                    results.Add(diameters[i]);
                }
            }
            return results;
        }

        private List<double> AddSecondPort(List<double> diameters)
        {
            // 在第一端增大的基础上放大
            var results = new List<double>();
            for (int i = 0; i < diameters.Count; i++)
            {
                if (i == 4 || i == 5)
                {
                    results.Add(diameters[i]);
                    results.Add(diameters[i]);
                }
                else
                {
                    results.Add(diameters[i]);
                }
            }
            return results;
        }

        private List<double> EnhanceFirstPort(List<double> diameters)
        {
            // 先取墙一端的两根放大
            var results = new List<double>();
            var firstEnlarge = ThSteelDataManager.Instance.FindEnhancedDiameter(diameters[0]);
            var secondEnlarge = ThSteelDataManager.Instance.FindEnhancedDiameter(diameters[1]);
            if(firstEnlarge.HasValue && secondEnlarge.HasValue)
            {
                results = diameters.Select(o => o).ToList();
                results[0] = firstEnlarge.Value;
                results[1] = secondEnlarge.Value;                
            }
            X++;
            return results;
        }

        private List<double> EnhanceSecondPort(List<double> diameters)
        {
            // 先取墙一端的两根放大
            var results = new List<double>();
            var thirdEnlarge = ThSteelDataManager.Instance.FindEnhancedDiameter(diameters[2]);
            var fourthEnlarge = ThSteelDataManager.Instance.FindEnhancedDiameter(diameters[3]);
            if (thirdEnlarge.HasValue && fourthEnlarge.HasValue)
            {
                results = diameters.Select(o => o).ToList();
                results[2] = thirdEnlarge.Value;
                results[3] = fourthEnlarge.Value;
            }
            X++;
            return results;
        }

        private List<double> GetAllDiameters(List<string> reinforces)
        {
            var results = new List<double>();
            reinforces.ForEach(o =>
            {
                var values = GetValues(o);
                if(values.Count==2 && values[0]>0 && values[1]>0)
                {
                    for(int i=1;i <= values[0];i++)
                    {
                        results.Add(values[1]);
                    }
                }
            });
            return results;
        }

        private List<double> GetValues(string reinforce)
        {
            var results = new List<double>();
            string pattern = @"\d+([.]{1}\d+){0,}";
            foreach(Match match in Regex.Matches(reinforce, pattern))
            {
                results.Add(double.Parse(match.Value));
            }
            return results;
        }
        private List<string> Split()
        {
            return Reinforce.Split('+').ToList()
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrEmpty(o))
                .Where(o=>IsValid(o))
                .ToList();
        }
        private bool IsValid(string reinforce)
        {
            // 8 C 100
            string pattern = @"^\s{0,}\d+\s{0,}[C]{1}\s{0,}\d+([.]\d+){0,}$";
            return Regex.IsMatch(reinforce.ToUpper(), pattern);
        }
        
        private double GetAllReinforceAreas(List<double> diameters)
        {
            return diameters.Sum(o=> ThSteelDataManager.Instance.GetSteelArea(o,DoubleTolerance));
        }
        private string ToReinforceSpec(List<double> diameters)
        {
            var specs = new List<string>();
            var newDiameters = diameters.Select(o => o).ToList();
            while (newDiameters.Count>0)
            {
                var first = newDiameters.First();
                int count = 0;
                for(int i=0;i< newDiameters.Count;i++)
                {
                    if(Math.Abs(newDiameters[i]- first)<= DoubleTolerance)
                    {
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
                specs.Add(ToReinforceSpec(count, first));
                for(int i=0;i< count;i++)
                {
                    newDiameters.RemoveAt(0);
                }
            }
           return string.Join("+", specs);
        }
        private string ToReinforceSpec(int count ,double diameter)
        {
            var floor = Math.Floor(diameter);
            if(Math.Abs(diameter- floor)<= DoubleTolerance)
            {
                return count + "C" + (int)floor;
            }
            else
            {
                return count + "C" + diameter.ToString("0.0");
            }
        }
    }
}
