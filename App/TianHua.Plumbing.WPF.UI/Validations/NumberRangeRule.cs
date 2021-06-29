using System.Globalization;
using System.Windows.Controls;

namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class NumberRangeRule : ValidationRule
    {
        protected NumberRangeInfo NumberRangeValidate { get; set; }
        protected NumberRangeRule()
        {
            NumberRangeValidate = new NumberRangeInfo();
        }
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
                    return new ValidationResult(false, NumberRangeValidate.ErrorMsg);
                }                
            }
            return new ValidationResult(false, NumberRangeValidate.NotAllowEmptyValueMsg);
        }
        public bool Validate(string value)
        {
            var number = 0.0;
            if (double.TryParse(value.Trim(), out number))
            {
                return NumberRangeValidate.CheckValid(number);
            }
            return false;
        }
    }
}
