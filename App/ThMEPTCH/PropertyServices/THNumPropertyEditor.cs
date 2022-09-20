using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace ThMEPTCH.PropertyServices
{
    class THNumPropertyEditor : PropertyEditorBase
    {
        public double minValue { get; set; }
        public double maxValue { get; set; }
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new NumericUpDown
        {
            Minimum = GetMaxMinValue(propertyItem, out double outMaxValue,out double increment,out int decimalPlaces),
            Maximum = outMaxValue,
            Increment = increment,
            DecimalPlaces = decimalPlaces,
            VerifyFunc = VerifyFunc,
            IsReadOnly = propertyItem.IsReadOnly,
        };
        public override DependencyProperty GetDependencyProperty() => NumericUpDown.ValueProperty;
        public override UpdateSourceTrigger GetUpdateSourceTrigger(PropertyItem propertyItem) => UpdateSourceTrigger.LostFocus;
        private OperationResult<bool> VerifyFunc(string data)
        {
            if (double.TryParse(data, out double value))
            {
                if (value >= minValue && value <= maxValue)
                {
                    return OperationResult.Success();
                }
                else
                {
                    return OperationResult.Failed(string.Format("不在取值范围内[{0},{1}]", minValue, maxValue));
                }
            }
            else
            {
                return OperationResult.Failed("不是数值");
            }
        }
        private double GetMaxMinValue(PropertyItem propertyItem, out double outMaxValue, out double increment, out int decimalPlaces)
        {
            outMaxValue = double.MaxValue;
            minValue = double.MinValue;
            var type = propertyItem.Value.GetType();
            var prop = type.GetProperty(propertyItem.PropertyName);
            if (prop.PropertyType.Name.ToString().ToLower().Contains("int"))
            {
                increment = 1;
                decimalPlaces = 0;
            }
            else 
            {
                increment = 1;
                decimalPlaces = 2;
            }
            var propItemAttrs = (System.Attribute[])prop.GetCustomAttributes();
            if (propItemAttrs.Length > 0) 
            {
                foreach (var attr in propItemAttrs) 
                {
                    if (attr is THNumberRangeAttribute rangeAttribute) 
                    {
                        if (rangeAttribute.IsIntRange)
                        {
                            outMaxValue = rangeAttribute.IntMaximum;
                            minValue = rangeAttribute.IntMinimum;
                        }
                        else 
                        {
                            outMaxValue = rangeAttribute.DoubleMaximum;
                            minValue = rangeAttribute.DoubleMinimum;
                        }
                        increment = rangeAttribute.Increment;
                        decimalPlaces = rangeAttribute.DecimalPlaces;
                    }
                }
            }
            maxValue = outMaxValue;
            return minValue;
        }
    }
    /// <summary>
    /// 数字输入框属性配合 PropertyEditor 的 NumericUpDown使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    class THNumberRangeAttribute : Attribute 
    {
        public int IntMinimum { get; }
        public int IntMaximum { get; }
        public double DoubleMaximum { get; }
        public double DoubleMinimum { get; }
        public bool IsIntRange { get; }
        public double Increment { get; }
        public int DecimalPlaces { get; }
        /// <summary>
        /// Int的输入范围单步增加的数字
        /// </summary>
        /// <param name="minimum">最小值(无限制是给int.MinValue)</param>
        /// <param name="maximum">最大值(无限制是给int.MaxValue)</param>
        /// <param name="increment">配合NumericUpDown使用 单击的步长</param>
        public THNumberRangeAttribute(int minimum, int maximum,int increment = 1)
        {
            IsIntRange = true;
            IntMinimum = minimum;
            IntMaximum = maximum;
            Increment = increment;
            DecimalPlaces = 0;
        }
        /// <summary>
        /// double的输入范围单步增长数字
        /// </summary>
        /// <param name="minimum">最小值(无限制是给double.MinValue)</param>
        /// <param name="maximum">最大值(无限制是给double.MaxValue)</param>
        /// <param name="increment">配合NumericUpDown使用 单击的步长</param>
        /// <param name="decimalPlaces">显示时小数点后保留几位有效数字</param>
        public THNumberRangeAttribute(double minimum, double maximum,double increment = 1,int decimalPlaces =2)
        {
            IsIntRange = false;
            DoubleMaximum = maximum;
            DoubleMinimum = minimum;
            Increment = increment;
            DecimalPlaces = decimalPlaces;
        }
    }
}
