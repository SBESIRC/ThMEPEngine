using System;

namespace TianHua.Plumbing.WPF.UI.Validations
{
    public class NumberRangeInfo
    {
        public Func<double , bool> ValidateMethod { get; set; }
        public string ErrorMsg { get; set; }
        public string NotAllowEmptyValueMsg { get; set; }
        public double MinNumber { get; set; }
        public double MaxNumber { get; set; }
        public NumberRangeInfo()
        {
            ErrorMsg = "";
            ValidateMethod = Validate3;
            NotAllowEmptyValueMsg = "不允许输入空的值";
        }

        public bool CheckValid(double value)
        {
            return ValidateMethod == null ? false : ValidateMethod(value);
        }
        /// <summary>
        /// value大于等于MinNumber and value小于等于MaxNumber
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Validate1(double value)
        {
            return value >= MinNumber && value <= MaxNumber;
        }
        /// <summary>
        /// value大于等于MinNumber and value小于MaxNumber
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Validate2(double value)
        {
            return value >= MinNumber && value < MaxNumber;
        }
        /// <summary>
        /// value大于MinNumber and value小于等于MaxNumber
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Validate3(double value)
        {
            return value > MinNumber && value <= MaxNumber;
        }
        /// <summary>
        /// value大于MinNumber and value小于MaxNumber
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Validate4(double value)
        {
            return value > MinNumber && value < MaxNumber;
        }
    }
}
