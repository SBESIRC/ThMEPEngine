using System.Globalization;
using System.Windows.Controls;

namespace Tianhua.Platform3D.UI.Validations
{
    public class GreaterThanZeroRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null)
            {
                string content = value as string;
                if (Validate(content))
                {
                    return new ValidationResult(true, "");
                }
                else
                {
                    return new ValidationResult(false, "输入的数值必须大于零");
                }
            }
            return new ValidationResult(false, "不允许输入空的数值");
        }
        public bool Validate(object content)
        {
            if(content.GetType()==typeof(int))
            {
                var value = (int)content;
                return value > 0;
            }
            else if (content.GetType() == typeof(double))
            {
                var value = (double)content;
                return value > 0;
            }
            else if(content.GetType() == typeof(string))
            {
                var value = content as string;
                double result;
                if(double.TryParse(value,out result))
                {
                    return result > 0;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
    }
}
