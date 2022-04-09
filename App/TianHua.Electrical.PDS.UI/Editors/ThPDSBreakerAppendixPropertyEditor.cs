using System;
using System.Linq;
using ThCADExtension;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using System.Windows.Controls.Primitives;
using HandyControl.Controls;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSBreakerAppendixPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = GetIsEnabled(propertyItem),
            ItemsSource = GetItemsSource(propertyItem)
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        protected override IValueConverter GetConverter(PropertyItem propertyItem)
        {
            return new ThPDSEnumDescriptionConverter<AppendixType>();
        }

        private bool GetIsEnabled(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSBreakerModel breaker)
            {
                return breaker.Type.GetEnumName<ComponentType>() != ComponentType.组合式RCD;
            }
            throw new NotSupportedException();
        }

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            return Enum.GetValues(propertyItem.PropertyType)
                .OfType<AppendixType>()
                .Where(o => Convert.ToUInt32(o) > 0)
                .Select(o => o.GetEnumDescription());
        }
    }
}
