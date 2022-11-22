using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using ThMEPWSS.PumpSectionalView.Model;
using NPOI.SS.Formula.Functions;

namespace TianHua.Plumbing.WPF.UI.Validations
{
    /// <summary>
    /// 
    /// </summary>
    public class AboveZeroAndHalfVlidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString().Trim())) 
                return new ValidationResult(false, "请检查您的输入！");

            //double outPut = double.Parse(value.ToString());
            double outPut= Convert.ToDouble(value.ToString());
            Regex halfRegex = new Regex("^[1-9]\\d*\\.[5]$|0\\.[5]$|^[1-9]\\d*$");//判断是否是0.5倍数
            if (outPut <= 0 || !halfRegex.IsMatch(value.ToString()))
            {
                return new ValidationResult(false, "请检查您的输入！");

            }
            return new ValidationResult(true, null);
        }

       

        
    }
}
