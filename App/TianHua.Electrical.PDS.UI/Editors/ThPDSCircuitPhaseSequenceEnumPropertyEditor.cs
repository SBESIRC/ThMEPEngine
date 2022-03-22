using System;
using System.Linq;
using System.Windows;
using System.Collections;
using HandyControl.Controls;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSCircuitPhaseSequenceEnumPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = IsEnabled(propertyItem),
            ItemsSource = ItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private bool IsEnabled(PropertyItem propertyItem)
        {
            var model = propertyItem.Value as ThPDSCircuitModel;
            if (model != null)
            {
                return model.PhaseSequence != PhaseSequence.L123;
            }
            else
            {
                return !propertyItem.IsReadOnly;
            }
        }

        private IEnumerable ItemsSource(PropertyItem propertyItem)
        {
            var model = propertyItem.Value as ThPDSCircuitModel;
            if (model != null)
            {
                var values = Enum.GetValues(propertyItem.PropertyType);
                if (model.PhaseSequence != PhaseSequence.L123)
                {
                    // 剔除掉L123相序
                    return values.OfType<PhaseSequence>().Where(o => o != PhaseSequence.L123);
                }
                else
                {
                    // 只保留L123相序
                    return values.OfType<PhaseSequence>().Where(o => o == PhaseSequence.L123);
                }
            }
            return Enum.GetValues(propertyItem.PropertyType);
        }
    }
}
