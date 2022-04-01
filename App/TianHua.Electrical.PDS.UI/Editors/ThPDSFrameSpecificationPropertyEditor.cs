using System;
using System.Windows;
using System.Collections;
using System.Windows.Controls.Primitives;
using HandyControl.Controls;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSFrameSpecificationPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSCPSModel cps)
            {
                return cps.AlternativeFrameSpecifications;
            }
            if (propertyItem.Value is ThPDSBreakerBaseModel breaker)
            {
                return breaker.AlternativeFrameSpecifications;
            }
            throw new NotSupportedException();
        }
    }
}
