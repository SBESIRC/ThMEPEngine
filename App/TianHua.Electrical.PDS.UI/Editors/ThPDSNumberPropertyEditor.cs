using System.Windows;
using System.Windows.Data;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSNumberPropertyEditor : PropertyEditorBase
    {
        public ThPDSNumberPropertyEditor()
        {
            Minimum = 0;
            Maximum = double.MaxValue;
        }

        public ThPDSNumberPropertyEditor(double minimum, double maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public double Minimum { get; set; }

        public double Maximum { get; set; }

        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new NumericUpDown
        {
            IsReadOnly = propertyItem.IsReadOnly,
            Minimum = Minimum,
            Maximum = Maximum
        };

        public override DependencyProperty GetDependencyProperty() => NumericUpDown.ValueProperty;

        public override UpdateSourceTrigger GetUpdateSourceTrigger(PropertyItem propertyItem) => UpdateSourceTrigger.LostFocus;
    }
}
