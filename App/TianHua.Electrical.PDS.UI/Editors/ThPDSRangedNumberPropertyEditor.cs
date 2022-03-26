using System;
using System.Windows;
using HandyControl.Data;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSRangedNumberPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new NumericUpDown
        {
            DecimalPlaces = 2,
            VerifyFunc = VerifyFunc,
            IsReadOnly = propertyItem.IsReadOnly,
        };

        public override DependencyProperty GetDependencyProperty() => NumericUpDown.ValueProperty;

        private OperationResult<bool> VerifyFunc(string data)
        {
            if (Double.TryParse(data, out double value))
            {
                if (value > 0 && value <= 1)
                {
                    return OperationResult.Success();
                }
                else
                {
                    return OperationResult.Failed("不在取值范围内(0,1]");
                }
            }
            else
            {
                return OperationResult.Failed("不是数值");
            }
        }
    }
}
