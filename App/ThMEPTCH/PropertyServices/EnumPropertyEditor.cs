using HandyControl.Controls;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ThCADExtension;
using ThControlLibraryWPF;

namespace ThMEPTCH.PropertyServices
{
    public class EnumPropertyEditor<T> : PropertyEditorBase where T : Enum
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem)
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        protected override IValueConverter GetConverter(PropertyItem propertyItem)
        {
            return new EnumDescriptionConverter<T>();
        }

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            return Enum.GetValues(propertyItem.PropertyType)
                .OfType<Enum>().OrderBy(c=> Convert.ToInt32(c))
                .Select(o => o.GetEnumDescription());
        }
    }
}
