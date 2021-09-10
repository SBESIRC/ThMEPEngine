using System.Globalization;
using System.Windows.Controls;

namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class NearbyDistanceRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if(value!=null)
            {
                string content = value as string;
                if (Validate(content))
                {
                    return new ValidationResult(true, "");
                }
                else
                {
                    return new ValidationResult(false, "请输入大于零的数");
                }
            }
            return new ValidationResult(false, "");
        }
        public bool Validate(string value)
        {
            double number;
            if (double.TryParse(value.Trim(), out number))
            {
                return number>0;
            }
            return false;
        }
    }
}
