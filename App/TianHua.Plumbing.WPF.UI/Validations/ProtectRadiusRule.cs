using System.Globalization;
using System.Windows.Controls;

namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class ProtectRadiusRule : ValidationRule
    {         
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if(value!=null)
            {
                string content = value as string;
                if(Validate(content))
                {
                    return new ValidationResult(true, "");
                }
                else
                {
                    return new ValidationResult(false, "用户只能输入不大于99的正整数");
                }                
            }
            return new ValidationResult(false, "不能输入空的值");
        }
        public bool Validate(string value)
        {
            var number = 0.0;
            if (double.TryParse(value.Trim(), out number))
            {
                if (number > 0 && number <= 99)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
