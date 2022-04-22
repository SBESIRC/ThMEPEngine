using System;
using System.Windows;
using System.Collections;
using HandyControl.Controls;
using TianHua.Electrical.PDS.UI.Project.Module;

namespace TianHua.Electrical.PDS.UI.Editors
{
    // https://stackoverflow.com/questions/3743269/editable-combobox-with-binding-to-value-not-in-list?rq=1
    public class ThPDSSecondaryCircuitDescriptionEditPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEditable = true,
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => System.Windows.Controls.ComboBox.TextProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSSecondaryCircuitModel sc)
            {
                return sc.Descriptions;
            }
            throw new NotSupportedException();
        }
    }
}
