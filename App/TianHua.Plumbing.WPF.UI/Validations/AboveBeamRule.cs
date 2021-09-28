namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class AboveBeamRule : NumberRangeRule
    {
        public AboveBeamRule()
        {
            NumberRangeValidate.ErrorMsg = "";
            NumberRangeValidate.NotAllowEmptyValueMsg = "不能输入空的值";
            NumberRangeValidate.MinNumber = 0;
            NumberRangeValidate.MaxNumber = 10000;
            NumberRangeValidate.ValidateMethod = NumberRangeValidate.Validate4;
        }
    }
}
