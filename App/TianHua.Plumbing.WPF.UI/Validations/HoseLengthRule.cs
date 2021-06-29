namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class HoseLengthRule : NumberRangeRule
    {
        public HoseLengthRule()
        {
            NumberRangeValidate.ErrorMsg = "只能输入小于100的正整数";
            NumberRangeValidate.NotAllowEmptyValueMsg = "不能输入空的值";
            NumberRangeValidate.MinNumber = 0;
            NumberRangeValidate.MaxNumber = 100;            
            NumberRangeValidate.ValidateMethod = NumberRangeValidate.Validate4;
        }
    }
}
