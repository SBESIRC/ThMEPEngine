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

namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class StringNotNullValidationRule : ValidationRule
    {
      
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString().Trim()))
                return new ValidationResult(false, "请检查您的输入！");

            
            return new ValidationResult(true,null);

        }

        
        
    }
}
