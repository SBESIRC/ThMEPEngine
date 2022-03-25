using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.UI.Project.Module.Component;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSBreakerRatedCurrentPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem)
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private List<string> GetItemsSource(PropertyItem propertyItem)
        {
            var model = propertyItem.Value as ThPDSBreakerModel;
            if (model != null)
            {
                return model.AlternativeRatedCurrents;
            }
            else
            {
                return new List<string> { model.RatedCurrent };
            }
        }
    }
}
