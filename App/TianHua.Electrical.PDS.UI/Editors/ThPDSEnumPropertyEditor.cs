using System;
using System.Linq;
using ThCADExtension;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using System.Windows.Controls.Primitives;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSEnumPropertyEditor<T> : PropertyEditorBase where T : Enum
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem)
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        protected override IValueConverter GetConverter(PropertyItem propertyItem)
        {
            return new ThPDSEnumDescriptionConverter<T>();
        }

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            return Enum.GetValues(propertyItem.PropertyType)
                .OfType<Enum>()
                .Where(o => Convert.ToUInt32(o) > 0)
                .Select(o => o.GetEnumDescription());
        }
    }
}
