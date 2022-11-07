using System;
using System.Windows;
using System.Collections;
using HandyControl.Controls;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSIcuPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSBreakerModel breaker)
            {
                return breaker.AlternativeIcus;
            }
            if (propertyItem.Value is ThPDSCPSModel cps)
            {
                return cps.AlternativeIcus;
            }
            throw new NotSupportedException();
        }
    }
}
