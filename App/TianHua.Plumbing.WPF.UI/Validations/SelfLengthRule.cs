namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class SelfLengthRule : NumberRangeRule
    {
        public SelfLengthRule()
        {
            NumberRangeValidate.ErrorMsg = "只能输入不超过99的正整数";
            NumberRangeValidate.NotAllowEmptyValueMsg = "不能输入空的值";
            NumberRangeValidate.MinNumber = 0;
            NumberRangeValidate.MaxNumber = 99;            
            NumberRangeValidate.ValidateMethod = NumberRangeValidate.Validate3;
        }
    }
}
