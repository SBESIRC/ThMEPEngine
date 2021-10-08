namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class AreaDensityRule : NumberRangeRule
    {
        public AreaDensityRule()
        {
            NumberRangeValidate.ErrorMsg = "";
            NumberRangeValidate.NotAllowEmptyValueMsg = "不能输入空的值";
            NumberRangeValidate.MinNumber = 0;
            NumberRangeValidate.MaxNumber = 10000;
            NumberRangeValidate.ValidateMethod = NumberRangeValidate.Validate4;
        }
    }
}
