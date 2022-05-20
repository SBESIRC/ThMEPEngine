using System;
using System.Windows;
using System.Windows.Data;
using HandyControl.Data;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSRangedNumberPropertyEditor : PropertyEditorBase
    {
        public ThPDSRangedNumberPropertyEditor()
        {
            Minimum = 0;
            Maximum = 1.0;
        }

        public ThPDSRangedNumberPropertyEditor(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public double Minimum { get; set; }

        public double Maximum { get; set; }

        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new NumericUpDown
        {
            Minimum = Minimum,
            Maximum = Maximum,
            Increment = 0.05,
            DecimalPlaces = 2,
            VerifyFunc = VerifyFunc,
            IsReadOnly = propertyItem.IsReadOnly,
        };

        public override DependencyProperty GetDependencyProperty() => NumericUpDown.ValueProperty;

        public override UpdateSourceTrigger GetUpdateSourceTrigger(PropertyItem propertyItem) => UpdateSourceTrigger.LostFocus;

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
