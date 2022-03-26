using System;
using System.Windows;
using System.Collections;
using HandyControl.Controls;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSBreakerRCDTypePropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            var model = propertyItem.Value as ThPDSResidualCurrentBreakerModel;
            if (model != null)
            {
                return model.AlternativeRCDTypes;
            }
            return Enum.GetValues(propertyItem.PropertyType);
        }
    }
}
